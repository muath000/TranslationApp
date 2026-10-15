using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TranslationApp.Domain;

namespace TranslationApp.UI.Controls;

/// <summary>
/// الزر الأساسي الموحد بنمط Windows 11 Fluent Design
/// </summary>
public class FluentButtonBase : Button
{
    static FluentButtonBase()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(FluentButtonBase),
            new FrameworkPropertyMetadata(typeof(FluentButtonBase)));
    }

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(string),
            typeof(FluentButtonBase),
            new PropertyMetadata(string.Empty));

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(
            nameof(CornerRadius),
            typeof(CornerRadius),
            typeof(FluentButtonBase),
            new PropertyMetadata(new CornerRadius(6)));

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public static readonly DependencyProperty ActionFlagProperty =
        DependencyProperty.Register(
            nameof(ActionFlag),
            typeof(TranslationActionFlags),
            typeof(FluentButtonBase),
            new PropertyMetadata(TranslationActionFlags.None));

    public TranslationActionFlags ActionFlag
    {
        get => (TranslationActionFlags)GetValue(ActionFlagProperty);
        set => SetValue(ActionFlagProperty, value);
    }

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(FluentButtonBase),
            new PropertyMetadata(false));

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }
}
