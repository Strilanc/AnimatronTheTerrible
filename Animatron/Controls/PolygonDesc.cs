using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using TwistedOak.Util;
using Animatron;
using System.Linq;

public sealed class PolygonDesc {
    public readonly Ani<IEnumerable<Point>> Pos;
    public readonly Ani<Brush> Stroke;
    public readonly Ani<Brush> Fill;
    public readonly Ani<double> StrokeThickness;
    public readonly Ani<double> Dashed;
    public PolygonDesc(Ani<IEnumerable<Point>> pos, Ani<Brush> stroke = null, Ani<double> strokeThickness = null, Ani<double> dashed = null, Ani<Brush> fill = null) {
        if (pos == null) throw new ArgumentNullException("pos");
        this.Pos = pos;
        this.Fill = fill ?? Brushes.Transparent;
        this.Stroke = stroke ?? Brushes.Transparent;
        this.StrokeThickness = strokeThickness ?? 0;
        this.Dashed = dashed ?? 0;
    }
    public void Link(Polygon polygon, IObservable<TimeSpan> pulse, Lifetime life) {
        Pos.Watch(life,
                  pulse,
                  e => {
                      polygon.Points.Clear();
                      foreach (var x in e)
                          polygon.Points.Add(x);
                  },
                  new SequenceEqualityComparer<Point>());
        Fill.Watch(life, pulse, e => polygon.Fill = e);
        Stroke.Watch(life, pulse, e => polygon.Stroke = e);
        StrokeThickness.Watch(life, pulse, e => polygon.StrokeThickness = e);
        Dashed.Watch(life, pulse, e => {
            polygon.StrokeDashArray.Clear();
            if (e != 0) polygon.StrokeDashArray.Add(e);
        });
    }
    private struct SequenceEqualityComparer<T> : IEqualityComparer<IEnumerable<T>> {
        public bool Equals(IEnumerable<T> x, IEnumerable<T> y) {
            return x.SequenceEqual(y);
        }
        public int GetHashCode(IEnumerable<T> obj) {
            var n = 0;
            var h = 0;
            unchecked {
                foreach (var e in obj) {
                    h *= 3;
                    h ^= e.GetHashCode();
                    n += 1;
                }
                h += n;
            }
            return h;
        }
    }
}