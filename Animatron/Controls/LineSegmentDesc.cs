using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using TwistedOak.Collections;
using TwistedOak.Util;
using LineSegment = SnipSnap.Mathematics.LineSegment;
using Animatron;

public sealed class LineSegmentDesc : IControlDescription<Line> {
    public readonly Ani<LineSegment> Pos;
    public readonly Ani<Brush> Stroke;
    public readonly Ani<double> Thickness;
    public readonly Ani<double> Dashed;
    public LineSegmentDesc(Ani<LineSegment> pos, Ani<Brush> stroke = null, Ani<double> thickness = null, Ani<double> dashed = null) {
        if (pos == null) throw new ArgumentNullException("pos");
        this.Pos = pos;
        this.Stroke = stroke ?? Brushes.Black;
        this.Thickness = thickness ?? 1;
        this.Dashed = dashed ?? 0;
    }
    public void Link(Line control, IObservable<TimeSpan> pulse, Lifetime life) {
        if (control == null) throw new ArgumentNullException("control");
        if (pulse == null) throw new ArgumentNullException("pulse");
        Pos.Select(e => e.Start.X).Watch(life, pulse, e => control.X1 = e);
        Pos.Select(e => e.End.X).Watch(life, pulse, e => control.X2 = e);
        Pos.Select(e => e.Start.Y).Watch(life, pulse, e => control.Y1 = e);
        Pos.Select(e => e.End.Y).Watch(life, pulse, e => control.Y2 = e);
        Stroke.Watch(life, pulse, e => control.Stroke = e);
        Thickness.Watch(life, pulse, e => control.StrokeThickness = e);
        Dashed.Watch(life, pulse, e => {
            control.StrokeDashArray.Clear();
            if (e != 0) control.StrokeDashArray.Add(e);
        });
    }
    public void Link(PerishableCollection<UIElement> controls, IObservable<TimeSpan> pulse, Lifetime life) {
        var r = new Line();
        Link(r, pulse, life);
        controls.Add(r, life);
    }
}