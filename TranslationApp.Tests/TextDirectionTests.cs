using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using TranslationApp.Domain;
using Xunit;
using Xunit.Abstractions;

namespace TranslationApp.Tests;

public class TextDirectionDetectorTests
{
    [Theory]
    [InlineData("مرحبا بك", TextDirection.RightToLeft)]
    [InlineData("hello", TextDirection.LeftToRight)]
    [InlineData("مرحبا hello 123", TextDirection.RightToLeft)]
    [InlineData("123 مرحبا", TextDirection.RightToLeft)]
    [InlineData("123", TextDirection.Neutral)]
    [InlineData("", TextDirection.Neutral)]
    [InlineData(null, TextDirection.Neutral)]
    [InlineData("!!! hello", TextDirection.LeftToRight)]
    [InlineData("   \t\n   ", TextDirection.Neutral)]
    [InlineData("!@#$%^&*()", TextDirection.Neutral)]
    [InlineData("12345 Hello", TextDirection.LeftToRight)]
    [InlineData("١٢٣٤٥ مرحبا", TextDirection.RightToLeft)]
    public void Detect_ReturnsCorrectTextDirection_ForGivenInput(string? input, TextDirection expectedDirection)
    {
        var actual = TextDirectionDetector.Detect(input);

        Assert.Equal(expectedDirection, actual);
    }
}

public class ArabicTextDirectionTests
{
    private readonly ITestOutputHelper _output;

    public ArabicTextDirectionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TextDirectionDetector_IdentifiesArabicCorrectly()
    {
        Assert.Equal(TextDirection.RightToLeft, TextDirectionDetector.Detect("مرحبا"));
        Assert.Equal(TextDirection.RightToLeft, TextDirectionDetector.Detect("السلام عليكم ورحمة الله"));
        Assert.Equal(TextDirection.RightToLeft, TextDirectionDetector.Detect("123 مرحبا"));
        Assert.Equal(TextDirection.LeftToRight, TextDirectionDetector.Detect("Hello world"));
        Assert.Equal(TextDirection.Neutral, TextDirectionDetector.Detect(""));
        Assert.Equal(TextDirection.Neutral, TextDirectionDetector.Detect("   "));
    }

