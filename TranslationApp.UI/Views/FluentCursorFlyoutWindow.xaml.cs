using System;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace TranslationApp.UI.Views;

/// <summary>
/// النافذة الطافية الذكية بجوار مؤشر الفأرة (Fluent Cursor Flyout Window)
/// </summary>
public partial class FluentCursorFlyoutWindow : Window
{
    private string _sourceText = string.Empty;
    private string _translatedText = string.Empty;
    private readonly DispatcherTimer _autoDismissTimer;

    public event Action<string, string>? OpenInAppRequested;
    public event Func<string, Task<string>>? TranslateRequested;

    public FluentCursorFlyoutWindow()
    {
        InitializeComponent();

        _autoDismissTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _autoDismissTimer.Tick += (_, _) =>
        {
            _autoDismissTimer.Stop();
            Hide();
        };
    }

    /// <summary>
    /// عرض النافذة بجوار مؤشر الفأرة بدقة مع مراعاة حواف شاشة ويندوز وتحويل الـ DPI
    /// </summary>
    public async void ShowNearCursor(Point cursorPoint, string sourceText, bool isPillMode, string initialTranslation = "")
    {
        _sourceText = sourceText;
        _translatedText = initialTranslation;

        // تحويل إحداثيات الفأرة الفيزيائية من Win32 إلى وحدات WPF DIPs المناسبة لتحجيم الشاشة
        Point dipPoint = ConvertPixelsToDips(cursorPoint);

        double virtualLeft = SystemParameters.VirtualScreenLeft;
        double virtualTop = SystemParameters.VirtualScreenTop;
        double virtualRight = virtualLeft + SystemParameters.VirtualScreenWidth;
        double virtualBottom = virtualTop + SystemParameters.VirtualScreenHeight;

        double flyoutWidth = isPillMode ? 140 : 340;
        double flyoutHeight = isPillMode ? 60 : 250;

        double targetX = dipPoint.X + 14;
        double targetY = dipPoint.Y + 14;

        // منع الخروج عن حواف الشاشة ومراعاة الشاشات المتعددة
        if (targetX + flyoutWidth > virtualRight) targetX = Math.Max(virtualLeft, dipPoint.X - flyoutWidth - 10);
        if (targetY + flyoutHeight > virtualBottom) targetY = Math.Max(virtualTop, dipPoint.Y - flyoutHeight - 10);

        Left = targetX;
        Top = targetY;

        if (isPillMode && string.IsNullOrWhiteSpace(initialTranslation))
        {
            PillContainer.Visibility = Visibility.Visible;
            MiniCardContainer.Visibility = Visibility.Collapsed;
        }
        else
        {
            SwitchToMiniCard();
            if (!string.IsNullOrWhiteSpace(initialTranslation))
            {
                TranslatedResultTextBlock.Text = initialTranslation;
            }
            else
            {
                await PerformTranslationAsync();
            }
        }

        Show();
        Activate();

        _autoDismissTimer.Stop();
        _autoDismissTimer.Start();
    }

    private Point ConvertPixelsToDips(Point pixelPoint)
    {
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget != null)
        {
            return source.CompositionTarget.TransformFromDevice.Transform(pixelPoint);
        }

        double dpiX = VisualTreeHelper.GetDpi(this).DpiScaleX;
        double dpiY = VisualTreeHelper.GetDpi(this).DpiScaleY;
        if (dpiX > 0 && dpiY > 0)
        {
            return new Point(pixelPoint.X / dpiX, pixelPoint.Y / dpiY);
        }

        return pixelPoint;
    }

    private void SwitchToMiniCard()
    {
        PillContainer.Visibility = Visibility.Collapsed;
        MiniCardContainer.Visibility = Visibility.Visible;
        CopiedToastTextBlock.Visibility = Visibility.Collapsed;
    }

    private async Task PerformTranslationAsync()
    {
        TranslatedResultTextBlock.Text = "جاري الترجمة...";
        if (TranslateRequested != null && !string.IsNullOrWhiteSpace(_sourceText))
        {
            try
            {
                _translatedText = await TranslateRequested.Invoke(_sourceText);
                TranslatedResultTextBlock.Text = _translatedText;
            }
            catch
            {
                TranslatedResultTextBlock.Text = "تعذرت الترجمة الفورية";
            }
        }
    }

    private async void PillActionButton_Click(object sender, RoutedEventArgs e)
    {
        _autoDismissTimer.Stop();
        SwitchToMiniCard();
        await PerformTranslationAsync();
        _autoDismissTimer.Start();
    }

    private void CopyResultButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_translatedText))
        {
            try
            {
                Clipboard.SetText(_translatedText);
                CopiedToastTextBlock.Visibility = Visibility.Visible;
            }
            catch { }
        }
    }

    private void SpeakResultButton_Click(object sender, RoutedEventArgs e)
    {
        // تم إلغاء خاصية الترجمة الصوتية مؤقتاً ليتم معالجتها لاحقاً
    }

    private void OpenInAppButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        OpenInAppRequested?.Invoke(_sourceText, _translatedText);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _autoDismissTimer.Stop();
        Hide();
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        _autoDismissTimer.Stop();
        Hide();
    }
}
