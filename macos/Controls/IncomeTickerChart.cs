using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Budget.Infrastructure;
using Budget.Models;

namespace Budget.Controls;

/// <summary>
/// Ticker-style line chart for the income history: hairline gridlines with clean
/// dollar ticks, a soft area wash under a 2px line, month labels along the bottom,
/// and a crosshair tooltip that snaps to the month nearest the pointer.
/// Avalonia port of the WPF control; the drawing logic is kept identical.
/// </summary>
public sealed class IncomeTickerChart : Control
{
    private const double PlotTopPadding = 16;
    private const double PlotRightPadding = 18;
    private const double AxisLabelGap = 10;
    private const double XAxisBandHeight = 26;
    private const double LabelFontSize = 11;
    private const int MaxPerPointDots = 24;

    private static readonly Typeface LabelTypeface = new(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);
    private static readonly Typeface ValueTypeface = new(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);

    private readonly List<Point> _renderedPoints = new();
    private int _hoverIndex = -1;

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<IncomeTickerChart, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<IBrush?> LineBrushProperty =
        AvaloniaProperty.Register<IncomeTickerChart, IBrush?>(nameof(LineBrush), Brushes.SteelBlue);

    public static readonly StyledProperty<IBrush?> GridLineBrushProperty =
        AvaloniaProperty.Register<IncomeTickerChart, IBrush?>(nameof(GridLineBrush), Brushes.Gainsboro);

    public static readonly StyledProperty<IBrush?> LabelBrushProperty =
        AvaloniaProperty.Register<IncomeTickerChart, IBrush?>(nameof(LabelBrush), Brushes.Gray);

    public static readonly StyledProperty<IBrush?> ValueBrushProperty =
        AvaloniaProperty.Register<IncomeTickerChart, IBrush?>(nameof(ValueBrush), Brushes.Black);

    public static readonly StyledProperty<IBrush?> SurfaceBrushProperty =
        AvaloniaProperty.Register<IncomeTickerChart, IBrush?>(nameof(SurfaceBrush), Brushes.White);

    static IncomeTickerChart()
    {
        AffectsRender<IncomeTickerChart>(
            ItemsSourceProperty,
            LineBrushProperty,
            GridLineBrushProperty,
            LabelBrushProperty,
            ValueBrushProperty,
            SurfaceBrushProperty);
    }

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Series color for the line, dots, and area wash.</summary>
    public IBrush? LineBrush
    {
        get => GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    /// <summary>Recessive hairline gridlines and the tooltip border.</summary>
    public IBrush? GridLineBrush
    {
        get => GetValue(GridLineBrushProperty);
        set => SetValue(GridLineBrushProperty, value);
    }

    /// <summary>Axis tick and month labels, and the crosshair.</summary>
    public IBrush? LabelBrush
    {
        get => GetValue(LabelBrushProperty);
        set => SetValue(LabelBrushProperty, value);
    }

    /// <summary>High-contrast text for the endpoint label and tooltip value.</summary>
    public IBrush? ValueBrush
    {
        get => GetValue(ValueBrushProperty);
        set => SetValue(ValueBrushProperty, value);
    }

    /// <summary>The card background behind the chart; used for marker rings and the tooltip fill.</summary>
    public IBrush? SurfaceBrush
    {
        get => GetValue(SurfaceBrushProperty);
        set => SetValue(SurfaceBrushProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != ItemsSourceProperty)
        {
            return;
        }

        if (change.OldValue is INotifyCollectionChanged oldSource)
        {
            oldSource.CollectionChanged -= OnItemsChanged;
        }

        if (change.NewValue is INotifyCollectionChanged newSource)
        {
            newSource.CollectionChanged += OnItemsChanged;
        }
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
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

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (_hoverIndex != -1)
        {
            _hoverIndex = -1;
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        _renderedPoints.Clear();

        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        // Transparent full-bounds rect so hover hit-testing covers the whole chart.
        context.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));

        var items = SnapshotItems();
        if (items.Count == 0)
        {
            return;
        }

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
            .Select(value => CreateText(FormatTick(value, step, culture), LabelTypeface, LabelFontSize, LabelBrush, culture))
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

        // Gridlines: solid hairlines one step off the surface, drawn on half-pixel
        // offsets so they stay crisp.
        var gridPen = new Pen(GridLineBrush, 1);
        for (var i = 0; i < tickValues.Count; i++)
        {
            var y = Math.Round(YFor(tickValues[i])) + 0.5;
            context.DrawLine(gridPen, new Point(plotLeft, y), new Point(plotRight, y));
            context.DrawText(tickLabels[i], new Point(plotLeft - AxisLabelGap - tickLabels[i].Width, y - (tickLabels[i].Height / 2)));
        }

        // Data positions, evenly spaced across the plot.
        for (var i = 0; i < items.Count; i++)
        {
            var x = items.Count == 1
                ? plotLeft + (plotWidth / 2)
                : plotLeft + (plotWidth * i / (items.Count - 1));
            _renderedPoints.Add(new Point(x, YFor((double)items[i].Amount)));
        }

        DrawMonthLabels(context, items, plotWidth, width, plotBottom, culture);

        var lineColor = LineBrush is ISolidColorBrush solidLine ? solidLine.Color : Colors.SteelBlue;

        if (_renderedPoints.Count > 1)
        {
            DrawAreaWash(context, lineColor, plotBottom);

            var linePen = new Pen(LineBrush, 2, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
            var lineGeometry = new StreamGeometry();
            using (var geometryContext = lineGeometry.Open())
            {
                geometryContext.BeginFigure(_renderedPoints[0], false);
                for (var i = 1; i < _renderedPoints.Count; i++)
                {
                    geometryContext.LineTo(_renderedPoints[i]);
                }

                geometryContext.EndFigure(false);
            }

            context.DrawGeometry(null, linePen, lineGeometry);
        }

        // Dots wear a 2px surface ring so they stay legible where they cross the line.
        if (_renderedPoints.Count <= MaxPerPointDots)
        {
            for (var i = 0; i < _renderedPoints.Count - 1; i++)
            {
                DrawDot(context, _renderedPoints[i], 4);
            }
        }

        DrawDot(context, _renderedPoints[^1], 5);
        DrawEndpointLabel(context, items[^1], plotTop, width, culture);

        if (_hoverIndex >= 0 && _hoverIndex < items.Count)
        {
            DrawHover(context, items[_hoverIndex], _renderedPoints[_hoverIndex], plotTop, plotBottom, width, culture);
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

    private void DrawMonthLabels(DrawingContext context, List<IncomeTrendItem> items, double plotWidth, double width, double plotBottom, CultureInfo culture)
    {
        var labels = items
            .Select(item => CreateText(item.MonthLabel, LabelTypeface, LabelFontSize, LabelBrush, culture))
            .ToList();

        var slotWidth = labels.Max(label => label.Width) + 18;
        var maxLabels = Math.Max(1, (int)(plotWidth / slotWidth));
        var step = (int)Math.Ceiling(items.Count / (double)maxLabels);

        // Walk backwards from the newest month so the right edge always keeps its label.
        for (var i = items.Count - 1; i >= 0; i -= step)
        {
            var label = labels[i];
            var x = Math.Clamp(_renderedPoints[i].X - (label.Width / 2), 0, width - label.Width);
            context.DrawText(label, new Point(x, plotBottom + 7));
        }
    }

    private void DrawAreaWash(DrawingContext context, Color lineColor, double plotBottom)
    {
        var areaGeometry = new StreamGeometry();
        using (var geometryContext = areaGeometry.Open())
        {
            geometryContext.BeginFigure(new Point(_renderedPoints[0].X, plotBottom), true);
            for (var i = 0; i < _renderedPoints.Count; i++)
            {
                geometryContext.LineTo(_renderedPoints[i]);
            }

            geometryContext.LineTo(new Point(_renderedPoints[^1].X, plotBottom));
            geometryContext.EndFigure(true);
        }

        var areaBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(0x2E, lineColor.R, lineColor.G, lineColor.B), 0),
                new GradientStop(Color.FromArgb(0x05, lineColor.R, lineColor.G, lineColor.B), 1)
            }
        };

        context.DrawGeometry(areaBrush, null, areaGeometry);
    }