    [Fact]
    public void WPF_TextBox_FlowDirection_RightToLeft_PositionsArabicAtRightSide()
    {
        Exception? threadEx = null;
        double char0Left = -1;
        double char0X = -1;
        double tbWidth = -1;

        var thread = new Thread(() =>
        {
            try
            {
                var tb = new TextBox
                {
                    Width = 400,
                    Height = 100,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    FlowDirection = FlowDirection.RightToLeft,
                    Text = "مرحبا بالعالم"
                };

                var win = new Window
                {
                    Width = 500,
                    Height = 200,
                    Content = tb
                };

                win.Show();
                tb.UpdateLayout();

                Rect rect0 = tb.GetRectFromCharacterIndex(0);
                char0Left = rect0.Left;
                char0X = rect0.X;
                tbWidth = tb.ActualWidth;

                win.Close();
            }
            catch (Exception ex)
            {
                threadEx = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadEx != null) throw threadEx;

        _output.WriteLine($"[WPF RTL TEST] TextBox Width={tbWidth}, Char 0 Left={char0Left}, X={char0X}");

        // في فضاء إحداثيات WPF المعكوس لـ RightToLeft:
        // الحافة القائدة (Leading Edge) لليمين تقابل إحداثي X/Left صغير (~ 2 إلى 15 بكسل من الحافة المعكوسة)
        Assert.True(char0Left < 20, $"Expected char 0 to start at right leading edge (< 20px), but was {char0Left}");
    }

    [Fact]
    public void WPF_TextBox_TextAlignment_Under_RightToLeft_Analysis()
    {
        double posDefault = -1;
        double posAlignLeft = -1;
        double posAlignRight = -1;

        var thread = new Thread(() =>
        {
            var tbDefault = new TextBox { Width = 400, FlowDirection = FlowDirection.RightToLeft, Text = "تجربة" };
            var tbLeft = new TextBox { Width = 400, FlowDirection = FlowDirection.RightToLeft, TextAlignment = TextAlignment.Left, Text = "تجربة" };
            var tbRight = new TextBox { Width = 400, FlowDirection = FlowDirection.RightToLeft, TextAlignment = TextAlignment.Right, Text = "تجربة" };

            var sp = new StackPanel();
            sp.Children.Add(tbDefault);
            sp.Children.Add(tbLeft);
            sp.Children.Add(tbRight);

            var win = new Window { Width = 500, Height = 300, Content = sp };
            win.Show();
            tbDefault.UpdateLayout();
            tbLeft.UpdateLayout();
            tbRight.UpdateLayout();

            posDefault = tbDefault.GetRectFromCharacterIndex(0).Left;
            posAlignLeft = tbLeft.GetRectFromCharacterIndex(0).Left;
            posAlignRight = tbRight.GetRectFromCharacterIndex(0).Left;

            win.Close();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        _output.WriteLine($"[RTL Alignment] Default={posDefault}, TextAlignment.Left={posAlignLeft}, TextAlignment.Right={posAlignRight}");
    }

    [Fact]
    public void WPF_TextBox_PointToScreen_VisualPosition_Test()
    {
        double screenX_Rtl_AlignLeft = 0;
        double screenX_Rtl_AlignRight = 0;
        double screenX_Ltr_AlignLeft = 0;
        double tbScreenX = 0;

        var thread = new Thread(() =>
        {
            var tbRtlLeft = new TextBox { Width = 400, Height = 40, FlowDirection = FlowDirection.RightToLeft, TextAlignment = TextAlignment.Left, Text = "مرحبا" };
            var tbRtlRight = new TextBox { Width = 400, Height = 40, FlowDirection = FlowDirection.RightToLeft, TextAlignment = TextAlignment.Right, Text = "مرحبا" };
            var tbLtrLeft = new TextBox { Width = 400, Height = 40, FlowDirection = FlowDirection.LeftToRight, TextAlignment = TextAlignment.Left, Text = "Hello" };

            var sp = new StackPanel();
            sp.Children.Add(tbRtlLeft);
            sp.Children.Add(tbRtlRight);
            sp.Children.Add(tbLtrLeft);

            var win = new Window { Width = 500, Height = 300, Left = 100, Top = 100, Content = sp };
            win.Show();
            tbRtlLeft.UpdateLayout();
            tbRtlRight.UpdateLayout();
            tbLtrLeft.UpdateLayout();

            var rectRtlLeft = tbRtlLeft.GetRectFromCharacterIndex(0);
            var rectRtlRight = tbRtlRight.GetRectFromCharacterIndex(0);
            var rectLtrLeft = tbLtrLeft.GetRectFromCharacterIndex(0);

            screenX_Rtl_AlignLeft = tbRtlLeft.PointToScreen(rectRtlLeft.TopLeft).X;
            screenX_Rtl_AlignRight = tbRtlRight.PointToScreen(rectRtlRight.TopLeft).X;
            screenX_Ltr_AlignLeft = tbLtrLeft.PointToScreen(rectLtrLeft.TopLeft).X;
            tbScreenX = tbRtlLeft.PointToScreen(new Point(0, 0)).X;

            win.Close();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        _output.WriteLine($"TextBox Left Edge Screen X: {tbScreenX}");
        _output.WriteLine($"Screen X for [RTL + TextAlignment.Left]: {screenX_Rtl_AlignLeft}");
        _output.WriteLine($"Screen X for [RTL + TextAlignment.Right]: {screenX_Rtl_AlignRight}");
        _output.WriteLine($"Screen X for [LTR + TextAlignment.Left]: {screenX_Ltr_AlignLeft}");

        // التحقق القاطع: عند استخدام TextAlignment.Left مع RightToLeft، يكون النص في أقصى اليمين فيزيائياً
        Assert.True(screenX_Rtl_AlignLeft > screenX_Rtl_AlignRight, 
            "TextAlignment.Left under RightToLeft must be at the far right edge of the screen!");
    }

    [Fact]
    public void ArabicText_StartsAtPhysicalRightEdge_AndNotLeft()
    {
        double rtlChar0X = 0;
        double ltrChar0X = 0;

        var thread = new Thread(() =>
        {
            var tb = new TextBox
            {
                Width = 400,
                Height = 100,
                FlowDirection = FlowDirection.RightToLeft,
                TextAlignment = TextAlignment.Left,
                Text = "مرحبا بكم في تطبيق الترجمة"
            };

            var win = new Window { Width = 500, Height = 200, Content = tb };
            win.Show();
            tb.UpdateLayout();

            var rectChar0 = tb.GetRectFromCharacterIndex(0);
            rtlChar0X = tb.PointToScreen(rectChar0.TopLeft).X;

            // عند التبديل للإنجليزية
            tb.FlowDirection = FlowDirection.LeftToRight;
            tb.TextAlignment = TextAlignment.Left;
            tb.Text = "Welcome to translation app";
            tb.UpdateLayout();

            var rectEng0 = tb.GetRectFromCharacterIndex(0);
            ltrChar0X = tb.PointToScreen(rectEng0.TopLeft).X;

            win.Close();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        // الحرف العربي الأول يبدأ فيزيائياً من يمين الشاشة بفارق مسافة شاسع مقارنة بالإنجليزي
        Assert.True(rtlChar0X > ltrChar0X + 250, 
            $"Arabic text must start from the right edge! Got Arabic X={rtlChar0X}, English X={ltrChar0X}");
    }
}
