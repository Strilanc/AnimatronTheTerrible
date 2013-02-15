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
        private static IEnumerable<Vector> PhaseCurve(double phaseWidth, double r = 1, double startPhase = 0) {
            if (phaseWidth < 0) phaseWidth += Math.PI*2;
            yield return new Vector(0, 0);
            for (var d = 0.0; d <= phaseWidth; d += 1/r) {
                yield return r * new Vector(Math.Cos(d + startPhase), -Math.Sin(d + startPhase));
            }
        } 
        private static void ShowComplex(this Animation animation,
                                        IObservable<Brush> fill,
                                        IObservable<Brush> valueStroke,
                                        IObservable<Complex> value,
                                        IObservable<Point> position,
                                        IObservable<double> unitRadius,
                                        Lifetime life,
                                        IObservable<Brush> valueGuideStroke = null,
                                        IObservable<double> phaseOffset = null,
                                        IObservable<double> phaseRadius = null) {
            phaseOffset = phaseOffset ?? 0.0.ToSingletonObservable();
            phaseRadius = phaseRadius ?? value.Select(e => Math.Max(0.05, e.Magnitude));
            animation.Polygons.Add(
                new PolygonDesc(
                    phaseOffset.CombineLatest(value, position, unitRadius, phaseRadius, (o, v, p, r, f) => PhaseCurve(v.Phase, f * r, o).Select(e => e + p).ToArray()),
                    fill: fill
                    ),
                life); 
            // dashed unit circle
            animation.Points.Add(
                new PointDesc(
                    pos: position,
                    radius: unitRadius,
                    stroke: valueGuideStroke ?? valueStroke,
                    strokeThickness: 0.5.ToSingletonObservable(),
                    dashed: 4.0.ToSingletonObservable()),
                life);
            // current value arrow
            animation.ShowArrow(life,
                                position,
                                value.CombineLatest(phaseOffset, (e, p) => e*Complex.Exp(Complex.ImaginaryOne*p))
                                     .CombineLatest(unitRadius, (v, r) => r*new Vector(v.Real, -v.Imaginary)),
                                stroke: valueStroke);
        }
        private static void ShowComplexProduct(this Animation animation,
                                               IObservable<SolidColorBrush> fill1,
                                               IObservable<SolidColorBrush> fill2,
                                               IObservable<SolidColorBrush> valueStroke1,
                                               IObservable<SolidColorBrush> valueStroke2,
                                               IObservable<Complex> value1,
                                               IObservable<Complex> value2,
                                               IObservable<Point> position,
                                               IObservable<double> unitRadius,
                                               IObservable<double> time,
                                               Lifetime life) {
            animation.ShowComplex(fill1,
                                  valueStroke1,
                                  time.CombineLatest(value1, value2, (t, v1, v2) => Complex.FromPolarCoordinates(
                                      v1.Magnitude.LerpTo(v1.Magnitude * v2.Magnitude, t.SmoothTransition(0, 0, 0, 1)),
                                      v1.Phase + 0.0.LerpTo(v2.Phase.ProperMod(Math.PI * 2), t.SmoothTransition(0, 0, 0, 1)))),
                                  position,
                                  unitRadius,
                                  life,
                                  phaseOffset: time.CombineLatest(value2, (t, v) => t.SmoothTransition(0, 1, 1, 0)*v.Phase.ProperMod(Math.PI * 2)),
                                  phaseRadius: value1.CombineLatest(value2, time, (v1, v2, t) => v1.Magnitude.LerpTo(v2.Magnitude, 0.5).LerpTo(v1.Magnitude*v2.Magnitude, t.SmoothTransition(0,0,0,1))));
            animation.ShowComplex(fill2.CombineLatest(time, (s, t) => s.LerpToTransparent(t.LerpTransition(0, 0, 0, 1))),
                                  valueStroke2.CombineLatest(time, (s, t) => s.LerpToTransparent(t.LerpTransition(0, 0, 0, 1))),
                                  time.CombineLatest(value1, value2, (t, v1, v2) => Complex.FromPolarCoordinates(
                                      v2.Magnitude.LerpTo(0, t.SmoothTransition(0, 0, 0, 1)),
                                      v2.Phase.ProperMod(Math.PI * 2).LerpTo(0, t.SmoothTransition(0, 0, 0, 1)))),
                                  position,
                                  unitRadius,
                                  life,
                                  Brushes.Transparent.ToSingletonObservable(),
                                  phaseRadius: value1.CombineLatest(value2, (v1, v2) => v1.Magnitude.LerpTo(v2.Magnitude, 0.5) * 0.75));
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
            animation.ShowComplex(fill.CombineLatest(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(1, 1, 1, 0, 0))),
                                  valueStroke.CombineLatest(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(1, 1, 1, 0, 0))),
                                  sum,
                                  target,
                                  unitRadius,
                                  life,
                                  valueStroke.CombineLatest(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(1, 1, 0, 0, 0))));
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
            u = ComplexMatrix.FromCellData(16.Range().Select(_ => cc()).ToArray());
            v = new ComplexVector(4.Range().Select(_ => cc()).ToArray());

            foreach (var c in v.Values.Count.Range()) {
                var tt = animation.NextElapsedTime().Select(e => e.Times(3));
                var t1 = tt.Select(e => e.TotalSeconds.LerpCycle(0,0.33, 0.66, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
                var x1 = tt.Select(e => e.TotalSeconds.LerpCycle(1,0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1));
                animation.ShowComplex(x1.Select(t => Brushes.Blue.LerpToTransparent(0.7).LerpToTransparent(t)),
                                      x1.Select(t => Brushes.Black.LerpToTransparent(0.7).LerpToTransparent(t)),
                                      v.Values[c].ToSingletonObservable(),
                                      t1.Select(e => new Point(50 + c*100, 50 + (u.Rows.Count+1)*100*e)),
                                      50.0.ToSingletonObservable(),
                                      life);
            }
            foreach (var r in u.Rows.Count.Range()) {
                var tt = animation.NextElapsedTime().Select(e => e.Times(3)-(r*0.5).Seconds());
                var t1 = tt.Select(e => e.TotalSeconds.LerpCycle(0,0.0, 0.0, 0.2, 0.4, 0.6, 0.8, 1.0, 1.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0));
                var x1 = tt.Select(e => e.TotalSeconds.LerpCycle(1,1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0));
                var x1b = tt.Select(e => e.TotalSeconds.LerpCycle(0,0.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0));
                var t2 = tt.Select(e => e.TotalSeconds.LerpCycle(0,0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0, 1.0));
                var x2 = tt.Select(e => e.TotalSeconds.LerpCycle(0,0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0));
                foreach (var c in u.Columns.Count.Range()) {
                    animation.ShowComplexProduct(x1.Select(t => Brushes.Orange.LerpToTransparent(0.5).LerpToTransparent(1 - t)),
                                                 x1b.Select(t => Brushes.Blue.LerpToTransparent(0.7).LerpToTransparent(1 - t)),
                                                 x1.Select(t => Brushes.Black.LerpToTransparent(1 - t)),
                                                 x1b.Select(t => Brushes.Black.LerpToTransparent(1 - t)),
                                                 u.Rows[r][c].ToSingletonObservable(),
                                                 v.Values[c].ToSingletonObservable(),
                                                 new Point(50 + c*100, 150 + r*100).ToSingletonObservable(),
                                                 50.0.ToSingletonObservable(),
                                                 t1,
                                                 life);
                }
                animation.ShowComplexSum(x2.Select(t => Brushes.Orange.LerpToTransparent(0.5).LerpToTransparent(1 - t)),
                                         x2.Select(t => Brushes.Black.LerpToTransparent(1 - t)),
                                         u.Rows[r].Zip(v.Values, (v1, v2) => v1 * v2).Select(e => e.ToSingletonObservable()),
                                         new Point(50, 150 + r * 100).ToSingletonObservable(),
                                         new Vector(100, 0).ToSingletonObservable(),
                                         50.0.ToSingletonObservable(),
                                         t2,
                                         life);
            }

            return animation;
        }
    }
}