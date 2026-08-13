using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using FlowDirection = System.Windows.FlowDirection;

namespace MarketFloat;

public sealed class Sparkline : FrameworkElement
{
    public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
        nameof(Values), typeof(IReadOnlyList<double>), typeof(Sparkline),
        new FrameworkPropertyMetadata(Array.Empty<double>(), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        nameof(Stroke), typeof(System.Windows.Media.Brush), typeof(Sparkline),
        new FrameworkPropertyMetadata(System.Windows.Media.Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
        nameof(Range), typeof(MarketRange), typeof(Sparkline),
        new FrameworkPropertyMetadata(MarketRange.Day, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty PriceFormatProperty = DependencyProperty.Register(
        nameof(PriceFormat), typeof(string), typeof(Sparkline),
        new FrameworkPropertyMetadata("N0", FrameworkPropertyMetadataOptions.AffectsRender));

    public IReadOnlyList<double> Values { get => (IReadOnlyList<double>)GetValue(ValuesProperty); set => SetValue(ValuesProperty, value); }
    public System.Windows.Media.Brush Stroke { get => (System.Windows.Media.Brush)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }
    public MarketRange Range { get => (MarketRange)GetValue(RangeProperty); set => SetValue(RangeProperty, value); }
    public string PriceFormat { get => (string)GetValue(PriceFormatProperty); set => SetValue(PriceFormatProperty, value); }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var values = Values;
        if (values is null || values.Count < 2 || ActualWidth <= 0 || ActualHeight <= 0) return;

        var min = values.Min();
        var max = values.Max();
        var spread = Math.Max(max - min, Math.Max(Math.Abs(max) * 0.0005, 0.01));
        min -= spread * 0.08;
        max += spread * 0.08;
        spread = max - min;

        const double left = 4, right = 52, top = 5, bottom = 20;
        var plotWidth = Math.Max(1, ActualWidth - left - right);
        var plotHeight = Math.Max(1, ActualHeight - top - bottom);
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(35, 255, 255, 255)), 1);
        var labelBrush = new SolidColorBrush(Color.FromArgb(150, 205, 213, 224));

        for (var i = 0; i < 3; i++)
        {
            var ratio = i / 2d;
            var y = top + ratio * plotHeight;
            dc.DrawLine(gridPen, new Point(left, y), new Point(left + plotWidth, y));
            DrawText(dc, (max - ratio * spread).ToString(PriceFormat, CultureInfo.CurrentCulture),
                left + plotWidth + 5, y - 7, labelBrush);
        }

        var labels = XLabels();
        for (var i = 0; i < labels.Length; i++)
        {
            var ratio = i / (double)(labels.Length - 1);
            var x = left + ratio * plotWidth;
            dc.DrawLine(gridPen, new Point(x, top), new Point(x, top + plotHeight));
            var text = MakeText(labels[i], labelBrush);
            var textX = i == 0 ? x : i == labels.Length - 1 ? x - text.Width : x - text.Width / 2;
            dc.DrawText(text, new Point(textX, top + plotHeight + 4));
        }

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            for (var i = 0; i < values.Count; i++)
            {
                var x = left + i * plotWidth / (values.Count - 1);
                var y = top + (max - values[i]) / spread * plotHeight;
                if (i == 0) ctx.BeginFigure(new Point(x, y), false, false);
                else ctx.LineTo(new Point(x, y), true, false);
            }
        }
        geometry.Freeze();
        dc.DrawGeometry(null, new Pen(Stroke, 2.2) { LineJoin = PenLineJoin.Round }, geometry);
    }

    private string[] XLabels()
    {
        var now = DateTime.Now;
        return Range switch
        {
            MarketRange.Day => new[] { now.AddDays(-1).ToString("HH:mm"), now.AddHours(-12).ToString("HH:mm"), "现在" },
            MarketRange.Week => new[] { now.AddDays(-7).ToString("M/d"), now.AddDays(-3.5).ToString("M/d"), now.ToString("M/d") },
            MarketRange.Month => new[] { now.AddMonths(-1).ToString("M/d"), now.AddDays(-15).ToString("M/d"), now.ToString("M/d") },
            MarketRange.Year => new[] { now.AddYears(-1).ToString("yyyy/M"), now.AddMonths(-6).ToString("yyyy/M"), now.ToString("yyyy/M") },
            _ => new[] { "开始", "现在" }
        };
    }

    private static void DrawText(DrawingContext dc, string value, double x, double y, System.Windows.Media.Brush brush)
        => dc.DrawText(MakeText(value, brush), new Point(x, y));

    private static FormattedText MakeText(string value, System.Windows.Media.Brush brush)
    {
        var mainWindow = System.Windows.Application.Current?.MainWindow;
        var dpi = mainWindow is null ? 1d : VisualTreeHelper.GetDpi(mainWindow).PixelsPerDip;
        return new FormattedText(value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), 9, brush, dpi);
    }
}
