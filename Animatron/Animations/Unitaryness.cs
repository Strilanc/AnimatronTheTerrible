using System;
using System.Collections.Generic;
using System.Numerics;
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
        private static Animation Arrow(Ani<Point> start,
                                       Ani<Vector> delta,
                                       Ani<Brush> stroke = null,
                                       Ani<double> thickness = null,
                                       Ani<double> wedgeLength = null,
                                       Ani<double> dashed = null) {
            wedgeLength = wedgeLength ?? 5;
            thickness = thickness ?? 1;
            return new Animation {
                new PointDesc(start, fill: stroke, radius: thickness.Select(e => e*1.25)),
                new LineSegmentDesc(
                    pos: start.Combine(delta, (p, d) => new LineSegment(p, d)),
                    stroke: stroke,
                    thickness: thickness,
                    dashed: dashed),
                new[] {+1, -1}.Select(
                    s => new LineSegmentDesc(
                             pos: start.Combine(
                                 delta,
                                 wedgeLength,
                                 (p, d, w) =>
                                 new LineSegment(p + d, (s*d.Perp().Normal() - d.Normal()).Normal()*Math.Min(w, d.Length/2))),
                             stroke: stroke,
                             thickness: thickness,
                             dashed: dashed))
            };
        }
        private static IEnumerable<Vector> PhaseCurve(double phaseWidth, double r = 1, double startPhase = 0) {
            if (phaseWidth < 0) phaseWidth += Math.PI*2;
            yield return new Vector(0, 0);
            for (var d = 0.0; d <= phaseWidth; d += Math.Min(0.1, 1 / r)) {
                yield return r * new Vector(Math.Cos(d + startPhase), -Math.Sin(d + startPhase));
            }
        } 
        private static Animation ShowComplex(Ani<Brush> fill,
                                        Ani<Brush> valueStroke,
                                        Ani<Complex> value,
                                        Ani<Point> position,
                                        Ani<double> unitRadius,
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
            var mag = value.Select(e => e.Magnitude*e.Magnitude);
            return new Animation {
                new PolygonDesc(
                    phaseOffset.Combine(value, position, unitRadius, phaseRadius, (o, v, p, r, f) => PhaseCurve(v.Phase, f * r*0.5, o).Select(e => e + p).ToArray().AsEnumerable()),
                    stroke: sweepFill.Select(e => e.LerpTo(Brushes.Black, 0.5, lerpAlpha: false)),
                    fill: sweepFill.Select(e => e.LerpToTransparent(0.5)),
                    strokeThickness: value.Select(e => Math.Min(e.Magnitude*3,1))),
                new RectDesc(
                    pos: position.Combine(unitRadius, (e, r) => new Rect(e.X - r, e.Y - r, 2*r, 2*r)),
                    stroke: valueGuideStroke ?? valueStroke,
                    strokeThickness: 0.5,
                    dashed: 4,
                    rotation: rotation,
                    rotationOrigin: new Point(0.5, 0.5)),
                // squared magnitude filling
                new RectDesc(
                    pos: position.Combine(unitRadius, mag, squish, (e, r, v, s) => new Rect(e.X - r, e.Y + (1 - 2 * v) * r, 2 * r * s, 2 * r * v)),
                    fill: fill,
                    rotation: rotation,
                    rotationOrigin: mag.Combine(squish, (v,s) => new Point(s < 0.001 ? 0 : (1/s)/2, 1 - 0.5/v))),
                // current value arrow
                Arrow(position,
                      value.Combine(phaseOffset, (e, p) => e*Complex.Exp(Complex.ImaginaryOne*p))
                           .Combine(unitRadius, (v, r) => r*new Vector(v.Real, -v.Imaginary)),
                      stroke: valueStroke)
            };
        }
        private static Animation ShowComplexProduct(Ani<Brush> fill1,
                                                    Ani<Brush> fill2,
                                                    Ani<Brush> valueStroke1,
                                                    Ani<Brush> valueStroke2,
                                                    Ani<Complex> value1,
                                                    Ani<Complex> value2,
                                                    Ani<Point> position,
                                                    Ani<double> unitRadius,
                                                    Ani<double> time,
                                                    Ani<Brush> fill3 = null) {
            if (fill1 == null) throw new ArgumentNullException("fill1");
            if (fill2 == null) throw new ArgumentNullException("fill2");
            if (valueStroke1 == null) throw new ArgumentNullException("valueStroke1");
            if (valueStroke2 == null) throw new ArgumentNullException("valueStroke2");
            if (value1 == null) throw new ArgumentNullException("value1");
            if (value2 == null) throw new ArgumentNullException("value2");
            if (position == null) throw new ArgumentNullException("position");
            if (unitRadius == null) throw new ArgumentNullException("unitRadius");
            if (time == null) throw new ArgumentNullException("time");
            fill3 = fill3 ?? fill1;
            return new Animation {
                // vertical input
                ShowComplex(fill1.Combine(time, (s, t) => s.LerpToTransparent(t.LerpTransition(0, 1, 1, 1, 1))),
                            valueStroke1,
                            time.Combine(value1,
                                         value2,
                                         (t, v1, v2) => Complex.FromPolarCoordinates(
                                             v1.Magnitude.LerpTo(v1.Magnitude*v2.Magnitude, t.SmoothTransition(0, 0, 1, 1, 1)),
                                             v1.Phase)),
                            position,
                            unitRadius,
                            rotation: Turn.FromNaturalAngle(Math.PI/2),
                            phaseOffset: time.Combine(value2, (t, v) => t.SmoothTransition(0, 0, 0, 1, 1)*v.Phase.ProperMod(Math.PI*2)),
                            sweepFill: fill1.Combine(time, (f, t) => f.LerpToTransparent(t.SmoothTransition(0, 0, 0, 0, 1))),
                            valueGuideStroke: Brushes.Transparent),

                // horizontal input
                ShowComplex(fill2.Combine(time,
                                          (s, t) => s.LerpToTransparent(t.LerpTransition(0, 1, 1, 1, 1))),
                            valueStroke2.Combine(time, (s, t) => s.LerpToTransparent(t.LerpTransition(0, 0, 0, 0, 1))),
                            time.Combine(value1,
                                         value2,
                                         (t, v1, v2) => Complex.FromPolarCoordinates(
                                             v2.Magnitude.LerpTo(v1.Magnitude*v2.Magnitude, t.SmoothTransition(0, 0, 1, 1, 1)),
                                             v2.Phase)),
                            position,
                            unitRadius,
                            sweepFill: fill2.Combine(time, (s, t) => s.LerpToTransparent(t.LerpTransition(0, 0, 0, 0, 1))),
                            valueGuideStroke: Brushes.Transparent),
            
                // result
                ShowComplex(fill3.Combine(time,
                                          (s, t) => s.LerpToTransparent(t.LerpTransition(1, 0, 0, 0, 0))),
                            Brushes.Transparent,
                            time.Combine(value1,
                                         value2,
                                         (t, v1, v2) => Complex.FromPolarCoordinates(
                                             v1.Magnitude < 0.001 ? 0 : v2.Magnitude.LerpTo(v1.Magnitude*v2.Magnitude, t.SmoothTransition(0, 0, 1, 1, 1)),
                                             v2.Phase + v1.Phase)),
                            position,
                            unitRadius,
                            valueGuideStroke: valueStroke1,
                            sweepFill: fill3.Combine(time, (f, t) => f.LerpToTransparent(t.SmoothTransition(1, 1, 1, 1, 0))),
                            squish: time.Combine(value1, (t, v) => Math.Pow(t.SmoothTransition(v.Magnitude, v.Magnitude, 1, 1, 1), 2)))
            };
        }
        private static Animation ShowComplexSum(Ani<Brush> fill1,
                                           Ani<Brush> fill2,
                                           Ani<Brush> valueStroke,
                                           IReadOnlyList<Ani<Complex>> values,
                                           Ani<Point> position,
                                           Ani<Point> target,
                                           Ani<Vector> positionDelta,
                                           Ani<double> unitRadius,
                                           Ani<double> time) {
            var sum = (Ani < Complex > )Complex.Zero;
            var animation = new Animation();
            foreach (var i in values.Count.Range()) {
                animation.Things.Add(
                    ShowComplex(
                        fill1.Combine(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(0, 2, 1, 1, 1))),
                        valueStroke.Combine(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(0, 0.1, 0.1, 1, 1))),
                        values[i],
                        position.Combine(positionDelta,
                                         time,
                                         sum,
                                         unitRadius,
                                         target,
                                         (p, d, t, s, u, p2) => (p + d*i).LerpTo(p2 + new Vector(s.Real*u, s.Imaginary*-u), t.SmoothTransition(0, 0, 1, 1, 1))),
                        unitRadius,
                        valueStroke.Combine(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(0, 1, 1, 1, 1))),
                        sweepFill: fill1.Combine(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(0, 0, 1, 1, 1)))),
                    Lifetime.Immortal);
                sum = sum.Combine(values[i], (s, v) => s + v);
            }
            animation.Things.Add(
                ShowComplex(
                    fill2.Combine(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(1, 1, 1, 1,0, 0))),
                    valueStroke.Combine(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(1, 1, 1,0, 0, 0))),
                    sum,
                    target,
                    unitRadius,
                    valueStroke.Combine(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(1, 1, 1,0, 0, 0))),
                    sweepFill: fill2.Combine(time, (s, t) => s.LerpToTransparent(t.SmoothTransition(1, 1, 1, 0, 0)))),
                Lifetime.Immortal);
            return animation;
        }
        public static Animation ShowMatrix(Rect pos,
                                      ComplexMatrix u,
                                      Ani<Brush> or,
                                      Ani<Brush> bla) {
            var d = Math.Min(pos.Width / (u.Columns.Count +2), pos.Height / (u.Rows.Count)) / 2;
            pos = new Rect(pos.TopLeft, new Size(d * (u.Columns.Count + 2) * 2, d * u.Rows.Count * 2));
            var ur = d;
            return new Animation {
                new RectDesc(new Rect(pos.X + d*2, pos.Y, pos.Width - d*4, pos.Height),
                             Brushes.Black,
                             strokeThickness: 0.6,
                             dashed: 5),
                new RectDesc(new Rect(pos.X, pos.Y, d*2, pos.Height),
                             Brushes.Black,
                             strokeThickness: 0.6,
                             dashed: 5),
                new RectDesc(new Rect(pos.Right - d*2, pos.Y, d*2, pos.Height),
                             Brushes.Black,
                             strokeThickness: 0.6,
                             dashed: 5),
                // matrix
                u.Rows.Count.Range().SelectMany(
                    r => u.Columns.Count.Range().Select(
                        c => ShowComplex(
                            or,
                            bla,
                            u.Rows[r][c],
                            (pos.TopLeft + new Vector(d*3 + c*d*2, d + r*d*2)),
                            ur)))
            };
        }
        public static Animation ShowMatrixMultiplication(TimeSpan period,
                                                    Rect pos,
                                                    ComplexMatrix u,
                                                    ComplexVector v,
                                                    Ani<Brush> or,
                                                    Ani<Brush> blu,
                                                    Ani<Brush> bla) {
            var d = Math.Min(pos.Width/(u.Columns.Count + 2), pos.Height/u.Rows.Count)/2;
            pos = new Rect(pos.TopLeft, new Size(d * (u.Columns.Count + 2)*2, d*u.Rows.Count*2));
            var ur = d;
            var animation = new Animation {
                new RectDesc(new Rect(pos.X + d * 2, pos.Y, pos.Width - d * 4, pos.Height),
                             Brushes.Black,
                             strokeThickness: 0.6,
                             dashed: 5),
                new RectDesc(new Rect(pos.X, pos.Y, d * 2, pos.Height),
                             Brushes.Black,
                             strokeThickness: 0.6,
                             dashed: 5),
                new RectDesc(new Rect(pos.Right - d*2, pos.Y, d * 2, pos.Height),
                             Brushes.Black,
                             strokeThickness: 0.6,
                             dashed: 5)
            };
            var per = animation.Periodic(period);
            var s0 = per.LimitedSameTime(0.Seconds(), period.DividedBy(4));
            foreach (var r in u.Rows.Count.Range()) {
                var pp = s0.Proper;
                // input vector
                var p1 = pos.TopLeft + new Vector(d, d + r*d*2);
                var p2 = pos.TopLeft + new Vector(d*3 + r*d*2, 0);
                var p3 = pos.TopLeft + new Vector(d*3 + r*d*2, u.Rows.Count*d*2);
                s0.Add(ShowComplex(pp.Combine(blu, (p, b) => b.LerpToTransparent(p.SmoothTransition(0, 0, 1))),
                                   pp.Combine(bla, (p, b) => b.LerpToTransparent(p.SmoothTransition(0, 0, 1))),
                                   v.Values[r],
                                   pp.Select(p => p.SmoothTransition(p1, p1, p2, p3)),
                                   ur,
                                   rotation: pp.Select(t => Turn.FromNaturalAngle(t.SmoothTransition(0, Math.PI/2, Math.PI/2)))));
                foreach (var c in u.Columns.Count.Range()) {
                    // vector copied into matrix
                    s0.Add(ShowComplex(pp.Combine(blu, (p, b) => b.LerpToTransparent(p.SmoothTransition(1, 1, 0))),
                                       pp.Combine(bla, (p, b) => b.LerpToTransparent(p.SmoothTransition(1, 1, 0))),
                                       v.Values[c],
                                       (pos.TopLeft + new Vector(d*3 + c*d*2, d + r*d*2)),
                                       ur,
                                       Brushes.Transparent,
                                       rotation: Turn.FromNaturalAngle(Math.PI/2)));
                    // matrix
                    s0.Add(ShowComplex(pp.Combine(or, (p, b) => b.LerpToTransparent(p.SmoothTransition(0, 0, 0))),
                                       pp.Combine(bla, (p, b) => b.LerpToTransparent(p.SmoothTransition(0, 0, 0))),
                                       u.Rows[r][c],
                                       (pos.TopLeft + new Vector(d*3 + c*d*2, d + r*d*2)),
                                       ur));
                }
            }
            var s1 = per.LimitedSameTime(period.Times(0.25), period.Times(0.5));
            foreach (var r in u.Rows.Count.Range()) {
                foreach (var c in u.Columns.Count.Range()) {
                    s1.Add(
                        ShowComplexProduct(
                            blu,
                            or,
                            bla,
                            bla,
                            v.Values[c],
                            u.Rows[r][c],
                            (pos.TopLeft + new Vector(d*3 + c*d*2, d + r*d*2)),
                            ur,
                            s1.Proper));
                }
            }
            var s2 = per.LimitedSameTime(period.Times(0.5), period);
            foreach (var r in u.Rows.Count.Range()) {
                s2.Add(
                    ShowComplexSum(
                        blu,
                        blu,
                        bla,
                        u.Rows[r].Count.Range()
                                 .Select(e => u.Rows[r][e]*v.Values[e])
                                 .Select(e => new ConstantAni<Complex>(e)),
                        (pos.TopLeft + new Vector(d*3, d + r*d*2)),
                        (pos.TopLeft + new Vector(d*3 + u.Columns.Count*d*2, d + r*d*2)),
                        new Vector(d*2, 0),
                        ur,
                        s2.Proper));
            }
            return animation;
        }
        public static Animation CreateComplexRepresentationAnimation(Lifetime life) {
            var c =
                Ani.Time
                   .Select(e => e.TotalSeconds*1)
                   .Select(e => Math.Floor(e) + 0.0.SmoothLerpTo(1, e % 1))
                   .Select(e => Math.Floor(e) + 0.0.SmoothLerpTo(1, e % 1))
                   .Select(e => Math.Floor(e) + 0.0.SmoothLerpTo(1, e % 1))
                   .Select(e => Math.Floor(e) + 0.0.SmoothLerpTo(1, e % 1))
                   .Select(t => Complex.FromPolarCoordinates(
                       t.SmoothCycle(0, 0.5, Math.Sqrt(0.5), 1, 0.5, 0.5, 0.5, Math.Sqrt(0.5)),
                       t.SmoothCycle(0, 0.0, 0, 0, 0, Math.PI / 2, 3*Math.PI/4, 7 * Math.PI / 4)));

            var animation = new Animation {
                new TextDesc(c.Select(e => e.Real.ToString("0.00").PadLeft(5) + (e.Imaginary < 0 ? " - " : " + ") + e.Imaginary.Abs().ToString("0.00") + "i"),
                             new Point(125, 150 - 100),
                             new Point(0.5, 1),
                             fontSize: 20),
                new TextDesc(c.Select(e => e.ToMagPhaseString("0.00", "0")), new Point(125, 150 + 100), new Point(0.5, 0), fontSize: 20),
                ShowComplex(Brushes.Orange.LerpToTransparent(0.5),
                            Brushes.Black,
                            c,
                            new Point(125, 150),
                            100),
                new TextDesc("Square Diagram of Complex Number", new Point(125, 5), new Point(0.5, 0), fontSize:15, foreground:Brushes.Gray)
            };


            return animation;
        }
        public static Animation CreateComplexProductAnimation() {
            var matrixFill = (Brush)Brushes.Orange.LerpToTransparent(0.5);
            var vectorFill = (Brush)Brushes.Blue.LerpToTransparent(0.7);
            var c1 = new Complex(1, 1) / 2;
            var c2 = new Complex(-1, 1) / 2;

            var animation = new Animation {
                new TextDesc("*", new Point((125 + 350)/2.0, 150), new Point(0.5, 0.5), fontSize: 20),
                new TextDesc("=", new Point((575 + 350)/2.0, 150), new Point(0.5, 0.5), fontSize: 20),
                new TextDesc(c1.ToPrettyString(), new Point(125, 150 - 100), new Point(0.5, 1), fontSize: 20),
                new TextDesc(c2.ToPrettyString(), new Point(350, 150 - 100), new Point(0.5, 1), fontSize: 20),
                new TextDesc((c1*c2).ToPrettyString(), new Point(575, 150 - 100), new Point(0.5, 1), fontSize: 20),

                new TextDesc(c1.ToMagPhaseString(), new Point(125, 150 + 100), new Point(0.5, 0), fontSize: 20),
                new TextDesc(c2.ToMagPhaseString(), new Point(350, 150 + 100), new Point(0.5, 0), fontSize: 20),
                new TextDesc((c1*c2).ToMagPhaseString(), new Point(575, 150 + 100), new Point(0.5, 0), fontSize: 20),
                new TextDesc("Multiplying Complex Numbers: Multiply Magnitudes, Add Phases",
                             new Point(350, 5),
                             new Point(0.5, 0),
                             fontSize: 15,
                             foreground: Brushes.Gray),

                ShowComplex(vectorFill,
                            Brushes.Black,
                            c1,
                            new Point(125, 150),
                            100),
                ShowComplex(matrixFill,
                            Brushes.Black,
                            c2,
                            new Point(350, 150),
                            100)
            };

            var p = animation.Dilated(0.4.Seconds()).Periodic(10.Seconds());
            var s1 = p.LimitedSameTime(0.Seconds(), 3.Seconds());
            s1.Add(ShowComplex(vectorFill,
                               Brushes.Black,
                               c1,
                               Ani.Anon(
                                   t =>
                                   new Point(125, 150).SmoothLerpTo(new Point(575, 150), (s1.Proper.ValueAt(t)*1.5).Min(1))
                                   + new Vector(0, (s1.Proper.ValueAt(t)*1.5).Min(1).LerpTransition(0, -25, 0))),
                               100,
                               rotation: Ani.Anon(t => Turn.FromNaturalAngle(0.0.SmoothLerpTo(Math.PI/2, (s1.Proper.ValueAt(t)*2).Min(1))))));
            s1.Add(ShowComplex(matrixFill,
                               Brushes.Black,
                               c2,
                               Ani.Anon(
                                   t =>
                                   new Point(350, 150).SmoothLerpTo(new Point(575, 150), (s1.Proper.ValueAt(t)*1.5).Min(1))
                                   + new Vector(0, (s1.Proper.ValueAt(t)*1.5).Min(1).LerpTransition(0, -25, 0))),
                               100));
            var sb = p.LimitedSameTime(3.Seconds(), 9.Seconds());
            sb.Add(
                ShowComplexProduct(vectorFill,
                                   matrixFill,
                                   Brushes.Black,
                                   Brushes.Black,
                                   c1,
                                   c2,
                                   new Point(575, 150),
                                   100,
                                   Ani.Anon(t => (sb.Proper.ValueAt(t)*1.2).Min(1)),
                                   vectorFill.LerpTo(matrixFill, 0.5)));

            var s2 = p.LimitedSameTime(9.Seconds(), 9.5.Seconds());
            s2.Add(ShowComplex(Ani.Anon(t => vectorFill.LerpTo(matrixFill, 0.5).LerpToTransparent(s2.Proper.ValueAt(t))),
                               Ani.Anon(t => (Brush)Brushes.Black.LerpToTransparent(s2.Proper.ValueAt(t))),
                               c1*c2,
                               new Point(575, 150),
                               100));

            return animation;
        }
        public static Animation CreateComplexSumAnimation() {
            var matrixFill = (Brush)Brushes.Orange.LerpToTransparent(0.5);
            var vectorFill = (Brush)Brushes.Blue.LerpToTransparent(0.7);
            var c1 = new Complex(-0.5, -0.5) * Math.Sqrt(0.5);
            var c2 = new Complex(-0.5, 0.5);

            var animation = new Animation {
                new TextDesc("+", new Point((125 + 350)/2.0, 150), new Point(0.5, 0.5), fontSize: 20),
                new TextDesc("=", new Point((575 + 350)/2.0, 150), new Point(0.5, 0.5), fontSize: 20),
                new TextDesc(c1.ToPrettyString(), new Point(125, 150 - 100), new Point(0.5, 1), fontSize: 20),
                new TextDesc(c2.ToPrettyString(), new Point(350, 150 - 100), new Point(0.5, 1), fontSize: 20),
                new TextDesc((c1 + c2).ToPrettyString(), new Point(575, 150 - 100), new Point(0.5, 1), fontSize: 20),

                new TextDesc(c1.ToMagPhaseString(), new Point(125, 150 + 100), new Point(0.5, 0), fontSize: 20),
                new TextDesc(c2.ToMagPhaseString(), new Point(350, 150 + 100), new Point(0.5, 0), fontSize: 20),
                new TextDesc((c1 + c2).ToMagPhaseString(), new Point(575, 150 + 100), new Point(0.5, 0), fontSize: 20),
                new TextDesc("Adding Complex Numbers: Place Arrows End to End",
                             new Point(350, 5),
                             new Point(0.5, 0),
                             fontSize: 15,
                             foreground: Brushes.Gray),

                ShowComplex(vectorFill,
                            Brushes.Black,
                            c1,
                            new Point(125, 150),
                            100),
                ShowComplex(matrixFill,
                            Brushes.Black,
                            c2,
                            new Point(350, 150),
                            100)
            };
            var p = animation.Dilated(0.4.Seconds()).Periodic(10.Seconds());
            p.LimitedSameTime(0.Seconds(), 9.5.Seconds()).Add(ShowComplexSum(
                Brushes.Transparent,
                vectorFill.LerpTo(matrixFill, 0.5),
                Brushes.Black,
                new Ani<Complex>[] {c1, c2},
                new Point(125, 150),
                new Point(575, 150),
                new Vector(225, 0),
                100,
                p.Proper));

            var s2 = p.LimitedSameTime(9.5.Seconds(), 10.Seconds());
            s2.Add(ShowComplex(Ani.Anon(t => vectorFill.LerpTo(matrixFill, 0.5).LerpToTransparent(s2.Proper.ValueAt(t))),
                               Ani.Anon(t => (Brush)Brushes.Black.LerpToTransparent(s2.Proper.ValueAt(t))),
                               c1 + c2,
                               new Point(575, 150),
                               100));

            return animation;
        }
        public static Animation CreateComplexTransformationAnimation() {
            var matrixFill = (Brush)Brushes.Orange.LerpToTransparent(0.5);
            var vectorFill = (Brush)Brushes.Blue.LerpToTransparent(0.7);

            var s = new Complex(Math.Sqrt(0.5), 0);
            var si = new Complex(0, s.Real);
            var c1 = Complex.FromPolarCoordinates(Math.Sqrt(0.75), 0);
            var c2 = Complex.FromPolarCoordinates(Math.Sqrt(0.2), Math.PI / 4);
            var c3 = Complex.FromPolarCoordinates(Math.Sqrt(0.05), Math.PI);
            var m = ComplexMatrix.FromCellData(s, si, 0, -s, si, 0, 0,0,Complex.FromPolarCoordinates(1, Math.PI));
            var v = new ComplexVector(new[] { c1, c2, c3 });
            var u = 50;

            var animation = new Animation {
                new TextDesc("Matrix Multiplication with Complex Numbers",
                             new Point(25 + u*(v.Values.Count + 2), 5),
                             new Point(0.5, 0),
                             fontSize: 15,
                             foreground: Brushes.Gray),
            };
            var r = new Rect(25, 25, u*2*(v.Values.Count + 2), u*2*v.Values.Count);
            var p = animation.Dilated(0.5.Seconds()).Periodic(10.Seconds());
            var qx1 = p.Proper.Select(e => (e * 10 - 9).Clamp(0, 1));
            var qx2 = p.Proper.Select(e => ((e - 0.6) * 10).Clamp(0, 1));

            p.Add(ShowMatrixMultiplication(
                10.Seconds(),
                r, 
                m,
                v,
                qx1.Select(matrixFill.LerpToTransparent),
                qx1.Select(vectorFill.LerpToTransparent),
                qx1.Select(x => (Brush)Brushes.Black.LerpToTransparent(x))));

            p.LimitedSameTime(0.Seconds(), 10.Seconds()).Add(ShowMatrixMultiplication(
                100000000.Seconds(),
                r,
                m,
                v,
                qx2.Select(x => matrixFill.LerpToTransparent(1 - x)),
                qx2.Select(x => vectorFill.LerpToTransparent(1 - x)),
                qx2.Select(x => (Brush)Brushes.Black.LerpToTransparent(1 - x))));

            return animation;
        }

        public static Animation CreateHadamardGateAnimation() {
            var matrixFill = (Brush)Brushes.Orange.LerpToTransparent(0.5);
            var vectorFill = (Brush)Brushes.Blue.LerpToTransparent(0.7);
            var pow = 1;
            var H = ComplexMatrix.MakeUnitaryHadamard(pow);
            var v0 = new ComplexVector(new Complex[] { 1, 0 });
            var v1 = v0 * H;
            var v2 = v1 * H;
            var u0 = new ComplexVector(new Complex[] { 0, 1 });
            var u1 = u0 * H;
            var u2 = u1 * H;

            var m = 150.0;
            var w = m / (v0.Values.Count+2)/2;
            var m2 = m/2;
            var span = 500;
            var x1 = 100;
            var x2 = x1 + span/2;

            var duration = 5.Seconds();
            var sw = Ani.Anon(t => new Point(t.DividedBy(duration).LerpTransition(-w * 2, span - w * 2), 0).Sweep(new Vector(0, 1000)));
            var t1 = duration.Times((x1 - m2 + w * 2) / span);
            var t2 = duration.Times((x1 + m2) / span);
            var t3 = duration.Times((x2 - m2 + w * 2) / span);
            var t4 = duration.Times((x2 + m2) / span);
            var t5 = duration;

            var wireY = 60;
            var animation = new Animation {
                new TextDesc("Hadamard Gate: From Pure to Mixed and Back",
                             new Point((x1+x2)/2.0, 5),
                             new Point(0.5, 0),
                             fontSize: 15,
                             foreground: Brushes.Gray),
                // wire
                new LineSegmentDesc(new Point(0, wireY).Sweep(new Vector(1000, 0)))
            };
            
            var period = animation.Periodic(duration);
            var period2 = animation.Dilated(1.Seconds(), zero: duration.DividedBy(2)).Periodic(duration);
            var halfPeriod = animation.Periodic(duration.DividedBy(2));
            
            // sweep line
            period.Add(new LineSegmentDesc(sw, Brushes.Red, 0.5, 4));
            period2.Add(new LineSegmentDesc(sw, Brushes.Red, 0.5, 4));
            
            // matrices (not shown during multiplication)
            var xx = halfPeriod.LimitedSameTime(0.Seconds(), t1);
            var xx2 = halfPeriod.LimitedSameTime(t2, t3);
            var m1 = ShowMatrix(new Rect(x1 - m2, 100, m2*2, m2*2),
                                H,
                                matrixFill,
                                Brushes.Black);
            var m_2 = ShowMatrix(new Rect(x2 - m2, 100, m2*2, m2*2),
                                 H,
                                 matrixFill,
                                 Brushes.Black);
            xx.Add(m1);
            xx.Add(m_2);
            xx2.Add(m1);
            xx2.Add(m_2);

            // matrix multiplications
            period.LimitedNewTime(t1, t2).Add(
                ShowMatrixMultiplication(
                        t2-t1,
                        new Rect(x1 - m2, 100, m2 * 2, m2 * 2),
                        H,
                        v0,
                        matrixFill,
                        vectorFill,
                        Brushes.Black));
            period.LimitedNewTime(t3, t4).Add(
                ShowMatrixMultiplication(
                    t4 - t3,
                    new Rect(x2 - m2, 100, m2*2, m2*2),
                    H,
                    v1,
                    matrixFill,
                    vectorFill,
                    Brushes.Black));
            period2.LimitedNewTime(t1, t2).Add(
                ShowMatrixMultiplication(
                        t2 - t1,
                        new Rect(x1 - m2, 100, m2 * 2, m2 * 2),
                        H,
                        u0,
                        matrixFill,
                        vectorFill,
                        Brushes.Black));
            period2.LimitedNewTime(t3, t4).Add(
                ShowMatrixMultiplication(
                    t4 - t3,
                    new Rect(x2 - m2, 100, m2 * 2, m2 * 2),
                    H,
                    u1,
                    matrixFill,
                    vectorFill,
                    Brushes.Black));

            animation.Add(new TextDesc(
                Ani.Anon(t => t.Mod(duration)).Select(t => t < t1.LerpTo(t2, 0.5) || t > t3.LerpTo(t4, 0.5) ? "On" : "Mixed"),
                Ani.Anon(t => new Point(t.DividedBy(duration).ProperMod(1).LerpTransition(-w * 2, span - w * 2), wireY))));
            animation.Add(new TextDesc(
                Ani.Anon(t => (t + duration.DividedBy(2)).Mod(duration)).Select(t => t < t1.LerpTo(t2, 0.5) || t > t3.LerpTo(t4, 0.5) ? "Off" : "Mixed"),
                Ani.Anon(t => new Point((t.DividedBy(duration)+0.5).ProperMod(1).LerpTransition(-w * 2, span - w * 2), wireY))));

            // show vector being 'pushed' by sweep line
            foreach (var z in new[] {new {p = period, v = new[] {v0, v1, v2}}, new {p = period2, v = new[] {u0, u1, u2}}}) {
                z.p.LimitedSameTime(0.Seconds(), t1).Add(z.v[0].Values.Count.Range().Select(
                    j => ShowComplex(
                        vectorFill,
                        Brushes.Black,
                        z.v[0].Values[j],
                        sw.Select(e => new Point(e.LerpAcross(0.3).X + w, 100 + w) + new Vector(0, j * w * 2)),
                        w)));
                z.p.LimitedSameTime(t2, t3).Add(z.v[1].Values.Count.Range().Select(
                    j => ShowComplex(
                        vectorFill,
                        Brushes.Black,
                        z.v[1].Values[j],
                        sw.Select(e => new Point(e.LerpAcross(0.3).X + w, 100 + w) + new Vector(0, j * w * 2)),
                        w)));
                z.p.LimitedSameTime(t4, t5).Add(z.v[2].Values.Count.Range().Select(
                    j => ShowComplex(
                        vectorFill,
                        Brushes.Black,
                        z.v[2].Values[j],
                        sw.Select(e => new Point(e.LerpAcross(0.3).X + w, 100 + w) + new Vector(0, j * w * 2)),
                        w)));
            }

            animation.Add(new Animation {
                // left hadamard gate
                new RectDesc(new Rect(x1 - 25, wireY - 25, 50, 50), Brushes.Black, Brushes.White, 1.0),
                new TextDesc("H", new Point(x1, wireY), new Point(0.5, 0.5), fontWeight: FontWeights.ExtraBold, fontSize: 20),
                // right hadamard gate
                new RectDesc(new Rect(x2 - 25, wireY - 25, 50, 50), Brushes.Black, Brushes.White, 1.0),
                new TextDesc("H", new Point(x2, wireY), new Point(0.5, 0.5), fontWeight: FontWeights.ExtraBold, fontSize: 20),
            });

            return animation;
        }
        public static Animation CreateSeparateWireHadamardGateAnimation() {
            var matrixFill = (Brush)Brushes.Orange.LerpToTransparent(0.5);
            var vectorFill = (Brush)Brushes.Blue.LerpToTransparent(0.7);
            var s = Math.Sqrt(0.5);
            var H1 = ComplexMatrix.FromCellData(
                s, s, 0, 0,
                s,-s, 0, 0,
                0, 0, s, s,
                0, 0, s,-s);
            var H2 = ComplexMatrix.FromCellData(
                s, 0, s, 0,
                0, s, 0, s,
                s, 0,-s, 0,
                0, s, 0,-s);
            var v0 = new ComplexVector(new Complex[] { 1, 0, 0, 0 });
            var v1 = v0 * H1;
            var v2 = v1 * H2;
            var u0 = new ComplexVector(new Complex[] { 0, 0, 0, 1 });
            var u1 = u0 * H1;
            var u2 = u1 * H2;

            var m = 150.0;
            var w = m / (v0.Values.Count + 2) / 2;
            var m2 = m / 2;
            var span = 500;
            var x1 = 100;
            var x2 = x1 + span / 2;

            var duration = 5.Seconds();
            var sw = Ani.Anon(t => new Point(t.DividedBy(duration).LerpTransition(-w * 2, span - w * 2), 0).Sweep(new Vector(0, 1000)));
            var t1 = duration.Times((x1 - m2 + w * 2) / span);
            var t2 = duration.Times((x1 + m2) / span);
            var t3 = duration.Times((x2 - m2 + w * 2) / span);
            var t4 = duration.Times((x2 + m2) / span);
            var t5 = duration;

            var wireY1 = 45;
            var wireY2 = 70;
            var animation = new Animation {
                new TextDesc("Hadamard Gates on Separate Wires",
                             new Point((x1+x2)/2.0, 5),
                             new Point(0.5, 0),
                             fontSize: 15,
                             foreground: Brushes.Gray),
                // wire
                new LineSegmentDesc(new Point(0, wireY1).Sweep(new Vector(1000, 0))),
                new LineSegmentDesc(new Point(0, wireY2).Sweep(new Vector(1000, 0)))
            };

            var period = animation.Periodic(duration);
            var period2 = animation.Dilated(1.Seconds(), zero: duration.DividedBy(2)).Periodic(duration);
            var halfPeriod = animation.Periodic(duration.DividedBy(2));

            // sweep line
            period.Add(new LineSegmentDesc(sw, Brushes.Red, 0.5, 4));
            period2.Add(new LineSegmentDesc(sw, Brushes.Red, 0.5, 4));

            // matrices (not shown during multiplication)
            var xx = halfPeriod.LimitedSameTime(0.Seconds(), t1);
            var xx2 = halfPeriod.LimitedSameTime(t2, t3);
            var m1 = ShowMatrix(new Rect(x1 - m2, 100, m2 * 2, m2 * 2),
                                H1,
                                matrixFill,
                                Brushes.Black);
            var m_2 = ShowMatrix(new Rect(x2 - m2, 100, m2 * 2, m2 * 2),
                                 H2,
                                 matrixFill,
                                 Brushes.Black);
            xx.Add(m1);
            xx.Add(m_2);
            xx2.Add(m1);
            xx2.Add(m_2);

            // matrix multiplications
            period.LimitedNewTime(t1, t2).Add(
                ShowMatrixMultiplication(
                        t2 - t1,
                        new Rect(x1 - m2, 100, m2 * 2, m2 * 2),
                        H1,
                        v0,
                        matrixFill,
                        vectorFill,
                        Brushes.Black));
            period.LimitedNewTime(t3, t4).Add(
                ShowMatrixMultiplication(
                    t4 - t3,
                    new Rect(x2 - m2, 100, m2 * 2, m2 * 2),
                    H2,
                    v1,
                    matrixFill,
                    vectorFill,
                    Brushes.Black));
            period2.LimitedNewTime(t1, t2).Add(
                ShowMatrixMultiplication(
                        t2 - t1,
                        new Rect(x1 - m2, 100, m2 * 2, m2 * 2),
                        H1,
                        u0,
                        matrixFill,
                        vectorFill,
                        Brushes.Black));
            period2.LimitedNewTime(t3, t4).Add(
                ShowMatrixMultiplication(
                    t4 - t3,
                    new Rect(x2 - m2, 100, m2 * 2, m2 * 2),
                    H2,
                    u1,
                    matrixFill,
                    vectorFill,
                    Brushes.Black));

            animation.Add(new TextDesc(
                Ani.Anon(t => t.Mod(duration)).Select(t => t < t1.LerpTo(t2, 0.5) ? "On" : "Mixed"),
                Ani.Anon(t => new Point(t.DividedBy(duration).ProperMod(1).LerpTransition(-w * 2, span - w * 2), wireY1))));
            animation.Add(new TextDesc(
                Ani.Anon(t => t.Mod(duration)).Select(t => t < t3.LerpTo(t4, 0.5) ? "Off" : "Mixed"),
                Ani.Anon(t => new Point(t.DividedBy(duration).ProperMod(1).LerpTransition(-w * 2, span - w * 2), wireY2))));

            animation.Add(new TextDesc(
                Ani.Anon(t => (t + duration.DividedBy(2)).Mod(duration)).Select(t => t < t1.LerpTo(t2, 0.5) ? "Off" : "Mixed"),
                Ani.Anon(t => new Point((t.DividedBy(duration) + 0.5).ProperMod(1).LerpTransition(-w * 2, span - w * 2), wireY1))));
            animation.Add(new TextDesc(
                Ani.Anon(t => (t + duration.DividedBy(2)).Mod(duration)).Select(t => t < t3.LerpTo(t4, 0.5) ? "Off" : "Mixed"),
                Ani.Anon(t => new Point((t.DividedBy(duration) + 0.5).ProperMod(1).LerpTransition(-w * 2, span - w * 2), wireY2))));

            // show vector being 'pushed' by sweep line
            foreach (var z in new[] { new { p = period, v = new[] { v0, v1, v2 } }, new { p = period2, v = new[] { u0, u1, u2 } } }) {
                z.p.LimitedSameTime(0.Seconds(), t1).Add(z.v[0].Values.Count.Range().Select(
                    j => ShowComplex(
                        vectorFill,
                        Brushes.Black,
                        z.v[0].Values[j],
                        sw.Select(e => new Point(e.LerpAcross(0.3).X + w, 100 + w) + new Vector(0, j * w * 2)),
                        w)));
                z.p.LimitedSameTime(t2, t3).Add(z.v[1].Values.Count.Range().Select(
                    j => ShowComplex(
                        vectorFill,
                        Brushes.Black,
                        z.v[1].Values[j],
                        sw.Select(e => new Point(e.LerpAcross(0.3).X + w, 100 + w) + new Vector(0, j * w * 2)),
                        w)));
                z.p.LimitedSameTime(t4, t5).Add(z.v[2].Values.Count.Range().Select(
                    j => ShowComplex(
                        vectorFill,
                        Brushes.Black,
                        z.v[2].Values[j],
                        sw.Select(e => new Point(e.LerpAcross(0.3).X + w, 100 + w) + new Vector(0, j * w * 2)),
                        w)));
            }

            var gateRadius = 15;
            animation.Add(new Animation {
                // left hadamard gate
                new RectDesc(new Rect(x1 - gateRadius, wireY1 - gateRadius, gateRadius*2, gateRadius*2), Brushes.Black, Brushes.White, 1.0),
                new TextDesc("H", new Point(x1, wireY1), new Point(0.5, 0.5), fontWeight: FontWeights.ExtraBold, fontSize: 20),
                // right hadamard gate
                new RectDesc(new Rect(x2 - gateRadius, wireY2 - gateRadius, gateRadius*2, gateRadius*2), Brushes.Black, Brushes.White, 1.0),
                new TextDesc("H", new Point(x2, wireY2), new Point(0.5, 0.5), fontWeight: FontWeights.ExtraBold, fontSize: 20),
            });

            return animation;
        }
        public static Animation CreateCombinedHadamardGateAnimation() {
            var matrixFill = (Brush)Brushes.Orange.LerpToTransparent(0.5);
            var vectorFill = (Brush)Brushes.Blue.LerpToTransparent(0.7);
            var H = ComplexMatrix.MakeUnitaryHadamard(2);
            var v0 = new ComplexVector(new Complex[] { 1, 0, 0, 0 });
            var v1 = v0 * H;
            var v2 = v1 * H;
            var u0 = new ComplexVector(new Complex[] { 0, 0, 0, 1 });
            var u1 = u0 * H;
            var u2 = u1 * H;

            var m = 150.0;
            var w = m / (v0.Values.Count + 2) / 2;
            var m2 = m / 2;
            var span = 500;
            var x1 = 100;
            var x2 = x1 + span / 2;

            var duration = 5.Seconds();
            var sw = Ani.Anon(t => new Point(t.DividedBy(duration).LerpTransition(-w * 2, span - w * 2), 0).Sweep(new Vector(0, 1000)));
            var t1 = duration.Times((x1 - m2 + w * 2) / span);
            var t2 = duration.Times((x1 + m2) / span);
            var t3 = duration.Times((x2 - m2 + w * 2) / span);
            var t4 = duration.Times((x2 + m2) / span);
            var t5 = duration;

            var wireY1 = 45;
            var wireY2 = 70;
            var animation = new Animation {
                new TextDesc("Combined Hadamard Gate",
                             new Point((x1+x2)/2.0, 5),
                             new Point(0.5, 0),
                             fontSize: 15,
                             foreground: Brushes.Gray),
                // wire
                new LineSegmentDesc(new Point(0, wireY1).Sweep(new Vector(1000, 0))),
                new LineSegmentDesc(new Point(0, wireY2).Sweep(new Vector(1000, 0)))
            };

            var period = animation.Periodic(duration);
            var period2 = animation.Dilated(1.Seconds(), zero: duration.DividedBy(2)).Periodic(duration);
            var halfPeriod = animation.Periodic(duration.DividedBy(2));

            // sweep line
            period.Add(new LineSegmentDesc(sw, Brushes.Red, 0.5, 4));
            period2.Add(new LineSegmentDesc(sw, Brushes.Red, 0.5, 4));

            // matrices (not shown during multiplication)
            var xx = halfPeriod.LimitedSameTime(0.Seconds(), t1);
            var xx2 = halfPeriod.LimitedSameTime(t2, t3);
            var m1 = ShowMatrix(new Rect(x1 - m2, 100, m2 * 2, m2 * 2),
                                H,
                                matrixFill,
                                Brushes.Black);
            var m_2 = ShowMatrix(new Rect(x2 - m2, 100, m2 * 2, m2 * 2),
                                 H,
                                 matrixFill,
                                 Brushes.Black);
            xx.Add(m1);
            xx.Add(m_2);
            xx2.Add(m1);
            xx2.Add(m_2);

            // matrix multiplications
            period.LimitedNewTime(t1, t2).Add(
                ShowMatrixMultiplication(
                        t2 - t1,
                        new Rect(x1 - m2, 100, m2 * 2, m2 * 2),
                        H,
                        v0,
                        matrixFill,
                        vectorFill,
                        Brushes.Black));
            period.LimitedNewTime(t3, t4).Add(
                ShowMatrixMultiplication(
                    t4 - t3,
                    new Rect(x2 - m2, 100, m2 * 2, m2 * 2),
                    H,
                    v1,
                    matrixFill,
                    vectorFill,
                    Brushes.Black));
            period2.LimitedNewTime(t1, t2).Add(
                ShowMatrixMultiplication(
                        t2 - t1,
                        new Rect(x1 - m2, 100, m2 * 2, m2 * 2),
                        H,
                        u0,
                        matrixFill,
                        vectorFill,
                        Brushes.Black));
            period2.LimitedNewTime(t3, t4).Add(
                ShowMatrixMultiplication(
                    t4 - t3,
                    new Rect(x2 - m2, 100, m2 * 2, m2 * 2),
                    H,
                    u1,
                    matrixFill,
                    vectorFill,
                    Brushes.Black));

            animation.Add(new TextDesc(
                Ani.Anon(t => t.Mod(duration)).Select(t => t < t1.LerpTo(t2, 0.5) ? "On" : t < t3.LerpTo(t4, 0.5) ? "Mixed" : "On"),
                Ani.Anon(t => new Point(t.DividedBy(duration).ProperMod(1).LerpTransition(-w * 2, span - w * 2), wireY1))));
            animation.Add(new TextDesc(
                Ani.Anon(t => t.Mod(duration)).Select(t => t < t1.LerpTo(t2, 0.5) ? "Off" : t < t3.LerpTo(t4, 0.5) ? "Mixed" : "Off"),
                Ani.Anon(t => new Point(t.DividedBy(duration).ProperMod(1).LerpTransition(-w * 2, span - w * 2), wireY2))));

            animation.Add(new TextDesc(
                Ani.Anon(t => (t + duration.DividedBy(2)).Mod(duration)).Select(t => t < t1.LerpTo(t2, 0.5) ? "Off" : t < t3.LerpTo(t4, 0.5) ? "Mixed" : "Off"),
                Ani.Anon(t => new Point((t.DividedBy(duration) + 0.5).ProperMod(1).LerpTransition(-w * 2, span - w * 2), wireY1))));
            animation.Add(new TextDesc(
                Ani.Anon(t => (t + duration.DividedBy(2)).Mod(duration)).Select(t => t < t1.LerpTo(t2, 0.5) ? "Off" : t < t3.LerpTo(t4, 0.5) ? "Mixed" : "Off"),
                Ani.Anon(t => new Point((t.DividedBy(duration) + 0.5).ProperMod(1).LerpTransition(-w * 2, span - w * 2), wireY2))));

            // show vector being 'pushed' by sweep line
            foreach (var z in new[] { new { p = period, v = new[] { v0, v1, v2 } }, new { p = period2, v = new[] { u0, u1, u2 } } }) {
                z.p.LimitedSameTime(0.Seconds(), t1).Add(z.v[0].Values.Count.Range().Select(
                    j => ShowComplex(
                        vectorFill,
                        Brushes.Black,
                        z.v[0].Values[j],
                        sw.Select(e => new Point(e.LerpAcross(0.3).X + w, 100 + w) + new Vector(0, j * w * 2)),
                        w)));
                z.p.LimitedSameTime(t2, t3).Add(z.v[1].Values.Count.Range().Select(
                    j => ShowComplex(
                        vectorFill,
                        Brushes.Black,
                        z.v[1].Values[j],
                        sw.Select(e => new Point(e.LerpAcross(0.3).X + w, 100 + w) + new Vector(0, j * w * 2)),
                        w)));
                z.p.LimitedSameTime(t4, t5).Add(z.v[2].Values.Count.Range().Select(
                    j => ShowComplex(
                        vectorFill,
                        Brushes.Black,
                        z.v[2].Values[j],
                        sw.Select(e => new Point(e.LerpAcross(0.3).X + w, 100 + w) + new Vector(0, j * w * 2)),
                        w)));
            }

            var gateRadius = 15;
            animation.Add(new Animation {
                // left hadamard gate
                new RectDesc(new Rect(x1 - gateRadius*2, wireY1/2+wireY2/2 - gateRadius*2, gateRadius*4, gateRadius*4), Brushes.Black, Brushes.White, 1.0),
                new TextDesc("H", new Point(x1-gateRadius*1.3, wireY1), new Point(0.5, 0.5), fontWeight: FontWeights.ExtraBold, fontSize: 12, foreground: Brushes.Gainsboro),
                new TextDesc("H", new Point(x1+gateRadius*1.3, wireY2), new Point(0.5, 0.5), fontWeight: FontWeights.ExtraBold, fontSize: 12, foreground: Brushes.Gainsboro),
                new TextDesc("H₂", new Point(x1, wireY1/2+wireY2/2), new Point(0.5, 0.5), fontWeight: FontWeights.ExtraBold, fontSize: 20),
                // right hadamard gate
                new RectDesc(new Rect(x2 - gateRadius*2, wireY1/2+wireY2/2 - gateRadius*2, gateRadius*4, gateRadius*4), Brushes.Black, Brushes.White, 1.0),
                new TextDesc("H", new Point(x2-gateRadius*1.3, wireY1), new Point(0.5, 0.5), fontWeight: FontWeights.ExtraBold, fontSize: 12, foreground: Brushes.Gainsboro),
                new TextDesc("H", new Point(x2+gateRadius*1.3, wireY2), new Point(0.5, 0.5), fontWeight: FontWeights.ExtraBold, fontSize: 12, foreground: Brushes.Gainsboro),
                new TextDesc("H₂", new Point(x2, wireY1/2+wireY2/2), new Point(0.5, 0.5), fontWeight: FontWeights.ExtraBold, fontSize: 20),
            });

            return animation;
        }
        public static Animation CreateGroverDiffusionAnimation() {
            var matrixFill = (Brush)Brushes.Orange.LerpToTransparent(0.5);
            var vectorFill = (Brush)Brushes.Blue.LerpToTransparent(0.7);
            var H = ComplexMatrix.MakeUnitaryHadamard(2);
            var v0 = new ComplexVector(new Complex[] { 1, 0, 0, 0 });
            var v1 = v0 * H;
            var v2 = v1 * H;
            var u0 = new ComplexVector(new Complex[] { 0, 0, 0, 1 });
            var u1 = u0 * H;
            var u2 = u1 * H;

            var m = 150.0;
            var w = m / (v0.Values.Count + 2) / 2;
            var m2 = m / 2;
            var span = 500;
            var x1 = 100;
            var x2 = x1 + span / 2;
            var xc = (x1 + x2)/2;

            var duration = 5.Seconds();
            var sw = Ani.Anon(t => new Point(t.DividedBy(duration).LerpTransition(-w * 2, span - w * 2), 0).Sweep(new Vector(0, 1000)));
            var t1 = duration.Times((x1 - m2 + w * 2) / span);
            var t2 = duration.Times((x1 + m2) / span);
            var t3 = duration.Times((x2 - m2 + w * 2) / span);
            var t4 = duration.Times((x2 + m2) / span);
            var t5 = duration;

            var wireY1 = 45;
            var wireY2 = 70;
            var animation = new Animation {
                new TextDesc("Grover Diffusion Operator: Invert Around the Average",
                             new Point((x1+x2)/2.0, 5),
                             new Point(0.5, 0),
                             fontSize: 15,
                             foreground: Brushes.Gray),
                // wire
                new LineSegmentDesc(new Point(0, wireY1).Sweep(new Vector(1000, 0))),
                new LineSegmentDesc(new Point(0, wireY2).Sweep(new Vector(1000, 0)))
            };

            var period = animation.Periodic(duration);
            var period2 = animation.Dilated(1.Seconds(), zero: duration.DividedBy(2)).Periodic(duration);
            var halfPeriod = animation.Periodic(duration.DividedBy(2));

            // sweep line
            period.Add(new LineSegmentDesc(sw, Brushes.Red, 0.5, 4));
            period2.Add(new LineSegmentDesc(sw, Brushes.Red, 0.5, 4));

            // matrices (not shown during multiplication)
            var xx = halfPeriod.LimitedSameTime(0.Seconds(), t1);
            var xx2 = halfPeriod.LimitedSameTime(t2, t3);
            var m1 = ShowMatrix(new Rect(x1 - m2, 100, m2 * 2, m2 * 2),
                                H,
                                matrixFill,
                                Brushes.Black);
            var m_2 = ShowMatrix(new Rect(x2 - m2, 100, m2 * 2, m2 * 2),
                                 H,
                                 matrixFill,
                                 Brushes.Black);
            xx.Add(m1);
            xx.Add(m_2);
            xx2.Add(m1);
            xx2.Add(m_2);

            // matrix multiplications
            period.LimitedNewTime(t1, t2).Add(
                ShowMatrixMultiplication(
                        t2 - t1,
                        new Rect(x1 - m2, 100, m2 * 2, m2 * 2),
                        H,
                        v0,
                        matrixFill,
                        vectorFill,
                        Brushes.Black));
            period.LimitedNewTime(t3, t4).Add(
                ShowMatrixMultiplication(
                    t4 - t3,
                    new Rect(x2 - m2, 100, m2 * 2, m2 * 2),
                    H,
                    v1,
                    matrixFill,
                    vectorFill,
                    Brushes.Black));
            period2.LimitedNewTime(t1, t2).Add(
                ShowMatrixMultiplication(
                        t2 - t1,
                        new Rect(x1 - m2, 100, m2 * 2, m2 * 2),
                        H,
                        u0,
                        matrixFill,
                        vectorFill,
                        Brushes.Black));
            period2.LimitedNewTime(t3, t4).Add(
                ShowMatrixMultiplication(
                    t4 - t3,
                    new Rect(x2 - m2, 100, m2 * 2, m2 * 2),
                    H,
                    u1,
                    matrixFill,
                    vectorFill,
                    Brushes.Black));

            animation.Add(new TextDesc(
                Ani.Anon(t => t.Mod(duration)).Select(t => t < t1.LerpTo(t2, 0.5) ? "On" : t < t3.LerpTo(t4, 0.5) ? "Mixed" : "On"),
                Ani.Anon(t => new Point(t.DividedBy(duration).ProperMod(1).LerpTransition(-w * 2, span - w * 2), wireY1))));
            animation.Add(new TextDesc(
                Ani.Anon(t => t.Mod(duration)).Select(t => t < t1.LerpTo(t2, 0.5) ? "Off" : t < t3.LerpTo(t4, 0.5) ? "Mixed" : "Off"),
                Ani.Anon(t => new Point(t.DividedBy(duration).ProperMod(1).LerpTransition(-w * 2, span - w * 2), wireY2))));

            animation.Add(new TextDesc(
                Ani.Anon(t => (t + duration.DividedBy(2)).Mod(duration)).Select(t => t < t1.LerpTo(t2, 0.5) ? "Off" : t < t3.LerpTo(t4, 0.5) ? "Mixed" : "Off"),
                Ani.Anon(t => new Point((t.DividedBy(duration) + 0.5).ProperMod(1).LerpTransition(-w * 2, span - w * 2), wireY1))));
            animation.Add(new TextDesc(
                Ani.Anon(t => (t + duration.DividedBy(2)).Mod(duration)).Select(t => t < t1.LerpTo(t2, 0.5) ? "Off" : t < t3.LerpTo(t4, 0.5) ? "Mixed" : "Off"),
                Ani.Anon(t => new Point((t.DividedBy(duration) + 0.5).ProperMod(1).LerpTransition(-w * 2, span - w * 2), wireY2))));

            // show vector being 'pushed' by sweep line
            foreach (var z in new[] { new { p = period, v = new[] { v0, v1, v2 } }, new { p = period2, v = new[] { u0, u1, u2 } } }) {
                z.p.LimitedSameTime(0.Seconds(), t1).Add(z.v[0].Values.Count.Range().Select(
                    j => ShowComplex(
                        vectorFill,
                        Brushes.Black,
                        z.v[0].Values[j],
                        sw.Select(e => new Point(e.LerpAcross(0.3).X + w, 100 + w) + new Vector(0, j * w * 2)),
                        w)));
                z.p.LimitedSameTime(t2, t3).Add(z.v[1].Values.Count.Range().Select(
                    j => ShowComplex(
                        vectorFill,
                        Brushes.Black,
                        z.v[1].Values[j],
                        sw.Select(e => new Point(e.LerpAcross(0.3).X + w, 100 + w) + new Vector(0, j * w * 2)),
                        w)));
                z.p.LimitedSameTime(t4, t5).Add(z.v[2].Values.Count.Range().Select(
                    j => ShowComplex(
                        vectorFill,
                        Brushes.Black,
                        z.v[2].Values[j],
                        sw.Select(e => new Point(e.LerpAcross(0.3).X + w, 100 + w) + new Vector(0, j * w * 2)),
                        w)));
            }

            var gateRadius = 15;
            animation.Add(new Animation {
                // left hadamard gate
                new RectDesc(new Rect(x1 - gateRadius*2, wireY1/2+wireY2/2 - gateRadius*2, gateRadius*4, gateRadius*4), Brushes.Black, Brushes.White, 1.0),
                new TextDesc("H", new Point(x1, wireY1/2+wireY2/2), new Point(0.5, 0.5), fontWeight: FontWeights.ExtraBold, fontSize: 20),
                // center operation
                new RectDesc(new Rect(xc - gateRadius*2, wireY1/2+wireY2/2 - gateRadius*2, gateRadius*4, gateRadius*4), Brushes.Black, Brushes.White, 1.0),
                new TextDesc("I-²·.", new Point(xc, wireY1/2+wireY2/2), new Point(0.5, 0.5), fontWeight: FontWeights.ExtraBold, fontSize: 20),
                // right hadamard gate
                new RectDesc(new Rect(x2 - gateRadius*2, wireY1/2+wireY2/2 - gateRadius*2, gateRadius*4, gateRadius*4), Brushes.Black, Brushes.White, 1.0),
                new TextDesc("H", new Point(x2, wireY1/2+wireY2/2), new Point(0.5, 0.5), fontWeight: FontWeights.ExtraBold, fontSize: 20),
            });

            return animation;
        }
    }
}