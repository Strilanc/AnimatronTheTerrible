using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Shapes;
using TwistedOak.Util;
using LineSegment = SnipSnap.Mathematics.LineSegment;
using Animatron;

public sealed class LineSegmentDesc {
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
    public void Link(Line line, IObservable<TimeSpan> pulse, Lifetime life) {
        if (line == null) throw new ArgumentNullException("line");
        if (pulse == null) throw new ArgumentNullException("pulse");
        Pos.Select(e => e.Start.X).Watch(life, pulse, e => line.X1 = e);
        Pos.Select(e => e.End.X).Watch(life, pulse, e => line.X2 = e);
        Pos.Select(e => e.Start.Y).Watch(life, pulse, e => line.Y1 = e);
        Pos.Select(e => e.End.Y).Watch(life, pulse, e => line.Y2 = e);
        Stroke.Watch(life, pulse, e => line.Stroke = e);
        Thickness.Watch(life, pulse, e => line.StrokeThickness = e);
        Dashed.Watch(life, pulse, e => {
            line.StrokeDashArray.Clear();
            if (e != 0) line.StrokeDashArray.Add(e);
        });
    }
}