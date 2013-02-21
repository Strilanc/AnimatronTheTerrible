using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using TwistedOak.Collections;
using TwistedOak.Util;
using Animatron;

public sealed class PointDesc : IControlDescription<Ellipse> {
    public readonly Ani<Point> Pos;
    public readonly Ani<Brush> Stroke;
    public readonly Ani<Brush> Fill;
    public readonly Ani<double> Radius;
    public readonly Ani<double> StrokeThickness;
    public readonly Ani<double> Dashed;
    public PointDesc(Ani<Point> pos, Ani<Brush> stroke = null, Ani<Brush> fill = null, Ani<double> radius = null, Ani<double> strokeThickness = null, Ani<double> dashed = null) {
        if (pos == null) throw new ArgumentNullException("pos");
        this.Pos = pos;
        this.Stroke = stroke ?? Brushes.Transparent;
        this.Fill = fill ?? Brushes.Transparent;
        this.Radius = radius ?? 1;
        this.StrokeThickness = strokeThickness ?? 0;
        this.Dashed = dashed ?? 0;
    }
    public void Link(Ellipse ellipse, IObservable<TimeSpan> pulse, Lifetime life) {
        var topLeft = Pos.Combine(Radius, (p, r) => p - new Vector(r, r));
        topLeft.Select(e => e.X).Watch(life, pulse, e => ellipse.SetValue(Canvas.LeftProperty, e));
        topLeft.Select(e => e.Y).Watch(life, pulse, e => ellipse.SetValue(Canvas.TopProperty, e));
        Radius.Watch(life, pulse, e => {
            ellipse.Width = e*2;
            ellipse.Height = e*2;
        });
        Fill.Watch(life, pulse, e => ellipse.Fill = e);
        Stroke.Watch(life, pulse, e => ellipse.Stroke = e);
        StrokeThickness.Watch(life, pulse, e => ellipse.StrokeThickness = e);
        Dashed.Watch(life, pulse, e => {
            ellipse.StrokeDashArray.Clear();
            if (e != 0) ellipse.StrokeDashArray.Add(e);
        });
    }
    public void Link(PerishableCollection<UIElement> controls, IObservable<TimeSpan> pulse, Lifetime life) {
        var r = new Ellipse();
        Link(r, pulse, life);
        controls.Add(r, life);
    }
}