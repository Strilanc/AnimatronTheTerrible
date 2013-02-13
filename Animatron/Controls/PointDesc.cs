using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using TwistedOak.Util;
using System.Reactive.Linq;

public sealed class PointDesc {
    public readonly IObservable<Point> Pos;
    public readonly IObservable<Brush> Stroke;
    public readonly IObservable<Brush> Fill;
    public readonly IObservable<double> Radius;
    public readonly IObservable<double> StrokeThickness;
    public PointDesc(IObservable<Point> pos, IObservable<Brush> stroke, IObservable<Brush> fill, IObservable<double> radius, IObservable<double> strokeThickness) {
        if (pos == null) throw new ArgumentNullException("pos");
        if (stroke == null) throw new ArgumentNullException("stroke");
        if (fill == null) throw new ArgumentNullException("fill");
        if (radius == null) throw new ArgumentNullException("radius");
        if (strokeThickness == null) throw new ArgumentNullException("strokeThickness");
        this.Pos = pos;
        this.Stroke = stroke;
        this.Fill = fill;
        this.Radius = radius;
        this.StrokeThickness = strokeThickness;
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
    }
}