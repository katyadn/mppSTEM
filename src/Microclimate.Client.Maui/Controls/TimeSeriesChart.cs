using System.Globalization;
using Microclimate.Client.Core.Presentation;

namespace Microclimate.Client.Maui.Controls;

public sealed class TimeSeriesChart : GraphicsView, IDrawable
{
    public static readonly BindableProperty SegmentsProperty = BindableProperty.Create(
        nameof(Segments), typeof(IReadOnlyList<IReadOnlyList<ChartPoint>>), typeof(TimeSeriesChart),
        propertyChanged: (view, _, _) => ((TimeSeriesChart)view).Invalidate());
    public static readonly BindableProperty UnitProperty = BindableProperty.Create(
        nameof(Unit), typeof(string), typeof(TimeSeriesChart), string.Empty,
        propertyChanged: (view, _, _) => ((TimeSeriesChart)view).Invalidate());

    public TimeSeriesChart() => Drawable = this;

    public IReadOnlyList<IReadOnlyList<ChartPoint>>? Segments
    {
        get => (IReadOnlyList<IReadOnlyList<ChartPoint>>?)GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }
    public string Unit
    {
        get => (string)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var segments = Segments;
        if (segments is null || segments.Count == 0) return;
        var points = segments.SelectMany(segment => segment).ToArray();
        if (points.Length == 0 || dirtyRect.Width < 140 || dirtyRect.Height < 100) return;

        var start = points.Min(point => point.Time);
        var end = points.Max(point => point.Time);
        if (end == start) { start = start.AddMinutes(-30); end = end.AddMinutes(30); }
        var duration = (end - start).TotalSeconds;
        var minimum = points.Min(point => point.Value);
        var maximum = points.Max(point => point.Value);
        var span = maximum - minimum;
        var padding = span > 0 ? span * 0.08 : Math.Max(Math.Abs(minimum) * 0.05, 1);
        var step = TickStep((span + 2 * padding) / 4);
        var lower = Math.Floor((minimum - padding) / step) * step;
        var upper = Math.Ceiling((maximum + padding) / step) * step;
        var plot = new RectF(dirtyRect.Left + 62, dirtyRect.Top + 26,
            dirtyRect.Width - 80, dirtyRect.Height - 88);
        var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var labelColor = Color.FromArgb(dark ? "#D1CBDC" : "#6B6474");
        var gridColor = Color.FromArgb(dark ? "#3E374A" : "#EAE6F0");
        var tickCount = (int)Math.Round((upper - lower) / step);
        var decimals = Math.Clamp((int)Math.Ceiling(-Math.Log10(step)) + (step / Math.Pow(10, Math.Floor(Math.Log10(step))) == 2.5 ? 1 : 0), 0, 4);

        canvas.SaveState();
        canvas.FontSize = 10;
        canvas.FontColor = labelColor;
        canvas.StrokeSize = 1;
        canvas.StrokeColor = gridColor;
        for (var tick = 0; tick <= tickCount; tick++)
        {
            var value = lower + tick * step;
            if (Math.Abs(value) < step / 1000) value = 0;
            var y = plot.Bottom - (float)((value - lower) / (upper - lower)) * plot.Height;
            canvas.DrawLine(plot.Left, y, plot.Right, y);
            canvas.DrawString(value.ToString("F" + decimals, CultureInfo.InvariantCulture),
                dirtyRect.Left, y - 8, 52, 16, HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        var intervals = plot.Width < 360 ? 2 : 4;
        var showTime = (end - start).TotalDays < 2;
        for (var tick = 0; tick <= intervals; tick++)
        {
            var x = plot.Left + plot.Width * tick / intervals;
            var time = start.AddSeconds(duration * tick / intervals).ToLocalTime();
            canvas.DrawLine(x, plot.Top, x, plot.Bottom);
            var textX = Math.Clamp(x - 30, dirtyRect.Left, dirtyRect.Right - 60);
            canvas.DrawString(time.ToString(showTime ? "HH:mm" : "dd.MM", CultureInfo.InvariantCulture),
                textX, plot.Bottom + 6, 60, 16, HorizontalAlignment.Center, VerticalAlignment.Top);
            if (showTime)
                canvas.DrawString(time.ToString("dd.MM", CultureInfo.InvariantCulture),
                    textX, plot.Bottom + 21, 60, 16, HorizontalAlignment.Center, VerticalAlignment.Top);
        }
        canvas.FontSize = 11;
        canvas.DrawString(string.IsNullOrWhiteSpace(Unit) ? "Значення" : $"Значення, {Unit}",
            plot.Left, dirtyRect.Top, plot.Width, 18, HorizontalAlignment.Left, VerticalAlignment.Top);
        canvas.DrawString("Час (місцевий)", plot.Left, dirtyRect.Bottom - 18, plot.Width, 18,
            HorizontalAlignment.Right, VerticalAlignment.Top);

        canvas.StrokeColor = Colors.MediumPurple;
        canvas.FillColor = Colors.MediumPurple;
        canvas.StrokeSize = 2;
        foreach (var segment in segments)
        {
            PointF? previous = null;
            foreach (var point in segment)
            {
                var position = new PointF(
                    plot.Left + (float)((point.Time - start).TotalSeconds / duration) * plot.Width,
                    plot.Bottom - (float)((point.Value - lower) / (upper - lower)) * plot.Height);
                if (previous is { } last) canvas.DrawLine(last.X, last.Y, position.X, position.Y);
                canvas.FillCircle(position.X, position.Y, 3);
                previous = position;
            }
        }
        canvas.RestoreState();
    }

    private static double TickStep(double desired)
    {
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(desired)));
        var fraction = desired / magnitude;
        var rounded = fraction <= 1 ? 1 : fraction <= 2 ? 2 : fraction <= 2.5 ? 2.5 : fraction <= 5 ? 5 : 10;
        return rounded * magnitude;
    }
}
