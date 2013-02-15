using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using TwistedOak.Util;
using System.Reactive.Linq;
using Animatron;

public sealed class RectDesc {
    public readonly IObservable<Rect> Pos;
    public readonly IObservable<Brush> Stroke;
    public readonly IObservable<Brush> Fill;
    public readonly IObservable<double> StrokeThickness;
    public readonly IObservable<double> Dashed;
    public RectDesc(IObservable<Rect> pos, IObservable<Brush> stroke = null, IObservable<Brush> fill = null, IObservable<double> strokeThickness = null, IObservable<double> dashed = null) {
        if (pos == null) throw new ArgumentNullException("pos");
        this.Pos = pos;
        this.Stroke = stroke ?? Brushes.Transparent.ToSingletonObservable();
        this.Fill = fill ?? Brushes.Transparent.ToSingletonObservable();
        this.StrokeThickness = strokeThickness ?? 0.0.ToSingletonObservable();
        this.Dashed = dashed ?? 0.0.ToSingletonObservable();
    }
    public void Link(Rectangle rectangle, Lifetime life) {
        Pos.Select(e => e.X).DistinctUntilChanged().Subscribe(e => rectangle.SetValue(Canvas.LeftProperty, e), life);
        Pos.Select(e => e.Y).DistinctUntilChanged().Subscribe(e => rectangle.SetValue(Canvas.TopProperty, e), life);
        Pos.Select(e => e.Width).DistinctUntilChanged().Subscribe(e => rectangle.Width = e, life);
        Pos.Select(e => e.Height).DistinctUntilChanged().Subscribe(e => rectangle.Height = e, life);
        Fill.DistinctUntilChanged().Subscribe(e => rectangle.Fill = e, life);
        Stroke.DistinctUntilChanged().Subscribe(e => rectangle.Stroke = e, life);
        StrokeThickness.DistinctUntilChanged().Subscribe(e => rectangle.StrokeThickness = e, life);
        Dashed.DistinctUntilChanged().Subscribe(
            e => {
                rectangle.StrokeDashArray.Clear();
                if (e != 0) rectangle.StrokeDashArray.Add(e);
            },
            life);
    }
}