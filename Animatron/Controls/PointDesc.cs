using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using TwistedOak.Util;
using System.Reactive.Linq;
using Animatron;

public sealed class PointDesc {
    public readonly IObservable<Point> Pos;
    public readonly IObservable<Brush> Stroke;
    public readonly IObservable<Brush> Fill;
    public readonly IObservable<double> Radius;
    public readonly IObservable<double> StrokeThickness;
    public readonly IObservable<double> Dashed;
    public PointDesc(IObservable<Point> pos, IObservable<Brush> stroke = null, IObservable<Brush> fill = null, IObservable<double> radius = null, IObservable<double> strokeThickness = null, IObservable<double> dashed = null) {
        if (pos == null) throw new ArgumentNullException("pos");
        this.Pos = pos;
        this.Stroke = stroke ?? Brushes.Transparent.ToSingletonObservable();
        this.Fill = fill ?? Brushes.Transparent.ToSingletonObservable();
        this.Radius = radius ?? 1.0.ToSingletonObservable();
        this.StrokeThickness = strokeThickness ?? 0.0.ToSingletonObservable();
        this.Dashed = dashed ?? 0.0.ToSingletonObservable();
    }
    public void Link(Ellipse ellipse, Lifetime life) {
        var topLeft = Pos.CombineLatest(Radius, (p, r) => p - new Vector(r, r));
        topLeft.Select(e => e.X).DistinctUntilChanged().Subscribe(e => ellipse.SetValue(Canvas.LeftProperty, e), life);
        topLeft.Select(e => e.Y).DistinctUntilChanged().Subscribe(e => ellipse.SetValue(Canvas.TopProperty, e), life);
        Radius.DistinctUntilChanged().Subscribe(e => {
            ellipse.Width = e*2;
            ellipse.Height = e*2;
        }, life);
        Fill.DistinctUntilChanged().Subscribe(e => ellipse.Fill = e, life);
        Stroke.DistinctUntilChanged().Subscribe(e => ellipse.Stroke = e, life);
        StrokeThickness.DistinctUntilChanged().Subscribe(e => ellipse.StrokeThickness = e, life);
        Dashed.DistinctUntilChanged().Subscribe(
            e => {
                ellipse.StrokeDashArray.Clear();
                if (e != 0) ellipse.StrokeDashArray.Add(e);
            },
            life);
    }
}