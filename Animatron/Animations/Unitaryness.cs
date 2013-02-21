using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;
using Animatron;
using SnipSnap.Mathematics;
using Strilanc.Angle;
using TwistedOak.Util;
using Strilanc.LinqToCollections;
using LineSegment = SnipSnap.Mathematics.LineSegment;
using System.Linq;
using TwistedOak.Element.Env;

namespace Animations {
    public static class Unitaryness {
        private static void ShowArrow(this Animation animation,
                                      Lifetime life,
                                      Ani<Point> start,
                                      Ani<Vector> delta,
                                      Ani<Brush> stroke = null,
                                      Ani<double> thickness = null,
                                      Ani<double> wedgeLength = null,
                                      Ani<double> dashed = null) {
            wedgeLength = wedgeLength ?? 5;
            thickness = thickness ?? 1;
            animation.Points.Add(
                new PointDesc(start, fill: stroke, radius: thickness.Select(e => e * 1.25)),
                life);
            animation.Lines.Add(
                new LineSegmentDesc(
                    pos: start.Combine(delta, (p, d) => new LineSegment(p, d)),
                    stroke: stroke,
                    thickness: thickness,
                    dashed: dashed),
                life);
            foreach (var s in new[] {+1, -1}) {
                animation.Lines.Add(
                    new LineSegmentDesc(
                        pos: start.Combine(delta, wedgeLength, (p, d, w) => new LineSegment(p + d, (s*d.Perp().Normal() - d.Normal()).Normal()*Math.Min(w, d.Length/2))),
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
                                        Ani<Brush> fill,
                                        Ani<Brush> valueStroke,
                                        Ani<Complex> value,
                                        Ani<Point> position,
                                        Ani<double> unitRadius,
                                        Lifetime life,
                                        Ani<Brush> valueGuideStroke = null,
                                        Ani<double> phaseOffset = null,
                                        Ani<double> phaseRadius = null,
                                        Ani<Turn> rotation = null,
                                        Ani<Brush> sweepFill = null,
                                        Ani<double> squish = null) {
            phaseOffset = phaseOffset ?? 0;
            phaseRadius = phaseRadius ?? value.Select(e => Math.Max(0.05, e.Magnitude));
            rotation = rotation ?? Turn.Zero;
            sweepFill = sweepFill ?? fill;
            squish = squish ?? 1.0;
            animation.Polygons.Add(
                new PolygonDesc(
                    phaseOffset.Combine(value, position, unitRadius, phaseRadius, (o, v, p, r, f) => PhaseCurve(v.Phase, f * r, o).Select(e => e + p).ToArray().AsEnumerable()),
                    fill: sweepFill
                    ),
                life); 
            // dashed unit circle
            //animation.Points.Add(
            //    new PointDesc(
            //        pos: position,
            //        radius: unitRadius,
            //        stroke: valueGuideStroke ?? valueStroke,
            //        strokeThickness: 0.5,
            //        dashed: 4),
            //    life);
            // dashed unit rect
            animation.Rects.Add(
                new RectDesc(
                    pos: position.Combine(unitRadius, (e, r) => new Rect(e.X - r, e.Y - r, 2*r, 2*r)),
                    stroke: valueGuideStroke ?? valueStroke,
                    strokeThickness: 0.5,
                    dashed: 4,
                    rotation: rotation,
                    rotationOrigin: new Point(0.5, 0.5)),
                life);
            // squared magnitude filling
            var mag = value.Select(e => e.Magnitude*e.Magnitude);
            animation.Rects.Add(
                new RectDesc(
                    pos: position.Combine(unitRadius, mag, squish, (e, r, v, s) => new Rect(e.X - r, e.Y + (1 - 2 * v) * r, 2 * r * s, 2 * r * v)),
                    fill: fill,
                    rotation: rotation,
                    rotationOrigin: mag.Combine(squish, (v,s) => new Point(s == 0 ? 0 : (1/s)/2, 1 - 0.5/v))),
                life);
            // dashed reference line along positive real axis
            animation.Lines.Add(
                new LineSegmentDesc(
                    position.Combine(unitRadius, (p, r) => new LineSegment(p, p + new Vector(r, 0))),
                    dashed: 4,
                    stroke: valueGuideStroke ?? valueStroke,
                    thickness: 0.5), 
                life);
            // current value arrow
            animation.ShowArrow(life,
                                position,
                                value.Combine(phaseOffset, (e, p) => e*Complex.Exp(Complex.ImaginaryOne*p))
                                     .Combine(unitRadius, (v, r) => r*new Vector(v.Real, -v.Imaginary)),
                                stroke: valueStroke);
        }
        private static void ShowComplexProduct(this Animation animation,
                                               Ani<Brush> fill1,
                                               Ani<Brush> fill2,
                                               Ani<Brush> valueStroke1,
                                               Ani<Brush> valueStroke2,
                                               Ani<Complex> value1,
                                               Ani<Complex> value2,
                                               Ani<Point> position,
                                               Ani<double> unitRadius,
                                               Ani<double> time,
                                               Lifetime life) {
            // vertical input
            animation.ShowComplex(fill1.Combine(time, (s, t) => s.LerpToTransparent(t.LerpTransition(0, 1, 1, 1, 1))),
                                  valueStroke1,
                                  time.Combine(value1, value2, (t, v1, v2) => Complex.FromPolarCoordinates(
                                      v1.Magnitude.LerpTo(v1.Magnitude * v2.Magnitude, t.SmoothTransition(0, 0, 1, 1, 1)),
                                      v1.Phase)),
                                  position,
                                  unitRadius,
                                  life,
                                  rotation: Turn.FromNaturalAngle(Math.PI / 2),
                                  phaseOffset: time.Combine(value2, (t, v) => t.SmoothTransition(0,0,0, 1, 1) * v.Phase.ProperMod(Math.PI * 2)),
                                  sweepFill: fill1.Combine(time, (f, t) => f.LerpToTransparent(t.SmoothTransition(0, 0, 0, 0, 1))),
                                  valueGuideStroke: Brushes.Transparent);

            // horizontal input
            animation.ShowComplex(fill2.Combine(time, (s, t) => s.LerpToTransparent(t.LerpTransition(0,1,1, 1, 1))),
                                  valueStroke2.Combine(time, (s, t) => s.LerpToTransparent(t.LerpTransition(0,0,0, 0, 1))),
                                  time.Combine(value1, value2, (t, v1, v2) => Complex.FromPolarCoordinates(
                                      v2.Magnitude.LerpTo(v1.Magnitude * v2.Magnitude, t.SmoothTransition(0, 0, 1, 1, 1)),
                                      v2.Phase)),
                                  position,
                                  unitRadius,
                                  life,
                                  sweepFill: fill2.Combine(time, (s, t) => s.LerpToTransparent(t.LerpTransition(0, 0, 0, 0, 1))),
                                  valueGuideStroke: Brushes.Transparent);
            
            // result
            animation.ShowComplex(fill1.Combine(time, (s, t) => s.LerpToTransparent(t.LerpTransition(1, 0, 0, 0, 0))),
                                  Brushes.Transparent,
                                  time.Combine(value1, value2, (t, v1, v2) => Complex.FromPolarCoordinates(
                                      v2.Magnitude.LerpTo(v1.Magnitude * v2.Magnitude, t.SmoothTransition(0,0,1, 1, 1)),
                                      v2.Phase + v1.Phase)),
                                  position,
                                  unitRadius,
                                  life,
                                  valueGuideStroke: valueStroke1,
                                  sweepFill: fill1.Combine(time, (f, t) => f.LerpToTransparent(t.SmoothTransition(1, 1, 1,1,0))),
                                  squish: time.Combine(value1, (t, v) => t.SmoothTransition(v.Magnitude * v.Magnitude, v.Magnitude * v.Magnitude, 1, 1, 1)));
        }
        private static void ShowComplexSum(this Animation animation,
                                           Ani<Brush> fill1,
                                           Ani<Brush> fill2,
                                           Ani<Brush> valueStroke,
                                           IReadOnlyList<Ani<Complex>> values,
                                           Ani<Point> position,
                                           Ani<Point> target,
                                           Ani<Vector> positionDelta,
                                           Ani<double> unitRadius,
                                           Ani<double> time,
                                           Lifetime life) {
            Ani<Complex> sum = new ConstantAni<Complex>(Complex.Zero);
            foreach (var i in values.Count.Range()) {
                animation.ShowComplex(fill1.Combine(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(0, 1, 1, 1, 1))),
                                      valueStroke.Combine(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(0, 0.1, 0.1, 1, 1))),
                                      values[i],
                                      position.Combine(positionDelta, time, sum, unitRadius, target, (p, d, t, s, u, p2) => (p + d * i).LerpTo(p2 + new Vector(s.Real * u, s.Imaginary * -u), t.SmoothTransition(0, 0, 1, 1, 1))),
                                      unitRadius,
                                      life,
                                      valueStroke.Combine(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(0, 1, 1, 1, 1))),
                                      sweepFill: fill1.Combine(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(0, 0, 1, 1, 1))));
                sum = sum.Combine(values[i], (s, v) => s + v);
            }
            animation.ShowComplex(fill2.Combine(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(1, 1, 1, 0, 0))),
                                  valueStroke.Combine(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(1, 1, 1, 0, 0))),
                                  sum,
                                  target,
                                  unitRadius,
                                  life,
                                  valueStroke.Combine(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(1, 1, 0, 0, 0))),
                                  sweepFill: fill2.Combine(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(1, 1, 0, 0, 0))));
        }
        private static void HideShow(this Animation animation, Ani<double> time, double offset, double activePeriod, Action<Lifetime, Ani<double>> setup, Lifetime life) {
            animation.HideShow(time.Select(t => t.ProperMod(1) - offset).Select(t => t >= 0 && t < activePeriod),
                               li => setup(li, time.Select(e => ((e.ProperMod(1) - offset) / activePeriod).Clamp(0, 1))),
                               life);
        }
        private static void HideShow(this Animation animation, TimeSpan totalPeriod, TimeSpan offset, TimeSpan activePeriod, Action<Lifetime, Ani<double>> setup, Lifetime life) {
            animation.HideShow(Ani.Anon(t => t.Mod(totalPeriod) - offset).Select(t => t >= TimeSpan.Zero && t < activePeriod),
                               li => setup(li, Ani.Anon(e => (e.Mod(totalPeriod) - offset).DividedBy(activePeriod).Clamp(0, 1))),
                               life);
        }
        private static void HideShow(this Animation animation, Ani<bool> visible, Action<Lifetime> setup, Lifetime life) {
            var source = new LifetimeSource();
            animation.NextElapsedTime().Select(visible.ValueAt).SkipWhile(e => !e).DistinctUntilChanged().Subscribe(e => {
                if (e) {
                    source = life.CreateDependentSource();
                    setup(source.Lifetime);
                } else {
                    source.EndLifetime();
                }
            }, life);
        }
        public static void ShowMatrixMultiplication(this Animation animation,
                                                    Ani<double> time,
                                                    Rect pos,
                                                    ComplexMatrix u,
                                                    ComplexVector v,
                                                    Ani<Brush> or,
                                                    Ani<Brush> blu,
                                                    Ani<Brush> gre,
                                                    Ani<Brush> bla,
                                                    Lifetime life) {
            var d = Math.Min(pos.Width/(u.Columns.Count + 2), pos.Height/(u.Rows.Count + 2))/2;
            var ur = d;
            animation.Rects.Add(
                new RectDesc(new Rect(pos.X + d * 2, pos.Y + d * 2, pos.Width - d * 4, pos.Height - d * 4),
                             Brushes.Black,
                             strokeThickness: 0.6,
                             dashed: 5),
                life);
            animation.Rects.Add(
                new RectDesc(new Rect(pos.X, pos.Y + d * 2, d * 2, pos.Height - d * 4),
                             Brushes.Black,
                             strokeThickness: 0.6,
                             dashed: 5),
                life);
            animation.Rects.Add(
                new RectDesc(new Rect(pos.Right - d*2, pos.Y + d * 2, d * 2, pos.Height - d * 4),
                             Brushes.Black,
                             strokeThickness: 0.6,
                             dashed: 5),
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
                        animation.ShowComplex(pp.Combine(blu, (p, b) => b.LerpToTransparent(p.SmoothTransition(0, 0, 1))),
                                              pp.Combine(bla, (p, b) => b.LerpToTransparent(p.SmoothTransition(0, 0, 1))),
                                              v.Values[r],
                                              pp.Select(p => p.SmoothTransition(p1, p1, p2, p3)),
                                              ur,
                                              li,
                                              rotation: pp.Select(t => Turn.FromNaturalAngle(t.SmoothTransition(0, Math.PI / 2, Math.PI / 2))));
                        foreach (var c in u.Columns.Count.Range()) {
                            // vector copied into matrix
                            animation.ShowComplex(pp.Combine(blu, (p, b) => b.LerpToTransparent(p.SmoothTransition(1, 1, 0))),
                                                  pp.Combine(bla, (p, b) => b.LerpToTransparent(p.SmoothTransition(1, 1, 0))),
                                                  v.Values[c],
                                                  (pos.TopLeft + new Vector(d * 3 + c * d * 2, d * 3 + r * d * 2)),
                                                  ur,
                                                  li,
                                                  Brushes.Transparent,
                                                  rotation: Turn.FromNaturalAngle(Math.PI / 2));
                            // matrix
                            animation.ShowComplex(pp.Combine(or, (p, b) => b.LerpToTransparent(p.SmoothTransition(0, 0, 0))),
                                                  pp.Combine(bla, (p, b) => b.LerpToTransparent(p.SmoothTransition(0, 0, 0))),
                                                  u.Rows[r][c],
                                                  (pos.TopLeft + new Vector(d*3 + c*d*2, d*3 + r*d*2)),
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
                                                         v.Values[c],
                                                         u.Rows[r][c],
                                                         (pos.TopLeft + new Vector(d * 3 + c * d * 2, d * 3 + r * d * 2)),
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
                                                          .Select(e => new ConstantAni<Complex>(e)),
                                                 (pos.TopLeft + new Vector(d*3, d*3 + r*d*2)),
                                                 (pos.TopLeft + new Vector(d*3 + u.Columns.Count*d*2, d*3 + r*d*2)),
                                                 new Vector(d*2, 0),
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
                                              u.Rows[r].Count.Range().Select(e => u.Rows[r][e]*v.Values[e]).Sum(),
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
            var sp = 10.Seconds();
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
                        Brushes.Orange.LerpToTransparent(0.5),
                        Brushes.Blue.LerpToTransparent(0.7),
                        Brushes.Green.LerpToTransparent(0.5),
                        Brushes.Black,
                        li),
                    life);
            }
            return animation;
        }
    }
}