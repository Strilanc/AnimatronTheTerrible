using System;
using System.Windows.Media;
using System.Windows.Shapes;
using TwistedOak.Util;
using LineSegment = SnipSnap.Mathematics.LineSegment;
using Animatron;
using System.Reactive.Linq;

public sealed class LineSegmentDesc {
    public readonly IObservable<LineSegment> Pos;
    public readonly IObservable<Brush> Stroke;
    public readonly IObservable<double> Thickness;
    public readonly IObservable<bool> Dashed;
    public LineSegmentDesc(IObservable<LineSegment> pos, IObservable<Brush> stroke = null, IObservable<double> thickness = null, IObservable<bool> dashed = null) {
        if (pos == null) throw new ArgumentNullException("pos");
        this.Pos = pos;
        this.Stroke = stroke ?? Brushes.Black.ToSingletonObservable();
        this.Thickness = thickness ?? 1.0.ToSingletonObservable();
        this.Dashed = dashed ?? false.ToSingletonObservable();
    }
    public void Link(Line line, Lifetime life) {
        Pos.Select(e => e.Start.X).DistinctUntilChanged().Subscribe(e => line.X1 = e, life);
        Pos.Select(e => e.End.X).DistinctUntilChanged().Subscribe(e => line.X2 = e, life);
        Pos.Select(e => e.Start.Y).DistinctUntilChanged().Subscribe(e => line.Y1 = e, life);
        Pos.Select(e => e.End.Y).DistinctUntilChanged().Subscribe(e => line.Y2 = e, life);
        Stroke.DistinctUntilChanged().Subscribe(e => line.Stroke = e, life);
        Thickness.DistinctUntilChanged().Subscribe(e => line.StrokeThickness = e, life);
        Dashed.DistinctUntilChanged().Subscribe(
            e => {
                line.StrokeDashArray.Clear();
                if (e) line.StrokeDashArray.Add(1.0);
            },
            life);
    }
}