    private void DrawDot(DrawingContext context, Point center, double radius)
    {
        context.DrawEllipse(SurfaceBrush, null, center, radius + 2, radius + 2);
        context.DrawEllipse(LineBrush, null, center, radius, radius);
    }

    private void DrawEndpointLabel(DrawingContext context, IncomeTrendItem item, double plotTop, double width, CultureInfo culture)
    {
        var label = CreateText(item.Amount.ToString("C0", culture), ValueTypeface, 12, ValueBrush, culture);
        var endpoint = _renderedPoints[^1];
        var x = endpoint.X + 11;
        if (x + label.Width > width - 2)
        {
            x = endpoint.X - 11 - label.Width;
        }

        var y = Math.Max(plotTop - 12, endpoint.Y - (label.Height / 2));
        context.DrawText(label, new Point(x, y));
    }

    private void DrawHover(DrawingContext context, IncomeTrendItem item, Point point, double plotTop, double plotBottom, double width, CultureInfo culture)
    {
        var crosshairColor = LabelBrush is ISolidColorBrush solidLabel ? solidLabel.Color : Colors.Gray;
        var crosshairPen = new Pen(
            new SolidColorBrush(Color.FromArgb(0x66, crosshairColor.R, crosshairColor.G, crosshairColor.B)),
            1);

        var crosshairX = Math.Round(point.X) + 0.5;
        context.DrawLine(crosshairPen, new Point(crosshairX, plotTop), new Point(crosshairX, plotBottom));

        DrawDot(context, point, 5);

        // Tooltip: the value leads, the month follows.
        var valueText = CreateText(MoneyText.Format(item.Amount), ValueTypeface, 13, ValueBrush, culture);
        var monthText = CreateText(item.MonthLabel, LabelTypeface, LabelFontSize, LabelBrush, culture);

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
        context.DrawRectangle(SurfaceBrush, borderPen, new Rect(boxX, boxY, boxWidth, boxHeight), 9, 9);
        context.DrawText(valueText, new Point(boxX + 12, boxY + 7));
        context.DrawText(monthText, new Point(boxX + 12, boxY + 10 + valueText.Height));
    }

    private static FormattedText CreateText(string text, Typeface typeface, double size, IBrush? brush, CultureInfo culture)
    {
        return new FormattedText(text, culture, FlowDirection.LeftToRight, typeface, size, brush);
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
