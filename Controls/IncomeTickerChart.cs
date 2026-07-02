using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Budget.Infrastructure;
using Budget.Models;

namespace Budget.Controls;

/// <summary>
/// Ticker-style line chart for the income history: hairline gridlines with clean
/// dollar ticks, a soft area wash under a 2px line, month labels along the bottom,
/// and a crosshair tooltip that snaps to the month nearest the pointer.
/// </summary>
public sealed class IncomeTickerChart : FrameworkElement
{
    private const double PlotTopPadding = 16;
    private const double PlotRightPadding = 18;
    private const double AxisLabelGap = 10;
    private const double XAxisBandHeight = 26;
    private const double LabelFontSize = 11;
    private const int MaxPerPointDots = 24;

    private static readonly FontFamily ChartFontFamily = new("Segoe UI");
    private static readonly Typeface LabelTypeface = new(ChartFontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
    private static readonly Typeface ValueTypeface = new(ChartFontFamily, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

    private readonly List<Point> _renderedPoints = new();
    private int _hoverIndex = -1;

    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource),
        typeof(IEnumerable),
        typeof(IncomeTickerChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnItemsSourceChanged));

    public static readonly DependencyProperty LineBrushProperty = RegisterBrush(nameof(LineBrush), Brushes.SteelBlue);

    public static readonly DependencyProperty GridLineBrushProperty = RegisterBrush(nameof(GridLineBrush), Brushes.Gainsboro);

    public static readonly DependencyProperty LabelBrushProperty = RegisterBrush(nameof(LabelBrush), Brushes.Gray);

    public static readonly DependencyProperty ValueBrushProperty = RegisterBrush(nameof(ValueBrush), Brushes.Black);

    public static readonly DependencyProperty SurfaceBrushProperty = RegisterBrush(nameof(SurfaceBrush), Brushes.White);

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Series color for the line, dots, and area wash.</summary>
    public Brush LineBrush
    {
        get => (Brush)GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    /// <summary>Recessive hairline gridlines and the tooltip border.</summary>
    public Brush GridLineBrush
    {
        get => (Brush)GetValue(GridLineBrushProperty);
        set => SetValue(GridLineBrushProperty, value);
    }

    /// <summary>Axis tick and month labels, and the crosshair.</summary>
    public Brush LabelBrush
    {
        get => (Brush)GetValue(LabelBrushProperty);
        set => SetValue(LabelBrushProperty, value);
    }

    /// <summary>High-contrast text for the endpoint label and tooltip value.</summary>
    public Brush ValueBrush
    {
        get => (Brush)GetValue(ValueBrushProperty);
        set => SetValue(ValueBrushProperty, value);
    }

    /// <summary>The card background behind the chart; used for marker rings and the tooltip fill.</summary>
    public Brush SurfaceBrush
    {
        get => (Brush)GetValue(SurfaceBrushProperty);
        set => SetValue(SurfaceBrushProperty, value);
    }

    private static DependencyProperty RegisterBrush(string name, Brush fallback)
    {
        return DependencyProperty.Register(
            name,
            typeof(Brush),
            typeof(IncomeTickerChart),
            new FrameworkPropertyMetadata(fallback, FrameworkPropertyMetadataOptions.AffectsRender));
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var chart = (IncomeTickerChart)d;
        if (e.OldValue is INotifyCollectionChanged oldSource)
        {
            oldSource.CollectionChanged -= chart.OnItemsChanged;
        }

        if (e.NewValue is INotifyCollectionChanged newSource)
        {
            newSource.CollectionChanged += chart.OnItemsChanged;
        }
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_renderedPoints.Count == 0)
        {
            return;
        }

        var position = e.GetPosition(this);
        var nearest = 0;
        var nearestDistance = double.MaxValue;
        for (var i = 0; i < _renderedPoints.Count; i++)
        {
            var distance = Math.Abs(_renderedPoints[i].X - position.X);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = i;
            }
        }

