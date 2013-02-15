using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;
using Animatron;
using SnipSnap.Mathematics;
using TwistedOak.Util;
using Strilanc.LinqToCollections;
using LineSegment = SnipSnap.Mathematics.LineSegment;
using System.Linq;
using TwistedOak.Element.Env;

namespace Animations {
    public static class Unitaryness {
        private static void ShowArrow(this Animation animation,
                                      Lifetime life,
                                      IObservable<Point> start,
                                      IObservable<Vector> delta,
                                      IObservable<Brush> stroke = null,
                                      IObservable<double> thickness = null,
                                      IObservable<double> wedgeLength = null,
                                      IObservable<double> dashed = null) {
            wedgeLength = wedgeLength ?? 5.0.ToSingletonObservable();
            thickness = thickness ?? 1.0.ToSingletonObservable();
            animation.Points.Add(
                new PointDesc(start, fill: stroke, radius: thickness.Select(e => e * 1.25)),
                life);
            animation.Lines.Add(
                new LineSegmentDesc(
                    pos: start.CombineLatest(delta, (p, d) => new LineSegment(p, d)),
                    stroke: stroke,
                    thickness: thickness,
                    dashed: dashed),
                life);
            foreach (var s in new[] {+1, -1}) {
                animation.Lines.Add(
                    new LineSegmentDesc(
                        pos: start.CombineLatest(delta, wedgeLength, (p, d, w) => new LineSegment(p + d, (s*d.Perp().Normal() - d.Normal()).Normal()*Math.Min(w, d.Length/2))),
                        stroke: stroke,
                        thickness: thickness,
                        dashed: dashed),
                    life);
            }
        }
        private static void ShowComplex(this Animation animation,
                                        IObservable<Brush> fill,
                                        IObservable<Brush> valueStroke,
                                        IObservable<Complex> value,
                                        IObservable<Point> position,
                                        IObservable<double> unitRadius,
                                        Lifetime life,
                                        IObservable<Brush> valueGuideStroke = null) {
            // background unit circle
            animation.Points.Add(
                new PointDesc(
                    pos: position,
                    fill: fill,
                    radius: unitRadius),
                life);
            // dashed magnitude circle
            animation.Points.Add(
                new PointDesc(
                    pos: position,
                    radius: unitRadius.CombineLatest(value, (r, c) => r*c.Magnitude),
                    stroke: valueGuideStroke ?? valueStroke,
                    strokeThickness: 0.5.ToSingletonObservable(),
                    dashed: 4.0.ToSingletonObservable()),
                life);
            // dashed reference line along positive real axis
            animation.Lines.Add(
                new LineSegmentDesc(
                    value.CombineLatest(position, unitRadius, (v, p, r) => new LineSegment(p, p + new Vector(r*v.Magnitude, 0))),
                    dashed: 4.0.ToSingletonObservable(),
                    stroke: valueGuideStroke ?? valueStroke,
                    thickness: 0.5.ToSingletonObservable()), 
                life);
            // current value arrow
            animation.ShowArrow(life,
                                position,
                                value.CombineLatest(unitRadius, (v, r) => r*new Vector(v.Real, -v.Imaginary)),
                                stroke: valueStroke);
        }
        private static void ShowComplexProduct(this Animation animation,
                                               IObservable<Brush> fill,
                                               IObservable<SolidColorBrush> valueStroke1,
                                               IObservable<SolidColorBrush> valueStroke2,
                                               IObservable<Complex> value1,
                                               IObservable<Complex> value2,
                                               IObservable<Point> position,
                                               IObservable<double> unitRadius,
                                               IObservable<double> time,
                                               Lifetime life) {
            var x1 = time.CombineLatest(value1, value2, (t, v1, v2) => Complex.FromPolarCoordinates(
                v1.Magnitude.Ln().LerpTo(v1.Magnitude.Ln() + v2.Magnitude.Ln(), t.SmoothTransition(0, 0, 1)).Exp(),
                v1.Phase.LerpTo(v1.Phase + v2.Phase, t.SmoothTransition(0, 1, 1))));
            var x2 = time.CombineLatest(value1, value2, (t, v1, v2) => Complex.FromPolarCoordinates(
                v2.Magnitude.Ln().LerpTo(0, t.SmoothTransition(0, 0, 1)).Exp(),
                v2.Phase.LerpTo(0, t.SmoothTransition(0, 1, 1))));
            
            animation.ShowComplex(fill, valueStroke1, x1, position, unitRadius, life);
            animation.ShowComplex(Brushes.Transparent.ToSingletonObservable(),
                                  valueStroke2.CombineLatest(time, (s, t) => s.LerpToTransparent(t.LerpTransition(0, 0, 0, 1))),
                                  x2,
                                  position,
                                  unitRadius,
                                  life,
                                  valueStroke2.CombineLatest(time, (s, t) => s.LerpToTransparent(t * 10)));

            // original values
            //animation.ShowComplex(Brushes.Transparent.ToSingletonObservable(),
            //                      valueStroke1.Select(s => s.LerpToTransparent(0.8)),
            //                      value1,
            //                      position,
            //                      unitRadius,
            //                      life,
            //                      Brushes.Transparent.ToSingletonObservable());
            //animation.ShowComplex(Brushes.Transparent.ToSingletonObservable(),
            //                      valueStroke2.Select(s => s.LerpToTransparent(0.8)),
            //                      value2,
            //                      position,
            //                      unitRadius,
            //                      life,
            //                      Brushes.Transparent.ToSingletonObservable());
        }
        private static void ShowComplexSum(this Animation animation,
                                           IObservable<SolidColorBrush> fill,
                                           IObservable<SolidColorBrush> valueStroke,
                                           IReadOnlyList<IObservable<Complex>> values,
                                           IObservable<Point> position,
                                           IObservable<Vector> positionDelta,
                                           IObservable<double> unitRadius,
                                           IObservable<double> time,
                                           Lifetime life) {

            var sum = Complex.Zero.ToSingletonObservable();
            var target = position.CombineLatest(positionDelta, (p, d) => p + d*(values.Count-1)/2.0);
            animation.ShowComplex(fill.CombineLatest(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(1, 1, 0, 0, 0))),
                                  Brushes.Transparent.ToSingletonObservable(),
                                  Complex.Zero.ToSingletonObservable(),
                                  target,
                                  unitRadius,
                                  life,
                                  Brushes.Transparent.ToSingletonObservable());
            foreach (var i in values.Count.Range()) {
                animation.ShowComplex(fill.CombineLatest(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(0, 0, 1, 1, 1))),
                                      valueStroke.CombineLatest(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(0, 0.1, 0.1, 1, 1))),
                                      values[i],
                                      position.CombineLatest(positionDelta, time, sum, unitRadius, target, (p, d, t, s, u, p2) => (p + d * i).LerpTo(p2 + new Vector(s.Real * u, s.Imaginary * -u), t.SmoothTransition(0, 0, 1, 1, 1))),
                                      unitRadius,
                                      life,
                                      valueStroke.CombineLatest(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(0, 1, 1, 1, 1))));
                sum = sum.CombineLatest(values[i], (s, v) => s + v).Cache(life);
            }
            animation.ShowComplex(Brushes.Transparent.ToSingletonObservable(),
                                  valueStroke.CombineLatest(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(1, 1, 1, 0, 0))),
                                  sum,
                                  target,
                                  unitRadius,
                                  life,
                                  valueStroke.CombineLatest(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(1, 1, 1, 1, 0))));
        }
        public static Animation Animate(Lifetime life) {
            var animation = new Animation();

            var s = Math.Sqrt(0.5);
            var i = Complex.ImaginaryOne;
            var v = new ComplexVector(new[] {s, i*s, -s, -i*s});
            var rr = new Random();
            var u = ComplexMatrix.FromCellData(
                s, s*s + s*i, -s, s-i,
                0, s, i * s, s + i,
                0, i * s, s, s + i,
                0, i * s, s, s + i);
            Func<Complex> cc = () => new Complex(rr.NextDouble() - 0.5, rr.NextDouble() - 0.5)*Math.Sqrt(2);
            u = ComplexMatrix.FromCellData(64.Range().Select(_ => cc()).ToArray());
            v = new ComplexVector(8.Range().Select(_ => cc()).ToArray());

            var tt = animation.NextElapsedTime().Select(e => e.Times(2));
            var t1 = tt.Select(e => e.TotalSeconds.LerpCycle(0.0, 0.2, 0.4, 0.6, 0.8, 1.0, 1.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0));
            var x1 = tt.Select(e => e.TotalSeconds.LerpCycle(1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0));
            var t2 = tt.Select(e => e.TotalSeconds.LerpCycle(0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0, 1.0));
            var x2 = tt.Select(e => e.TotalSeconds.LerpCycle(0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0));
            foreach (var r in u.Rows.Count.Range()) {
                foreach (var c in u.Columns.Count.Range()) {
                    animation.ShowComplexProduct(x1.Select(t => Brushes.Orange.LerpToTransparent(1 - t)),
                                                 x1.Select(t => Brushes.Black.LerpToTransparent(1 - t)),
                                                 x1.Select(t => Brushes.Red.LerpToTransparent(1 - t)),
                                                 u.Rows[r][c].ToSingletonObservable(),
                                                 v.Values[c].ToSingletonObservable(),
                                                 new Point(50 + c*50, 50 + r*50).ToSingletonObservable(),
                                                 20.0.ToSingletonObservable(),
                                                 t1,
                                                 life);
                }
                animation.ShowComplexSum(x2.Select(t => Brushes.Orange.LerpToTransparent(1-t)),
                                         x2.Select(t => Brushes.Black.LerpToTransparent(1 - t)),
                                         u.Rows[r].Zip(v.Values, (v1, v2) => v1*v2).Select(e => e.ToSingletonObservable()),
                                         new Point(50, 50 + r * 50).ToSingletonObservable(),
                                         new Vector(50, 0).ToSingletonObservable(), 
                                         20.0.ToSingletonObservable(),
                                         t2,
                                         life);
            }

            return animation;
        }
    }
}