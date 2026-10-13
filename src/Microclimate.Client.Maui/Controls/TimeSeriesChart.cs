using Microclimate.Client.Core.Presentation;

namespace Microclimate.Client.Maui.Controls;

public sealed class TimeSeriesChart : GraphicsView, IDrawable
{
    public static readonly BindableProperty SegmentsProperty = BindableProperty.Create(
        nameof(Segments), typeof(IReadOnlyList<IReadOnlyList<ChartPoint>>), typeof(TimeSeriesChart),
        propertyChanged: (view, _, _) => ((TimeSeriesChart)view).Invalidate());

    public TimeSeriesChart() => Drawable = this;

    public IReadOnlyList<IReadOnlyList<ChartPoint>>? Segments
    {
        get => (IReadOnlyList<IReadOnlyList<ChartPoint>>?)GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var segments = Segments;
        if (segments is null || segments.Count == 0)
        {
            return;
        }

        var points = segments.SelectMany(segment => segment).ToArray();
        if (points.Length == 0)
        {
            return;
        }

        var start = points.Min(point => point.Time);
        var duration = Math.Max(1, (points.Max(point => point.Time) - start).TotalSeconds);
        var min = points.Min(point => point.Value);
        var range = Math.Max(1, points.Max(point => point.Value) - min);
        const float padding = 12;
        var width = Math.Max(1, dirtyRect.Width - padding * 2);
        var height = Math.Max(1, dirtyRect.Height - padding * 2);
        canvas.StrokeColor = Colors.MediumPurple;
        canvas.FillColor = Colors.MediumPurple;
        canvas.StrokeSize = 2;

        foreach (var segment in segments)
        {
            PointF? previous = null;
            foreach (var point in segment)
            {
                var position = new PointF(
                    dirtyRect.Left + padding + (float)((point.Time - start).TotalSeconds / duration) * width,
                    dirtyRect.Top + padding + height - (float)((point.Value - min) / range) * height);
                if (previous is { } last)
                {
                    canvas.DrawLine(last.X, last.Y, position.X, position.Y);
                }

                canvas.FillCircle(position.X, position.Y, 3);
                previous = position;
            }
        }
    }
}