        if (nearest != _hoverIndex)
        {
            _hoverIndex = nearest;
            InvalidateVisual();
        }
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoverIndex != -1)
        {
            _hoverIndex = -1;
            InvalidateVisual();
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        _renderedPoints.Clear();

        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        // Transparent full-bounds rect so hover hit-testing covers the whole chart.
        drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));

        var items = SnapshotItems();
        if (items.Count == 0)
        {
            return;
        }

        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var culture = CultureInfo.CurrentCulture;

        var minAmount = (double)items.Min(item => item.Amount);
        var maxAmount = (double)items.Max(item => item.Amount);
        var (axisMin, axisMax, step) = ComputeAxis(minAmount, maxAmount);

        var tickValues = new List<double>();
        for (var value = axisMin; value <= axisMax + (step * 0.001); value += step)
        {
            tickValues.Add(value);
        }

        var tickLabels = tickValues
            .Select(value => CreateText(FormatTick(value, step, culture), LabelTypeface, LabelFontSize, LabelBrush, pixelsPerDip, culture))
            .ToList();

        var plotLeft = tickLabels.Max(label => label.Width) + AxisLabelGap;
        var plotTop = PlotTopPadding;
        var plotRight = width - PlotRightPadding;
        var plotBottom = height - XAxisBandHeight;
        var plotWidth = plotRight - plotLeft;
        var plotHeight = plotBottom - plotTop;
        if (plotWidth < 40 || plotHeight < 30)
        {
            return;
        }

        double YFor(double value) => plotBottom - ((value - axisMin) / (axisMax - axisMin) * plotHeight);

        // Gridlines: solid hairlines one step off the surface, with crisp pixel snapping.
        var gridPen = new Pen(GridLineBrush, 1);
        gridPen.Freeze();
        var guidelines = new GuidelineSet();
        foreach (var value in tickValues)
        {
            guidelines.GuidelinesY.Add(YFor(value) + 0.5);
        }

        drawingContext.PushGuidelineSet(guidelines);
        for (var i = 0; i < tickValues.Count; i++)
        {
            var y = YFor(tickValues[i]);
            drawingContext.DrawLine(gridPen, new Point(plotLeft, y), new Point(plotRight, y));
            drawingContext.DrawText(tickLabels[i], new Point(plotLeft - AxisLabelGap - tickLabels[i].Width, y - (tickLabels[i].Height / 2)));
        }

        drawingContext.Pop();

        // Data positions, evenly spaced across the plot.
        for (var i = 0; i < items.Count; i++)
        {
            var x = items.Count == 1
                ? plotLeft + (plotWidth / 2)
                : plotLeft + (plotWidth * i / (items.Count - 1));
            _renderedPoints.Add(new Point(x, YFor((double)items[i].Amount)));
        }

        DrawMonthLabels(drawingContext, items, plotWidth, width, plotBottom, pixelsPerDip, culture);

        var lineColor = LineBrush is SolidColorBrush solidLine ? solidLine.Color : Colors.SteelBlue;

        if (_renderedPoints.Count > 1)
        {
            DrawAreaWash(drawingContext, lineColor, plotBottom);

            var linePen = new Pen(LineBrush, 2)
            {
                LineJoin = PenLineJoin.Round,
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            linePen.Freeze();

            var lineGeometry = new StreamGeometry();
            using (var context = lineGeometry.Open())
            {
                context.BeginFigure(_renderedPoints[0], false, false);
                context.PolyLineTo(_renderedPoints.Skip(1).ToList(), true, true);
            }

            lineGeometry.Freeze();
            drawingContext.DrawGeometry(null, linePen, lineGeometry);
        }

        // Dots wear a 2px surface ring so they stay legible where they cross the line.
        if (_renderedPoints.Count <= MaxPerPointDots)
        {
            for (var i = 0; i < _renderedPoints.Count - 1; i++)
            {
                DrawDot(drawingContext, _renderedPoints[i], 4);
            }
        }

        DrawDot(drawingContext, _renderedPoints[^1], 5);
        DrawEndpointLabel(drawingContext, items[^1], plotTop, width, pixelsPerDip, culture);

        if (_hoverIndex >= 0 && _hoverIndex < items.Count)
        {
            DrawHover(drawingContext, items[_hoverIndex], _renderedPoints[_hoverIndex], plotTop, plotBottom, width, pixelsPerDip, culture);
        }
    }

    private List<IncomeTrendItem> SnapshotItems()
    {
        var items = new List<IncomeTrendItem>();
        if (ItemsSource is null)
        {
            return items;
        }

        foreach (var entry in ItemsSource)
        {
            if (entry is IncomeTrendItem item)
            {
                items.Add(item);
            }
        }

        return items;
    }

    private void DrawMonthLabels(DrawingContext drawingContext, List<IncomeTrendItem> items, double plotWidth, double width, double plotBottom, double pixelsPerDip, CultureInfo culture)
    {
        var labels = items
            .Select(item => CreateText(item.MonthLabel, LabelTypeface, LabelFontSize, LabelBrush, pixelsPerDip, culture))
            .ToList();

        var slotWidth = labels.Max(label => label.Width) + 18;
        var maxLabels = Math.Max(1, (int)(plotWidth / slotWidth));
        var step = (int)Math.Ceiling(items.Count / (double)maxLabels);

        // Walk backwards from the newest month so the right edge always keeps its label.
        for (var i = items.Count - 1; i >= 0; i -= step)
        {
            var label = labels[i];
            var x = Math.Clamp(_renderedPoints[i].X - (label.Width / 2), 0, width - label.Width);
            drawingContext.DrawText(label, new Point(x, plotBottom + 7));
        }
    }

    private void DrawAreaWash(DrawingContext drawingContext, Color lineColor, double plotBottom)
    {
        var areaGeometry = new StreamGeometry();
        using (var context = areaGeometry.Open())
        {
            context.BeginFigure(new Point(_renderedPoints[0].X, plotBottom), true, true);
            context.LineTo(_renderedPoints[0], false, false);
            context.PolyLineTo(_renderedPoints.Skip(1).ToList(), false, false);
            context.LineTo(new Point(_renderedPoints[^1].X, plotBottom), false, false);
        }

        areaGeometry.Freeze();

        var areaBrush = new LinearGradientBrush(
            Color.FromArgb(0x2E, lineColor.R, lineColor.G, lineColor.B),
            Color.FromArgb(0x05, lineColor.R, lineColor.G, lineColor.B),
            new Point(0.5, 0),
            new Point(0.5, 1));
        areaBrush.Freeze();
        drawingContext.DrawGeometry(areaBrush, null, areaGeometry);
    }

    private void DrawDot(DrawingContext drawingContext, Point center, double radius)
    {
        drawingContext.DrawEllipse(SurfaceBrush, null, center, radius + 2, radius + 2);
        drawingContext.DrawEllipse(LineBrush, null, center, radius, radius);
    }

    private void DrawEndpointLabel(DrawingContext drawingContext, IncomeTrendItem item, double plotTop, double width, double pixelsPerDip, CultureInfo culture)
    {
        var label = CreateText(item.Amount.ToString("C0", culture), ValueTypeface, 12, ValueBrush, pixelsPerDip, culture);
        var endpoint = _renderedPoints[^1];
        var x = endpoint.X + 11;
        if (x + label.Width > width - 2)
        {
            x = endpoint.X - 11 - label.Width;
        }

        var y = Math.Max(plotTop - 12, endpoint.Y - (label.Height / 2));
        drawingContext.DrawText(label, new Point(x, y));
    }

    private void DrawHover(DrawingContext drawingContext, IncomeTrendItem item, Point point, double plotTop, double plotBottom, double width, double pixelsPerDip, CultureInfo culture)
    {
        var crosshairColor = LabelBrush is SolidColorBrush solidLabel ? solidLabel.Color : Colors.Gray;
        var crosshairPen = new Pen(new SolidColorBrush(Color.FromArgb(0x66, crosshairColor.R, crosshairColor.G, crosshairColor.B)), 1);
        crosshairPen.Freeze();

        var crosshairGuidelines = new GuidelineSet();
        crosshairGuidelines.GuidelinesX.Add(point.X + 0.5);
        drawingContext.PushGuidelineSet(crosshairGuidelines);
        drawingContext.DrawLine(crosshairPen, new Point(point.X, plotTop), new Point(point.X, plotBottom));
        drawingContext.Pop();

        DrawDot(drawingContext, point, 5);

        // Tooltip: the value leads, the month follows.
        var valueText = CreateText(MoneyText.Format(item.Amount), ValueTypeface, 13, ValueBrush, pixelsPerDip, culture);
        var monthText = CreateText(item.MonthLabel, LabelTypeface, LabelFontSize, LabelBrush, pixelsPerDip, culture);

        var boxWidth = Math.Max(valueText.Width, monthText.Width) + 24;
        var boxHeight = valueText.Height + monthText.Height + 17;

        var boxX = point.X + 14;
        if (boxX + boxWidth > width - 2)
        {
            boxX = point.X - 14 - boxWidth;
        }

        var boxY = point.Y - boxHeight - 12;
        if (boxY < 2)
        {
            boxY = point.Y + 12;
        }

        var borderPen = new Pen(GridLineBrush, 1);
        borderPen.Freeze();
        drawingContext.DrawRoundedRectangle(SurfaceBrush, borderPen, new Rect(boxX, boxY, boxWidth, boxHeight), 9, 9);
        drawingContext.DrawText(valueText, new Point(boxX + 12, boxY + 7));
        drawingContext.DrawText(monthText, new Point(boxX + 12, boxY + 10 + valueText.Height));
    }

    private static FormattedText CreateText(string text, Typeface typeface, double size, Brush brush, double pixelsPerDip, CultureInfo culture)
    {
        return new FormattedText(text, culture, FlowDirection.LeftToRight, typeface, size, brush, pixelsPerDip);
    }

    /// <summary>Expands the data range to padded "nice" bounds with a clean tick step.</summary>
    private static (double Min, double Max, double Step) ComputeAxis(double min, double max)
    {
        if (max - min < 0.01)
        {
            var spread = Math.Max(Math.Abs(max) * 0.1, 1);
            min -= spread;
            max += spread;
        }

        var padding = (max - min) * 0.08;
        max += padding;
        var paddedMin = min - padding;
        // Income is never negative, so don't pad the floor below zero.
        min = min >= 0 ? Math.Max(0, paddedMin) : paddedMin;

        var step = NiceStep((max - min) / 4);
        var axisMin = Math.Floor(min / step) * step;
        var axisMax = Math.Ceiling(max / step) * step;
        if (axisMax <= axisMin)
        {
            axisMax = axisMin + step;
        }

        return (axisMin, axisMax, step);
    }

    private static double NiceStep(double roughStep)
    {
        var power = Math.Pow(10, Math.Floor(Math.Log10(Math.Max(roughStep, 0.01))));
        var fraction = roughStep / power;
        var nice = fraction <= 1 ? 1 : fraction <= 2 ? 2 : fraction <= 2.5 ? 2.5 : fraction <= 5 ? 5 : 10;
        return nice * power;
    }

    private static string FormatTick(double value, double step, CultureInfo culture)
    {
        var decimals = step >= 1 && step == Math.Floor(step) ? 0 : 2;
        return value.ToString($"C{decimals}", culture);
    }
}
