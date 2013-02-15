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
            // dashed reference line along positive real axis
            animation.Lines.Add(
                new LineSegmentDesc(
                    position.CombineLatest(unitRadius, (p, r) => new LineSegment(p, p + new Vector(r, 0))),
                    dashed: 4.0.ToSingletonObservable(),
                    stroke: valueGuideStroke ?? valueStroke,
                    thickness: 0.5.ToSingletonObservable()), 
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
                                      v1.Magnitude.LerpTo(v1.Magnitude * v2.Magnitude, t.SmoothTransition(0, 0, 1)),
                                      v1.Phase + 0.0.LerpTo(v2.Phase.ProperMod(Math.PI * 2), t.SmoothTransition(0, 0, 1)))),
                                  position,
                                  unitRadius,
                                  life,
                                  phaseOffset: time.CombineLatest(value2, (t, v) => t.SmoothTransition(0, 1, 0)*v.Phase.ProperMod(Math.PI * 2)));
            animation.ShowComplex(fill2.CombineLatest(time, (s, t) => s.LerpToTransparent(t.LerpTransition(0, 0, 1))),
                                  valueStroke2.CombineLatest(time, (s, t) => s.LerpToTransparent(t.LerpTransition(0, 0, 1))),
                                  time.CombineLatest(value1, value2, (t, v1, v2) => Complex.FromPolarCoordinates(
                                      v2.Magnitude.LerpTo(0, t.SmoothTransition(0, 0, 1)),
                                      v2.Phase.ProperMod(Math.PI * 2).LerpTo(0, t.SmoothTransition(0, 0, 1)))),
                                  position,
                                  unitRadius,
                                  life,
                                  Brushes.Transparent.ToSingletonObservable());
        }
        private static void ShowComplexSum(this Animation animation,
                                           IObservable<SolidColorBrush> fill1,
                                           IObservable<SolidColorBrush> fill2,
                                           IObservable<SolidColorBrush> valueStroke,
                                           IReadOnlyList<IObservable<Complex>> values,
                                           IObservable<Point> position,
                                           IObservable<Point> target,
                                           IObservable<Vector> positionDelta,
                                           IObservable<double> unitRadius,
                                           IObservable<double> time,
                                           Lifetime life) {

            var sum = Complex.Zero.ToSingletonObservable();
            foreach (var i in values.Count.Range()) {
                animation.ShowComplex(fill1.CombineLatest(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(0, 0, 1, 1, 1))),
                                      valueStroke.CombineLatest(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(0, 0.1, 0.1, 1, 1))),
                                      values[i],
                                      position.CombineLatest(positionDelta, time, sum, unitRadius, target, (p, d, t, s, u, p2) => (p + d * i).LerpTo(p2 + new Vector(s.Real * u, s.Imaginary * -u), t.SmoothTransition(0, 0, 1, 1, 1))),
                                      unitRadius,
                                      life,
                                      valueStroke.CombineLatest(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(0, 1, 1, 1, 1))));
                sum = sum.CombineLatest(values[i], (s, v) => s + v).Cache(life);
            }
            animation.ShowComplex(fill2.CombineLatest(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(1, 1, 0, 0, 0))),
                                  valueStroke.CombineLatest(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(1, 1, 1, 0, 0))),
                                  sum,
                                  target,
                                  unitRadius,
                                  life,
                                  valueStroke.CombineLatest(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(1, 1, 0, 0, 0))));
        }
        private static void HideShow(this Animation animation, IObservable<double> time, double offset, double activePeriod, Action<Lifetime, IObservable<double>> setup, Lifetime life) {
            animation.HideShow(time.Select(t => t.ProperMod(1) - offset).Select(t => t >= 0 && t < activePeriod),
                               li => setup(li, time.Select(e => ((e.ProperMod(1) - offset) / activePeriod).Clamp(0, 1))),
                               life);
        }
        private static void HideShow(this Animation animation, TimeSpan totalPeriod, TimeSpan offset, TimeSpan activePeriod, Action<Lifetime, IObservable<double>> setup, Lifetime life) {
            animation.HideShow(animation.NextElapsedTime().Select(t => t.Mod(totalPeriod) - offset).Select(t => t >= TimeSpan.Zero && t < activePeriod),
                               li => setup(li, animation.NextElapsedTime().Select(e => (e.Mod(totalPeriod) - offset).DividedBy(activePeriod).Clamp(0, 1))),
                               life);
        }
        private static void HideShow(this Animation animation, IObservable<bool> visible, Action<Lifetime> setup, Lifetime life) {
            var source = new LifetimeSource();
            visible.SkipWhile(e => !e).DistinctUntilChanged().Subscribe(e => {
                if (e) {
                    source = life.CreateDependentSource();
                    setup(source.Lifetime);
                } else {
                    source.EndLifetime();
                }
            }, life);
        }
        public static void ShowMatrixMultiplication(this Animation animation,
                                                    IObservable<double> time,
                                                    Rect pos,
                                                    ComplexMatrix u,
                                                    ComplexVector v,
                                                    IObservable<SolidColorBrush> or,
                                                    IObservable<SolidColorBrush> blu,
                                                    IObservable<SolidColorBrush> gre,
                                                    IObservable<SolidColorBrush> bla,
                                                    Lifetime life) {
            var d = Math.Min(pos.Width/(u.Columns.Count + 2), pos.Height/(u.Rows.Count + 2))/2;
            var ur = d.ToSingletonObservable();
            animation.Rects.Add(
                new RectDesc(new Rect(pos.X + d * 2, pos.Y + d * 2, pos.Width - d * 4, pos.Height - d * 4).ToSingletonObservable(),
                             Brushes.Black.ToSingletonObservable(),
                             strokeThickness: 0.6.ToSingletonObservable(),
                             dashed: 5.0.ToSingletonObservable()),
                life);
            animation.Rects.Add(
                new RectDesc(new Rect(pos.X, pos.Y + d * 2, d * 2, pos.Height - d * 4).ToSingletonObservable(),
                             Brushes.Black.ToSingletonObservable(),
                             strokeThickness: 0.6.ToSingletonObservable(),
                             dashed: 5.0.ToSingletonObservable()),
                life);
            animation.Rects.Add(
                new RectDesc(new Rect(pos.Right - d*2, pos.Y + d * 2, d * 2, pos.Height - d * 4).ToSingletonObservable(),
                             Brushes.Black.ToSingletonObservable(),
                             strokeThickness: 0.6.ToSingletonObservable(),
                             dashed: 5.0.ToSingletonObservable()),
                life);
            animation.HideShow(
                time,
                0,
                0.25,
                (li, pp) => {
                    foreach (var r in u.Rows.Count.Range()) {
                        // input vector
                        var p1 = new Point(d, d*3 + r*d*2);
                        var p2 = new Point(d*3 + r*d*2, d);
                        var p3 = new Point(d*3 + r*d*2, d + (u.Rows.Count + 1)*d*2);
                        animation.ShowComplex(pp.CombineLatest(blu, (p, b) => b.LerpToTransparent(p.SmoothTransition(0, 0, 1))),
                                              pp.CombineLatest(bla, (p, b) => b.LerpToTransparent(p.SmoothTransition(0, 0, 1))),
                                              v.Values[r].ToSingletonObservable(),
                                              pp.Select(p => p.SmoothTransition(p1, p1, p2, p3)),
                                              ur,
                                              li);
                        foreach (var c in u.Columns.Count.Range()) {
                            // vector copied into matrix
                            animation.ShowComplex(pp.CombineLatest(blu, (p, b) => b.LerpToTransparent(p.SmoothTransition(1, 1, 0))),
                                                  pp.CombineLatest(bla, (p, b) => b.LerpToTransparent(p.SmoothTransition(1, 1, 0))),
                                                  v.Values[c].ToSingletonObservable(),
                                                  (pos.TopLeft + new Vector(d * 3 + c * d * 2, d * 3 + r * d * 2)).ToSingletonObservable(),
                                                  ur,
                                                  li,
                                                  Brushes.Transparent.ToSingletonObservable());
                            // matrix
                            animation.ShowComplex(pp.CombineLatest(or, (p, b) => b.LerpToTransparent(p.SmoothTransition(0, 0, 0))),
                                                  pp.CombineLatest(bla, (p, b) => b.LerpToTransparent(p.SmoothTransition(0, 0, 0))),
                                                  u.Rows[r][c].ToSingletonObservable(),
                                                  (pos.TopLeft + new Vector(d*3 + c*d*2, d*3 + r*d*2)).ToSingletonObservable(),
                                                  ur,
                                                  li);
                        }
                    }
                },
                life);
            animation.HideShow(
                time,
                0.25,
                0.25,
                (li, pp) => {
                    foreach (var r in u.Rows.Count.Range()) {
                        foreach (var c in u.Columns.Count.Range()) {
                            animation.ShowComplexProduct(blu,
                                                         or,
                                                         bla,
                                                         bla,
                                                         v.Values[c].ToSingletonObservable(),
                                                         u.Rows[r][c].ToSingletonObservable(),
                                                         (pos.TopLeft + new Vector(d * 3 + c * d * 2, d * 3 + r * d * 2)).ToSingletonObservable(),
                                                         ur,
                                                         pp,
                                                         li);
                        }
                    }
                },
                life);
            animation.HideShow(
                time,
                0.5,
                0.4,
                (li, pp) => {
                    foreach (var r in u.Rows.Count.Range()) {
                        animation.ShowComplexSum(blu,
                                                 blu,
                                                 bla,
                                                 u.Rows[r].Count.Range()
                                                          .Select(e => u.Rows[r][e]*v.Values[e])
                                                          .Select(e => e.ToSingletonObservable()),
                                                 (pos.TopLeft + new Vector(d*3, d*3 + r*d*2)).ToSingletonObservable(),
                                                 (pos.TopLeft + new Vector(d*3 + u.Columns.Count*d*2, d*3 + r*d*2)).ToSingletonObservable(),
                                                 new Vector(d*2, 0).ToSingletonObservable(),
                                                 ur,
                                                 pp,
                                                 li);
                    }
                },
                life);
            animation.HideShow(
                time,
                0.9,
                0.1,
                (li, pp) => {
                    foreach (var r in u.Rows.Count.Range()) {
                        // solution
                        animation.ShowComplex(blu,
                                              bla,
                                              u.Rows[r].Count.Range().Select(e => u.Rows[r][e]*v.Values[e]).Sum().ToSingletonObservable(),
                                              pp.Select(p => (pos.TopLeft + new Vector(d*3 + u.Columns.Count*d*2, d*3 + r*d*2)).LerpTo(pos.TopLeft + new Vector(d, d*3+r*d*2), p.SmoothTransition(0,1,1))),
                                              ur,
                                              li);
                    }
                },
                life);
        }
        public static Animation Animate(Lifetime life) {
            var animation = new Animation();

            var s = Math.Sqrt(0.5);
            var i = Complex.ImaginaryOne;
            var z = i*s;
            var v = new ComplexVector(new[] { -s, 0, 0, i * s });
            var u = ComplexMatrix.FromCellData(
                0, s, z, 0,
                s, 0, 0, z,
                z, 0, 0, s,
                0, z, s, 0);

            var n = 8;
            var rr = new Random();
            Func<Complex> cc = () => new Complex(rr.NextDouble() - 0.5, rr.NextDouble() - 0.5)*1.05;
            u = ComplexMatrix.FromCellData((n * n).Range().Select(_ => cc()).ToArray());
            var uw = ComplexMatrix.FromCellData((n * n).Range().Select(_ => cc()).ToArray());
            var hw = ComplexMatrix.FromCellData((n * n).Range().Select(_ => cc()).ToArray());
            v = new ComplexVector(n.Range().Select(_ => cc()).ToArray());

            //uw = ComplexMatrix.FromCellData(
            //    (from c in n.Range()
            //     from r in n.Range()
            //     select r != c ? Complex.Zero : r == 1 ? -Complex.One : Complex.One).ToArray());
            //hw = ComplexMatrix.FromCellData(
            //    (from c in n.Range()
            //     from r in n.Range()
            //     select -2*Complex.One/n + (r == c ? 1 : 0)).ToArray());
            
            //u = uw*hw;
            //v = new ComplexVector(ReadOnlyList.Repeat(Complex.One/Math.Sqrt(n), n));
            var nr = (int)Math.Floor(Math.Sqrt(n))*6;
            var sp = 3.Seconds();
            foreach (var pi in nr.Range()) {
                var x1 = v;
                var m = pi%2 == 0 ? uw : hw;
                v *= m;
                animation.HideShow(
                    sp.Times(nr+1),
                    sp.Times(pi),
                    sp,
                    (li, tt) =>
                    animation.ShowMatrixMultiplication(
                        tt,
                        new Rect(0, 0, 500, 500),
                        m,
                        x1,
                        Brushes.Orange.LerpToTransparent(0.5).ToSingletonObservable(),
                        Brushes.Blue.LerpToTransparent(0.7).ToSingletonObservable(),
                        Brushes.Green.LerpToTransparent(0.5).ToSingletonObservable(),
                        Brushes.Black.ToSingletonObservable(),
                        li),
                    life);
            }
            return animation;
        }
    }
}