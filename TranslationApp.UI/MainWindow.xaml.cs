using System;
using System.Collections.Generic;
using System.Globalization;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TranslationApp.Application;
using TranslationApp.Domain;
using AppLanguage = TranslationApp.Domain.Language;
using TranslationApp.Infrastructure.Translation;
using TranslationApp.UI.Controls;
using TranslationApp.UI.Helpers;
using TranslationApp.UI.Services;
using TranslationApp.UI.Views;

namespace TranslationApp.UI
{
    /// <summary>
    /// نافذة التطبيق الرئيسية مع دعم الاختصارات العامة والمساعد الذكي لمؤشر الفأرة (Fluent Architecture)
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly GlobalHotKeyService _hotKeyService = new();
#pragma warning disable CS0649
        private FluentCursorFlyoutWindow? _flyoutWindow;
#pragma warning restore CS0649
        private readonly TranslateTextUseCase _translateUseCase = new(new GoogleWebTranslator());
        private readonly UserPreferences _prefs;

        private CursorAssistantFlags _assistantFlags;
        private HotKeyDefinition _currentHotKey;
        private LanguagePair _languages;
        private bool _isRecordingHotKey;
        private CancellationTokenSource? _translationCts;
        private FrameworkElement? _lastClosedPlacementTarget;
        private DateTime _languageDropdownClosedTimestamp = DateTime.MinValue;

