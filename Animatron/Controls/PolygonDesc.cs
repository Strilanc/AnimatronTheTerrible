using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using TwistedOak.Util;
using LineSegment = SnipSnap.Mathematics.LineSegment;
using Animatron;
using System.Reactive.Linq;
using System.Linq;

public sealed class PolygonDesc {
    public readonly IObservable<IEnumerable<Point>> Pos;
    public readonly IObservable<Brush> Stroke;
    public readonly IObservable<Brush> Fill;
    public readonly IObservable<double> StrokeThickness;
    public readonly IObservable<double> Dashed;
    public PolygonDesc(IObservable<IEnumerable<Point>> pos, IObservable<Brush> stroke = null, IObservable<double> strokeThickness = null, IObservable<double> dashed = null, IObservable<Brush> fill = null) {
        if (pos == null) throw new ArgumentNullException("pos");
        this.Pos = pos;
        this.Fill = fill ?? Brushes.Transparent.ToSingletonObservable();
        this.Stroke = stroke ?? Brushes.Transparent.ToSingletonObservable();
        this.StrokeThickness = strokeThickness ?? 0.0.ToSingletonObservable();
        this.Dashed = dashed ?? 0.0.ToSingletonObservable();
    }
    public void Link(Polygon polygon, Lifetime life) {
        Pos.DistinctUntilChanged(new SequenceEqualityComparer<Point>()).Subscribe(e => {
            polygon.Points.Clear();
            foreach (var x in e)
                polygon.Points.Add(x);
        }, life);
        Fill.DistinctUntilChanged().Subscribe(e => polygon.Fill = e, life);
        Stroke.DistinctUntilChanged().Subscribe(e => polygon.Stroke = e, life);
        StrokeThickness.DistinctUntilChanged().Subscribe(e => polygon.StrokeThickness = e, life);
        Dashed.DistinctUntilChanged().Subscribe(
            e => {
                polygon.StrokeDashArray.Clear();
                if (e != 0) polygon.StrokeDashArray.Add(e);
            },
            life);
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