        private readonly System.Windows.Threading.DispatcherTimer _debounceTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(400)
        };
        private long _currentRequestId = 0;
        private AppLanguage? _lastDetectedLang;
        private TranslationFailureReason? _lastFailureReason;

        public MainWindow()
        {
            InitializeComponent();

            _debounceTimer.Tick += DebounceTimer_Tick;

            _prefs = AppSettingsService.Load();
            _assistantFlags = _prefs.AssistantFlags;
            _currentHotKey = _prefs.HotKey;
            _languages = _prefs.Languages;

            ApplyInitialPreferences();

            LanguageDropdownPopup.Closed += (_, _) =>
            {
                _languageDropdownClosedTimestamp = DateTime.UtcNow;
                _lastClosedPlacementTarget = LanguageDropdownPopup.PlacementTarget as FrameworkElement;
            };
            Deactivated += (_, _) => CloseLanguageDropdown();
            LocationChanged += (_, _) => CloseLanguageDropdown();
            SizeChanged += (_, _) => CloseLanguageDropdown();
            SourceInitialized += MainWindow_SourceInitialized;
            StateChanged += MainWindow_StateChanged;
            KeyDown += MainWindow_KeyDown;
            Closed += MainWindow_Closed;
        }

        private void DebounceTimer_Tick(object? sender, EventArgs e)
        {
            _debounceTimer.Stop();
            TriggerTranslation();
        }

        private void ApplyInitialPreferences()
        {
            UpdateLanguageBarLabels();
            HotKeyDisplayTextBlock.Text = _currentHotKey.DisplayText;

            AssistantEnabledCheckBox.IsChecked = (_assistantFlags & CursorAssistantFlags.IsEnabled) != 0;
            PillModeRadioButton.IsChecked = (_assistantFlags & CursorAssistantFlags.PillMode) != 0;
            InstantCardRadioButton.IsChecked = (_assistantFlags & CursorAssistantFlags.MiniCardMode) != 0;

            AutoCopyCheckBox.IsChecked = _prefs.AutoCopy;
            Topmost = _prefs.AlwaysOnTop;
            AlwaysOnTopCheckBox.IsChecked = Topmost;

            RefreshFavoritesList();
            UpdateSourceTextDirection(string.Empty);
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (MaximizeButton != null)
            {
                MaximizeButton.Content = (WindowState == WindowState.Maximized) ? "\uE923" : "\uE922";
                bool isArabic = AppLocalization.ResolveIsArabic(_prefs.AppLanguage);
                MaximizeButton.ToolTip = (WindowState == WindowState.Maximized) 
                    ? (isArabic ? "استعادة" : "Restore") 
                    : (isArabic ? "تكبير" : "Maximize");
            }
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            // 1. مراقبة تغيرات ثيم ولغة ويندوز والتفاعل المباشر معها
            SystemThemeBridge.InitializeThemeTracking(this, onSettingChanged: () =>
            {
                ApplyTheme(_prefs.Theme, save: false);
                if (_prefs.AppLanguage == AppLanguageMode.System)
                {
                    ApplyAppLanguage(AppLanguageMode.System, save: false);
                }
            });

            ApplyTheme(_prefs.Theme, save: false);
            ApplyAppLanguage(_prefs.AppLanguage, save: false);
            UpdatePinButtonVisual();
        }

        /// <summary>
        /// استجابة فورية عند ضغط الاختصار العام من أي متصفح أو تطبيق في ويندوز
        /// </summary>
        private async void OnGlobalHotKeyPressed()
        {
            if ((_assistantFlags & CursorAssistantFlags.IsEnabled) == 0) return;

            string capturedText = await TextSelectionCapture.TryCaptureSelectedTextAsync();
            if (string.IsNullOrWhiteSpace(capturedText)) return;

            Point mousePoint = TextSelectionCapture.GetCurrentMousePosition();
            bool isPillMode = (_assistantFlags & CursorAssistantFlags.PillMode) != 0;

            _flyoutWindow?.ShowNearCursor(mousePoint, capturedText, isPillMode);
        }

        private async Task<string> OnFlyoutTranslateRequestedAsync(string sourceText)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                var result = await _translateUseCase.ExecuteAsync(new TranslationRequest(sourceText, _languages.Source, _languages.Target), cts.Token);
                return result.Text;
            }
            catch
            {
                return "تعذرت الترجمة الفورية";
            }
        }

        private void OnFlyoutOpenInAppRequested(string sourceText, string translatedText)
        {
            SourceTextBox.Text = sourceText;
            TargetTextBox.Text = translatedText;

            UpdateSourceTextDirection(sourceText);
            UpdateTargetTextDirection(translatedText);

            if (WindowState == WindowState.Minimized)
            {
                SystemCommands.RestoreWindow(this);
            }
            Activate();
        }

        #region مركز الإعدادات وتخصيص الاختصار

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            CloseLanguageDropdown();
            SettingsOverlayLayer.Visibility = (SettingsOverlayLayer.Visibility == Visibility.Visible)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void CloseSettingsDrawerButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsOverlayLayer.Visibility = Visibility.Collapsed;
        }

        private void SettingsScrim_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SettingsOverlayLayer.Visibility = Visibility.Collapsed;
        }

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            Topmost = !Topmost;
            AlwaysOnTopCheckBox.IsChecked = Topmost;
            _prefs.AlwaysOnTop = Topmost;

            UpdatePinButtonVisual();
            SavePreferences();
        }

        private void UpdatePinButtonVisual()
        {
            PinButton.IsActive = Topmost;
        }

        #region معالجات بطاقات الإعدادات الأساسية

        private void ThemeSystemButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyTheme(AppThemeMode.System);
        }

        private void ThemeLightButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyTheme(AppThemeMode.Light);
        }

        private void ThemeDarkButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyTheme(AppThemeMode.Dark);
        }

        private void ApplyTheme(AppThemeMode mode, bool save = true)
        {
            SetButtonActiveVisual(ThemeSystemButton, mode == AppThemeMode.System);
            SetButtonActiveVisual(ThemeLightButton, mode == AppThemeMode.Light);
            SetButtonActiveVisual(ThemeDarkButton, mode == AppThemeMode.Dark);

            bool? forceDark = mode switch
            {
                AppThemeMode.Dark => true,
                AppThemeMode.Light => false,
                _ => null
            };

            SystemThemeBridge.SyncWithSystemTheme(this, forceDark);
            UpdatePinButtonVisual();

            _prefs.Theme = mode;
            if (save)
            {
                SavePreferences();
            }
        }

        private void LangSystemButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyAppLanguage(AppLanguageMode.System);
        }

        private void LangArabicButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyAppLanguage(AppLanguageMode.Arabic);
        }

        private void LangEnglishButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyAppLanguage(AppLanguageMode.English);
        }

        private void ApplyAppLanguage(AppLanguageMode mode, bool save = true)
        {
            SetButtonActiveVisual(LangSystemButton, mode == AppLanguageMode.System);
            SetButtonActiveVisual(LangArabicButton, mode == AppLanguageMode.Arabic);
            SetButtonActiveVisual(LangEnglishButton, mode == AppLanguageMode.English);

            bool isArabic = AppLocalization.ResolveIsArabic(mode);

            try
            {
                var culture = new CultureInfo(isArabic ? "ar-SA" : "en-US");
                CultureInfo.CurrentUICulture = culture;
                CultureInfo.CurrentCulture = culture;
            }
            catch { }

            MainContentGrid.FlowDirection = isArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            SettingsFlyoutContainer.FlowDirection = isArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            UpdateLocalizedStrings(isArabic);

            _prefs.AppLanguage = mode;
            if (save)
            {
                SavePreferences();
            }
        }

        private void AlwaysOnTopCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Topmost = (AlwaysOnTopCheckBox.IsChecked == true);
            _prefs.AlwaysOnTop = Topmost;

            UpdatePinButtonVisual();
            SavePreferences();
        }

        private void AutoCopyCheckBox_Click(object sender, RoutedEventArgs e)
        {
            _prefs.AutoCopy = (AutoCopyCheckBox.IsChecked == true);
            SavePreferences();
        }

        private void SetButtonActiveVisual(FluentButtonBase button, bool isActive)
        {
            if (button == null) return;
            button.IsActive = isActive;
        }

        private void UpdateLocalizedStrings(bool isArabic)
        {
            PinButton.ToolTip = isArabic ? "إبقاء النافذة في المقدمة" : "Keep window on top";
            SettingsButton.ToolTip = isArabic ? "الإعدادات والتفضيلات" : "Settings & Preferences";
            MinimizeButton.ToolTip = isArabic ? "تصغير" : "Minimize";
            MaximizeButton.ToolTip = (WindowState == WindowState.Maximized) 
                ? (isArabic ? "استعادة" : "Restore") 
                : (isArabic ? "تكبير" : "Maximize");
            CloseButton.ToolTip = isArabic ? "إغلاق" : "Close";

            SwapLanguagesButton.ToolTip = isArabic ? "تبديل اللغات" : "Swap languages";

            SourceClearButton.ToolTip = isArabic ? "مسح الحقل" : "Clear text";

            CopyButton.Content = isArabic ? "نسخ" : "Copy";
            CopyButton.ToolTip = isArabic ? "نسخ الترجمة" : "Copy translation";
            FavoriteButton.ToolTip = isArabic ? "إضافة للمفضلة" : "Add to favorites";

            SettingsHeaderTitle.Text = isArabic ? "الإعدادات والتفضيلات" : "Settings & Preferences";
            CloseSettingsDrawerButton.ToolTip = isArabic ? "إغلاق الإعدادات" : "Close settings";

            AppearanceCardTitle.Text = isArabic ? "المظهر والثيم" : "Appearance & Theme";
            AppearanceCardSubtitle.Text = isArabic ? "التكيف مع مظهر ويندوز" : "Match Windows appearance";
            ThemeSystemButton.Content = isArabic ? "النظام" : "System";
            ThemeLightButton.Content = isArabic ? "فاتح" : "Light";
            ThemeDarkButton.Content = isArabic ? "داكن" : "Dark";

            AppLanguageCardTitle.Text = isArabic ? "لغة التطبيق" : "App Language";
            AppLanguageCardSubtitle.Text = isArabic ? "لغة عرض واجهة التطبيق" : "Application display language";
            LangSystemButton.Content = isArabic ? "النظام" : "System";
            LangArabicButton.Content = isArabic ? "العربية" : "Arabic";
            LangEnglishButton.Content = isArabic ? "English" : "English";

            WindowBehaviorCardTitle.Text = isArabic ? "سلوك النافذة" : "Window Behavior";
            WindowBehaviorCardSubtitle.Text = isArabic ? "خيارات التثبيت والأتمتة" : "Pinning & automation options";
            AlwaysOnTopLabel.Text = isArabic ? "إبقاء النافذة في المقدمة دائماً" : "Keep window always on top";
            AutoCopyLabel.Text = isArabic ? "نسخ الترجمة تلقائياً إلى الحافظة" : "Copy translation automatically to clipboard";

            if (AboutAppTitleTextBlock != null) AboutAppTitleTextBlock.Text = isArabic ? "حول التطبيق" : "About App";
            if (AboutAppSubtitleTextBlock != null) AboutAppSubtitleTextBlock.Text = isArabic ? "معلومات الإصدار والمطور" : "Version & developer info";
            if (DeveloperAppModelTextBlock != null) DeveloperAppModelTextBlock.Text = isArabic ? "دوت نت 9" : ".NET 9";
            if (DeveloperNameTextBlock != null) DeveloperNameTextBlock.Text = isArabic ? "المطور: muath" : "Developer: muath";
            if (MadeInRiyadhTextBlock != null) MadeInRiyadhTextBlock.Text = isArabic ? "مكان الصنع: المملكة العربية السعودية، الرياض 🇸🇦" : "Made in: Kingdom of Saudi Arabia, Riyadh 🇸🇦";
            if (DeveloperEmailButton != null) DeveloperEmailButton.ToolTip = isArabic ? "مراسلة المطور عبر البريد" : "Send email to developer";

            RetryTranslationButton.Content = isArabic ? "إعادة المحاولة" : "Retry";
            UpdateLanguageBarLabels();
            if (_lastFailureReason.HasValue && TranslationErrorPanel.Visibility == Visibility.Visible)
            {
                ShowError(_lastFailureReason.Value);
            }
        }

        private void AboutAppHeader_Click(object sender, MouseButtonEventArgs e)
        {
            if (AboutAppDetailsPanel == null || AboutAppChevronTextBlock == null) return;

            bool isCurrentlyExpanded = (AboutAppDetailsPanel.Visibility == Visibility.Visible);
            if (isCurrentlyExpanded)
            {
                AboutAppDetailsPanel.Visibility = Visibility.Collapsed;
                AboutAppChevronTextBlock.Text = "\uE70D"; // ChevronDown
            }
            else
            {
                AboutAppDetailsPanel.Visibility = Visibility.Visible;
                AboutAppChevronTextBlock.Text = "\uE70E"; // ChevronUp
            }
        }

        private void DeveloperEmailButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo("mailto:mmuath2025@gmail.com?subject=TranslationApp%20Feedback")
                {
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch
            {
                try
                {
                    Clipboard.SetText("mmuath2025@gmail.com");
                    string msg = AppLocalization.ResolveIsArabic(_prefs.AppLanguage)
                        ? "تم نسخ البريد الإلكتروني إلى الحافظة: mmuath2025@gmail.com"
                        : "Email copied to clipboard: mmuath2025@gmail.com";
                    MessageBox.Show(msg, "TranslationApp", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch { }
            }
        }

        #endregion

        private void AssistantEnabledCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (AssistantEnabledCheckBox.IsChecked == true)
            {
                _assistantFlags |= CursorAssistantFlags.IsEnabled;
                _hotKeyService.Register(_currentHotKey);
            }
            else
            {
                _assistantFlags &= ~CursorAssistantFlags.IsEnabled;
                _hotKeyService.Unregister();
            }

            _prefs.AssistantFlags = _assistantFlags;
            SavePreferences();
        }

        private void AssistantMode_Changed(object sender, RoutedEventArgs e)
        {
            if (PillModeRadioButton.IsChecked == true)
            {
                _assistantFlags |= CursorAssistantFlags.PillMode;
                _assistantFlags &= ~CursorAssistantFlags.MiniCardMode;
            }
            else
            {
                _assistantFlags |= CursorAssistantFlags.MiniCardMode;
                _assistantFlags &= ~CursorAssistantFlags.PillMode;
            }

            _prefs.AssistantFlags = _assistantFlags;
            SavePreferences();
        }

        private void ChangeHotKeyButton_Click(object sender, RoutedEventArgs e)
        {
            _isRecordingHotKey = !_isRecordingHotKey;
            if (_isRecordingHotKey)
            {
                ChangeHotKeyButton.Content = "إلغاء";
                HotKeyDisplayTextBlock.Text = "اضغط المفاتيح الآن...";
            }
            else
            {
                ChangeHotKeyButton.Content = "تغيير";
                HotKeyDisplayTextBlock.Text = _currentHotKey.DisplayText;
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (LanguageDropdownPopup != null && LanguageDropdownPopup.IsOpen)
                {
                    LanguageDropdownPopup.IsOpen = false;
                    e.Handled = true;
                    return;
                }

                if (SettingsOverlayLayer.Visibility == Visibility.Visible)
                {
                    SettingsOverlayLayer.Visibility = Visibility.Collapsed;
                    e.Handled = true;
                    return;
                }
            }

            if (!_isRecordingHotKey) return;

            HotKeyModifiers modifiers = HotKeyModifiers.None;
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) modifiers |= HotKeyModifiers.Control;
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) modifiers |= HotKeyModifiers.Shift;
            if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0) modifiers |= HotKeyModifiers.Alt;
            if ((Keyboard.Modifiers & ModifierKeys.Windows) != 0) modifiers |= HotKeyModifiers.Win;

            Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;

            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            int vk = KeyInterop.VirtualKeyFromKey(key);
            if (vk > 0 && modifiers != HotKeyModifiers.None)
            {
                var newHotKey = new HotKeyDefinition(modifiers, vk);
                if (_hotKeyService.Register(newHotKey))
                {
                    _currentHotKey = newHotKey;
                    _prefs.HotKey = newHotKey;
                    HotKeyDisplayTextBlock.Text = _currentHotKey.DisplayText;
                    SavePreferences();
                }
                else
                {
                    HotKeyDisplayTextBlock.Text = "الاختصار محجوز أو غير متاح!";
                    Task.Delay(1800).ContinueWith(_ => Dispatcher.Invoke(() => HotKeyDisplayTextBlock.Text = _currentHotKey.DisplayText));
                }

                _isRecordingHotKey = false;
                ChangeHotKeyButton.Content = "تغيير";
                e.Handled = true;
            }
        }

        #endregion

        #region تفاعلات مساحة العمل والترجمة واختيار اللغات

        private void SourceLanguageCapsule_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            e.Handled = true;
            SourceLanguageCapsule.CaptureMouse();
        }

        private void SourceLanguageCapsule_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            e.Handled = true;

            bool wasCaptured = SourceLanguageCapsule.IsMouseCaptured;
            if (wasCaptured)
            {
                SourceLanguageCapsule.ReleaseMouseCapture();
            }

            if (wasCaptured && SourceLanguageCapsule.IsMouseOver)
            {
                ToggleLanguageDropdown(SourceLanguageCapsule, isSource: true);
            }
        }

        private void TargetLanguageCapsule_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            e.Handled = true;
            TargetLanguageCapsule.CaptureMouse();
        }

        private void TargetLanguageCapsule_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            e.Handled = true;

            bool wasCaptured = TargetLanguageCapsule.IsMouseCaptured;
            if (wasCaptured)
            {
                TargetLanguageCapsule.ReleaseMouseCapture();
            }

            if (wasCaptured && TargetLanguageCapsule.IsMouseOver)
            {
                ToggleLanguageDropdown(TargetLanguageCapsule, isSource: false);
            }
        }

        private void ToggleLanguageDropdown(FrameworkElement targetElement, bool isSource)
        {
            if (LanguageDropdownPopup.IsOpen)
            {
                bool wasSameTarget = (LanguageDropdownPopup.PlacementTarget == targetElement);
                LanguageDropdownPopup.IsOpen = false;

                if (wasSameTarget)
                {
                    return;
                }
            }
            else if (_lastClosedPlacementTarget == targetElement &&
                     (DateTime.UtcNow - _languageDropdownClosedTimestamp).TotalMilliseconds < 450)
            {
                // النقر بالزر الأيسر على نفس الكبسولة أثناء إغلاق القائمة يمنع إعادة فتحها فوراً (Toggle Close)
                _lastClosedPlacementTarget = null;
                return;
            }

            OpenLanguageDropdown(targetElement, isSource);
        }

        private void CloseLanguageDropdown()
        {
            if (LanguageDropdownPopup != null && LanguageDropdownPopup.IsOpen)
            {
                LanguageDropdownPopup.IsOpen = false;
            }
        }

        private void OpenLanguageDropdown(FrameworkElement targetElement, bool isSource)
        {
            LanguageDropdownItemsHost.Children.Clear();
            LanguageDropdownPopup.FlowDirection = FlowDirection;

            if (isSource)
            {
                bool isAutoSelected = (_languages.Source == AppLanguage.Auto);
                var autoButton = CreateLanguageDropdownItem(
                    displayName: "اكتشاف تلقائي — Auto Detect",
                    isSelected: isAutoSelected,
                    onClick: () =>
                    {
                        LanguageDropdownPopup.IsOpen = false;
                        if (_languages.Source == AppLanguage.Auto) return;

                        _languages = _languages.WithSource(AppLanguage.Auto);
                        _lastDetectedLang = null;
                        UpdateLanguageBarLabels();
                        _prefs.Languages = _languages;
                        SavePreferences();

                        UpdateSourceTextDirection(SourceTextBox.Text);

                        if (!string.IsNullOrWhiteSpace(SourceTextBox.Text))
                        {
                            TriggerTranslation();
                        }
                    });

                LanguageDropdownItemsHost.Children.Add(autoButton);

                LanguageDropdownItemsHost.Children.Add(new Border
                {
                    Height = 1,
                    Background = (Brush)FindResource("DividerLineBrush"),
                    Margin = new Thickness(6, 4, 6, 4)
                });
            }

            var languages = new[]
            {
                AppLanguage.Arabic,
                AppLanguage.English,
                AppLanguage.French,
                AppLanguage.German,
                AppLanguage.Spanish,
                AppLanguage.Turkish
            };

            var currentLang = isSource ? _languages.Source : _languages.Target;

            foreach (var lang in languages)
            {
                bool isSelected = (currentLang == lang);
                var langButton = CreateLanguageDropdownItem(
                    displayName: lang.GetDisplayName(),
                    isSelected: isSelected,
                    onClick: () =>
                    {
                        LanguageDropdownPopup.IsOpen = false;

                        if ((isSource ? _languages.Source : _languages.Target) == lang)
                        {
                            return;
                        }

                        _languages = isSource ? _languages.WithSource(lang) : _languages.WithTarget(lang);
                        if (isSource)
                        {
                            _lastDetectedLang = null;
                        }

                        UpdateLanguageBarLabels();
                        _prefs.Languages = _languages;
                        SavePreferences();

                        UpdateSourceTextDirection(SourceTextBox.Text);

                        if (!string.IsNullOrWhiteSpace(SourceTextBox.Text))
                        {
                            TriggerTranslation();
                        }
                    });

                LanguageDropdownItemsHost.Children.Add(langButton);
            }

            LanguageDropdownPopup.PlacementTarget = targetElement;
            LanguageDropdownPopup.IsOpen = true;
        }

        private Button CreateLanguageDropdownItem(string displayName, bool isSelected, Action onClick)
        {
            var button = new Button
            {
                Style = (Style)FindResource("LanguageDropdownItemButtonStyle")
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var checkIcon = new TextBlock
            {
                Text = "\uE73E",
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = 12,
                Foreground = (Brush)FindResource("SystemAccentBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Visibility = isSelected ? Visibility.Visible : Visibility.Hidden
            };
            Grid.SetColumn(checkIcon, 0);
            grid.Children.Add(checkIcon);

            var textBlock = new TextBlock
            {
                Text = displayName,
                FontSize = 13,
                FontWeight = isSelected ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = (Brush)FindResource("TextPrimaryBrush"),
                FontFamily = new FontFamily("Segoe UI Variable, Segoe UI"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0)
            };
            Grid.SetColumn(textBlock, 1);
            grid.Children.Add(textBlock);

            button.Content = grid;

            button.Click += (_, e) =>
            {
                e.Handled = true;
                onClick();
            };

            return button;
        }

        private void UpdateLanguageBarLabels()
        {
            bool isArabic = AppLocalization.ResolveIsArabic(_prefs.AppLanguage);
            SourceLanguageLabel.Text = _languages.FormatSourceDisplayName(isArabic, _lastDetectedLang);
            TargetLanguageLabel.Text = _languages.FormatTargetDisplayName(isArabic);
        }

        private void SwapLanguagesButton_Click(object sender, RoutedEventArgs e)
        {
            CloseLanguageDropdown();
            _languages = _languages.Swap(_lastDetectedLang);
            _lastDetectedLang = null;
            UpdateLanguageBarLabels();

            _prefs.Languages = _languages;
            SavePreferences();

            string temp = SourceTextBox.Text;
            SourceTextBox.Text = TargetTextBox.Text;
            TargetTextBox.Text = temp;

            UpdateSourceTextDirection(SourceTextBox.Text);
            UpdateTargetTextDirection(TargetTextBox.Text);

            if (!string.IsNullOrWhiteSpace(SourceTextBox.Text))
            {
                TriggerTranslation();
            }
        }

        private void SourceTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = SourceTextBox.Text;
            SourceCounterTextBlock.Text = $"{text.Length} / 5000";

            UpdateSourceTextDirection(text);

            if (string.IsNullOrWhiteSpace(text))
            {
                _debounceTimer.Stop();
                _translationCts?.Cancel();
                _currentRequestId++;
                HideError();
                TargetTextBox.Text = string.Empty;
                TranslationProgressBar.Visibility = Visibility.Collapsed;
                FavoriteButton.Foreground = (Brush)FindResource("TextPrimaryBrush");
                if (_languages.Source == AppLanguage.Auto)
                {
                    _lastDetectedLang = null;
                    UpdateLanguageBarLabels();
                }
                return;
            }

            if (text.Length > 5000)
            {
                _debounceTimer.Stop();
                _translationCts?.Cancel();
                _currentRequestId++;
                TranslationProgressBar.Visibility = Visibility.Collapsed;
                ShowError(TranslationFailureReason.TextTooLong);
                return;
            }

            HideError();
            TranslationProgressBar.Visibility = Visibility.Visible;
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private void UpdateSourceTextDirection(string text)
        {
            bool isArabicUi = AppLocalization.ResolveIsArabic(_prefs.AppLanguage);
            var dir = TextDirectionDetector.ResolveSourceDirection(text, _languages.Source, isArabicUi);
            SourceTextBox.FlowDirection = (dir == TextDirection.RightToLeft) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            SourceTextBox.TextAlignment = TextAlignment.Left;
        }

        private void UpdateTargetTextDirection(string text)
        {
            var dir = TextDirectionDetector.ResolveTargetDirection(text, _languages.Target);
            TargetTextBox.FlowDirection = (dir == TextDirection.RightToLeft) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            TargetTextBox.TextAlignment = TextAlignment.Left;
        }

        private async void TriggerTranslation()
        {
            string text = SourceTextBox.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                TranslationProgressBar.Visibility = Visibility.Collapsed;
                return;
            }

            long requestId = Interlocked.Increment(ref _currentRequestId);

            // إدارة CancellationTokenSource بأمان دون التخلص الفوري أثناء تشغيل مهمة سابقة
            _translationCts?.Cancel();
            var cts = new CancellationTokenSource();
            _translationCts = cts;
            var token = cts.Token;

            TranslationProgressBar.Visibility = Visibility.Visible;
            HideError();

            try
            {
                var request = new TranslationRequest(text, _languages.Source, _languages.Target);
                var result = await _translateUseCase.ExecuteAsync(request, token);

                // منع وصول نتائج قديمة بعد طلب أحدث (Race Condition Guard)
                if (token.IsCancellationRequested || requestId != Interlocked.Read(ref _currentRequestId))
                {
                    return;
                }

                TargetTextBox.Text = result.Text;
                UpdateTargetTextDirection(result.Text);
                TranslationProgressBar.Visibility = Visibility.Collapsed;

                if (_languages.Source == AppLanguage.Auto)
                {
                    _lastDetectedLang = result.DetectedLanguage;
                    UpdateLanguageBarLabels();
                }

                if (_prefs.AutoCopy && !string.IsNullOrWhiteSpace(result.Text))
                {
                    try { Clipboard.SetText(result.Text); } catch { }
                }
            }
            catch (OperationCanceledException)
            {
                if (requestId == Interlocked.Read(ref _currentRequestId))
                {
                    TranslationProgressBar.Visibility = Visibility.Collapsed;
                }
            }
            catch (TranslationException ex)
            {
                if (requestId == Interlocked.Read(ref _currentRequestId) && !token.IsCancellationRequested)
                {
                    TranslationProgressBar.Visibility = Visibility.Collapsed;
                    ShowError(ex.Reason);
                }
            }
            catch (Exception ex)
            {
                if (requestId == Interlocked.Read(ref _currentRequestId) && !token.IsCancellationRequested)
                {
                    TranslationProgressBar.Visibility = Visibility.Collapsed;
                    var reason = (ex is TimeoutException) 
                        ? TranslationFailureReason.Timeout 
                        : TranslationFailureReason.ServiceUnavailable;
                    ShowError(reason);
                }
            }
        }

        private void ShowError(TranslationFailureReason reason)
        {
            _lastFailureReason = reason;
            bool isArabic = AppLocalization.ResolveIsArabic(_prefs.AppLanguage);
            var info = reason.GetDisplayInfo(isArabic);

            ErrorIconTextBlock.Text = info.Icon;
            ErrorTitleTextBlock.Text = info.Title;
            ErrorMessageTextBlock.Text = info.Message;
            RetryTranslationButton.Content = isArabic ? "إعادة المحاولة" : "Retry";

            TargetTextBox.Visibility = Visibility.Collapsed;
            TranslationErrorPanel.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            _lastFailureReason = null;
            TranslationErrorPanel.Visibility = Visibility.Collapsed;
            TargetTextBox.Visibility = Visibility.Visible;
        }

        private void RetryTranslationButton_Click(object sender, RoutedEventArgs e)
        {
            HideError();
            TriggerTranslation();
        }

        private void SourceSpeakButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void TargetSpeakButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void SpeakText(string text, AppLanguage lang)
        {
            return;
        }

        private void SourceClearButton_Click(object sender, RoutedEventArgs e)
        {
            _debounceTimer.Stop();
            _translationCts?.Cancel();
            _currentRequestId++;
            HideError();
            SourceTextBox.Clear();
            TargetTextBox.Clear();
            TranslationProgressBar.Visibility = Visibility.Collapsed;
            _lastDetectedLang = null;
            UpdateLanguageBarLabels();
            FavoriteButton.Foreground = (Brush)FindResource("TextPrimaryBrush");
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (TranslationErrorPanel.Visibility == Visibility.Visible) return;

            if (!string.IsNullOrWhiteSpace(TargetTextBox.Text))
            {
                try { Clipboard.SetText(TargetTextBox.Text); } catch { }

                bool isArabic = AppLocalization.ResolveIsArabic(_prefs.AppLanguage);
                CopyButton.Content = isArabic ? "تم النسخ ✓" : "Copied ✓";

                var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
                timer.Tick += (s, ev) =>
                {
                    timer.Stop();
                    CopyButton.Content = isArabic ? "نسخ" : "Copy";
                };
                timer.Start();
            }
        }

        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            string source = SourceTextBox.Text.Trim();
            string target = TargetTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target)) return;

            var existing = _prefs.Favorites.Find(f => f.SourceText == source);
            if (existing != null)
            {
                _prefs.Favorites.Remove(existing);
                FavoriteButton.Foreground = (Brush)FindResource("TextPrimaryBrush");
            }
            else
            {
                _prefs.Favorites.Insert(0, new FavoriteItem(source, target, _languages.Source, _languages.Target, DateTime.Now));
                FavoriteButton.Foreground = Brushes.Gold;
            }

            RefreshFavoritesList();
            SavePreferences();
        }

        private void ClearFavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            _prefs.Favorites.Clear();
            FavoriteButton.Foreground = (Brush)FindResource("TextPrimaryBrush");
            RefreshFavoritesList();
            SavePreferences();
        }

        private void RefreshFavoritesList()
        {
            FavoritesItemsControl.ItemsSource = null;
            FavoritesItemsControl.ItemsSource = _prefs.Favorites;
            FavoritesCountTextBlock.Text = $"{_prefs.Favorites.Count} عناصر محفوظة";
        }

        private void SavePreferences()
        {
            _prefs.AssistantFlags = _assistantFlags;
            _prefs.HotKey = _currentHotKey;
            _prefs.Languages = _languages;
            AppSettingsService.Save(_prefs);
        }

        #endregion

        #region أزرار شريط العنوان القياسية

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                SystemCommands.RestoreWindow(this);
            }
            else
            {
                SystemCommands.MaximizeWindow(this);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            SavePreferences();
            _hotKeyService.Dispose();
            _flyoutWindow?.Close();
        }

        #endregion
    }
}