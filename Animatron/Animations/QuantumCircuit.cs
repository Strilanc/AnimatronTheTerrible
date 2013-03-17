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
    public static class QuantumCircuit {
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
                                        Ani<Turn> rotation = null,
                                        Ani<Brush> sweepFill = null,
                                        Ani<double> squish = null) {
            phaseOffset = phaseOffset ?? 0;
            var phaseRadius = value.Select(e => Math.Max(0.05, e.Magnitude));
            rotation = rotation ?? Turn.Zero;
            sweepFill = sweepFill ?? fill;
            squish = squish ?? 1.0;
            var mag = value.Select(e => e.Magnitude*e.Magnitude);
            return new Animation {
                new PolygonDesc(
                    phaseOffset.Combine(value, position, unitRadius, phaseRadius, (o, v, p, r, f) => PhaseCurve(v.Phase, f * r*0.5, o).Select(e => e + p).ToArray().AsEnumerable()),
                    stroke: sweepFill.Select(e => e.LerpTo(Brushes.Black, 0.5, lerpAlpha: false)),
                    fill: sweepFill.Select(e => e.LerpToTransparent(0.5)),
                    strokeThickness: value.Select(e => Math.Min(Math.Abs(e.Phase)*10,Math.Min(e.Magnitude*3,1)))),
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
            var r = 50;
            var cy = 100;
            var x1 = 125 / 2;
            var x2 = 350 / 2;
            var x3 = x2 + (x2 - x1);
            var f = 14;

            var animation = new Animation {
                new TextDesc("*", new Point((x1 + x2)/2.0, cy), new Point(0.5, 0.5), fontSize: 20),
                new TextDesc("=", new Point((x3 + x2)/2.0, cy), new Point(0.5, 0.5), fontSize: 20),
                new TextDesc(c1.ToPrettyString(), new Point(x1, cy - r), new Point(0.5, 1), fontSize: f),
                new TextDesc(c2.ToPrettyString(), new Point(x2, cy - r), new Point(0.5, 1), fontSize: f),
                new TextDesc((c1*c2).ToPrettyString(), new Point(x3, cy - r), new Point(0.5, 1), fontSize: f),

                new TextDesc(c1.ToMagPhaseString(), new Point(x1, cy + r), new Point(0.5, 0), fontSize: f),
                new TextDesc(c2.ToMagPhaseString(), new Point(x2, cy + r), new Point(0.5, 0), fontSize: f),
                new TextDesc((c1*c2).ToMagPhaseString(), new Point(x3, cy + r), new Point(0.5, 0), fontSize: f),
                new TextDesc("Complex Product: Multiply Magnitudes, Add Phases",
                             new Point(x2, 5),
                             new Point(0.5, 0),
                             fontSize: 15,
                             foreground: Brushes.Gray),

                ShowComplex(vectorFill,
                            Brushes.Black,
                            c1,
                            new Point(x1, cy),
                            r),
                ShowComplex(matrixFill,
                            Brushes.Black,
                            c2,
                            new Point(x2, cy),
                            r)
            };

            var p = animation.Dilated(0.4.Seconds()).Periodic(10.Seconds());
            var s1 = p.LimitedSameTime(0.Seconds(), 3.Seconds());
            s1.Add(ShowComplex(vectorFill,
                               Brushes.Black,
                               c1,
                               Ani.Anon(
                                   t =>
                                   new Point(x1, cy).SmoothLerpTo(new Point(x3, cy), (s1.Proper.ValueAt(t)*1.5).Min(1))
                                   + new Vector(0, (s1.Proper.ValueAt(t)*1.5).Min(1).LerpTransition(0, -25, 0))),
                               r,
                               rotation: Ani.Anon(t => Turn.FromNaturalAngle(0.0.SmoothLerpTo(Math.PI/2, (s1.Proper.ValueAt(t)*2).Min(1))))));
            s1.Add(ShowComplex(matrixFill,
                               Brushes.Black,
                               c2,
                               Ani.Anon(
                                   t =>
                                   new Point(x2, cy).SmoothLerpTo(new Point(x3, cy), (s1.Proper.ValueAt(t)*1.5).Min(1))
                                   + new Vector(0, (s1.Proper.ValueAt(t)*1.5).Min(1).LerpTransition(0, -25, 0))),
                               r));
            var sb = p.LimitedSameTime(3.Seconds(), 9.Seconds());
            sb.Add(
                ShowComplexProduct(vectorFill,
                                   matrixFill,
                                   Brushes.Black,
                                   Brushes.Black,
                                   c1,
                                   c2,
                                   new Point(x3, cy),
                                   r,
                                   Ani.Anon(t => (sb.Proper.ValueAt(t)*1.2).Min(1)),
                                   vectorFill.LerpTo(matrixFill, 0.5)));

            var s2 = p.LimitedSameTime(9.Seconds(), 9.5.Seconds());
            s2.Add(ShowComplex(Ani.Anon(t => vectorFill.LerpTo(matrixFill, 0.5).LerpToTransparent(s2.Proper.ValueAt(t))),
                               Ani.Anon(t => (Brush)Brushes.Black.LerpToTransparent(s2.Proper.ValueAt(t))),
                               c1*c2,
                               new Point(x3, cy),
                               r));

            return animation;
        }
        public static Animation CreateComplexSumAnimation() {
            var matrixFill = (Brush)Brushes.Orange.LerpToTransparent(0.5);
            var vectorFill = (Brush)Brushes.Blue.LerpToTransparent(0.7);
            var c1 = new Complex(-0.5, -0.5) * Math.Sqrt(0.5);
            var c2 = new Complex(-0.5, 0.5);
            var r = 50;
            var cy = 100;
            var x1 = 125/2;
            var x2 = 350/2;
            var x3 = x2 + (x2-x1);
            var f = 14;

            var animation = new Animation {
                new TextDesc("+", new Point((x1 + x2)/2.0, cy), new Point(0.5, 0.5), fontSize: 20),
                new TextDesc("=", new Point((x3 + x2)/2.0, cy), new Point(0.5, 0.5), fontSize: 20),
                new TextDesc(c1.ToPrettyString(), new Point(x1, cy - r), new Point(0.5, 1), fontSize: f),
                new TextDesc(c2.ToPrettyString(), new Point(x2, cy - r), new Point(0.5, 1), fontSize: f),
                new TextDesc((c1 + c2).ToPrettyString(), new Point(x3, cy - r), new Point(0.5, 1), fontSize: f),

                new TextDesc(c1.ToMagPhaseString(), new Point(x1, cy + r), new Point(0.5, 0), fontSize: f),
                new TextDesc(c2.ToMagPhaseString(), new Point(x2, cy + r), new Point(0.5, 0), fontSize: f),
                new TextDesc((c1 + c2).ToMagPhaseString(), new Point(x3, cy + r), new Point(0.5, 0), fontSize: f),
                new TextDesc("Adding Complex Numbers: Place Arrows End to End",
                             new Point(x2, 5),
                             new Point(0.5, 0),
                             fontSize: 15,
                             foreground: Brushes.Gray),

                ShowComplex(vectorFill,
                            Brushes.Black,
                            c1,
                            new Point(x1, cy),
                            r),
                ShowComplex(matrixFill,
                            Brushes.Black,
                            c2,
                            new Point(x2, cy),
                            r)
            };
            var p = animation.Dilated(0.4.Seconds()).Periodic(10.Seconds());
            p.LimitedSameTime(0.Seconds(), 9.5.Seconds()).Add(ShowComplexSum(
                Brushes.Transparent,
                vectorFill.LerpTo(matrixFill, 0.5),
                Brushes.Black,
                new Ani<Complex>[] {c1, c2},
                new Point(x1, cy),
                new Point(x3, cy),
                new Vector(x2-x1, 0),
                r,
                p.Proper));

            var s2 = p.LimitedSameTime(9.5.Seconds(), 10.Seconds());
            s2.Add(ShowComplex(Ani.Anon(t => vectorFill.LerpTo(matrixFill, 0.5).LerpToTransparent(s2.Proper.ValueAt(t))),
                               Ani.Anon(t => (Brush)Brushes.Black.LerpToTransparent(s2.Proper.ValueAt(t))),
                               c1 + c2,
                               new Point(x3, cy),
                               r));

            return animation;
        }
        public static Animation CreateComplexTransformationAnimation() {
            var matrixFill = (Brush)Brushes.Orange.LerpToTransparent(0.5);
            var vectorFill = (Brush)Brushes.Blue.LerpToTransparent(0.7);

            var s = new Complex(Math.Sqrt(0.5), 0);
            var si = new Complex(0, s.Real);
            var c1 = Complex.FromPolarCoordinates(Math.Sqrt(0.75), 0);
            var c2 = Complex.FromPolarCoordinates(Math.Sqrt(0.2), Math.PI / 4);
            var m = ComplexMatrix.FromCellData(s, si, -s, si);
            var v = new ComplexVector(new[] { c1, c2 });
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

        public static Animation CreateNotGateAnimation() {
            return CreateCircuitAnimation(
                300,
                5.Seconds(),
                new CircuitOperationWithStyle {
                    Operation = ComplexMatrix.FromCellData(0,1,1,0),
                    Description = "Not",
                    Width = 60
                }.Repeat(1),
                new[] {
                    new CircuitInputWithStyle {
                        Color = Brushes.Blue.LerpToTransparent(0.7),
                        Value = new ComplexVector(new Complex[] {1, 0})
                    }
                },
                "Not Gate");
        }
        public static Animation CreateHadamardGateAnimation() {
            return CreateCircuitAnimation(
                600,
                10.Seconds(),
                new CircuitOperationWithStyle {
                    Operation = ComplexMatrix.MakeUnitaryHadamard(1),
                    Description = "H",
                    Width = 40
                }.Repeat(2),
                new[] {
                    new CircuitInputWithStyle {
                        Color = Brushes.Blue.LerpToTransparent(0.7),
                        Value = new ComplexVector(new Complex[] {1, 0})
                    },
                    new CircuitInputWithStyle {
                        Color = Brushes.Red.LerpToTransparent(0.7),
                        Value = new ComplexVector(new Complex[] {0, 1})
                    }
                },
                "Hadamard Gate: From Pure to Mixed and Back");
        }
        public static Animation CreateSeparateWireHadamardGateAnimation() {
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
            return CreateCircuitAnimation(
                600,
                10.Seconds(),
                new[] {
                    new CircuitOperationWithStyle {
                        Operation = H1,
                        Description = "H",
                        Width = 40
                    },
                    new CircuitOperationWithStyle {
                        Operation = H2,
                        Description = "H",
                        Width = 40,
                        MinWire = 1,
                        MaxWire = 1
                    }
                },
                new CircuitInputWithStyle {
                    Color = Brushes.Blue.LerpToTransparent(0.7),
                    Value = new ComplexVector(new Complex[] {1, 0, 0, 0})
                }.Repeat(3),
                "Hadamard Gates on Separate Wires");
        }
        public static Animation CreateCombinedHadamardGateAnimation() {
            return CreateCircuitAnimation(
                500,
                8.Seconds(),
                new CircuitOperationWithStyle {
                    Operation = ComplexMatrix.MakeUnitaryHadamard(2),
                    Description = "H₂",
                    Width = 80,
                    MinWire = 0,
                    MaxWire = 1,
                    WireHints = new[] {"H", "H"}
                }.Repeat(2),
                new[] {
                    new CircuitInputWithStyle {
                        Color = Brushes.Blue.LerpToTransparent(0.7),
                        Value = new ComplexVector(new Complex[] {1, 0, 0, 0})
                    },
                    new CircuitInputWithStyle {
                        Color = Brushes.Red.LerpToTransparent(0.7),
                        Value = new ComplexVector(new Complex[] {0, 0, 0, 1})
                    }
                },
                "Combined Hadamard Gate");
        }
        public static Animation CreateGroverDiffusionAnimation() {
            var H = ComplexMatrix.MakeUnitaryHadamard(2);
            var I2 = ComplexMatrix.FromCellData(
                -1,0,0,0,
                 0,1,0,0,
                 0,0,1,0,
                 0,0,0,1);

            return CreateCircuitAnimation(
                800,
                20.Seconds(),
                new[] {
                    new CircuitOperationWithStyle {
                        Operation = H,
                        Description = "H",
                        Width = 80,
                        MinWire = 0,
                        MaxWire = 1
                    },
                    new CircuitOperationWithStyle {
                        Operation = I2,
                        Description = "I-²·.",
                        Width = 80,
                        MinWire = 0,
                        MaxWire = 1
                    },
                    new CircuitOperationWithStyle {
                        Operation = H,
                        Description = "H",
                        Width = 80,
                        MinWire = 0,
                        MaxWire = 1
                    }
                },
                new CircuitInputWithStyle {
                    Color = Brushes.Blue.LerpToTransparent(0.7),
                    Value = new ComplexVector(new Complex[] { -0.5, 0.5, 0.5, 0.5 })
                }.Repeat(4),
                "Grover Diffusion Operator: Invert Around the Average");
        }
        public static Animation CreateGroverIterationAnimation() {
            var H = ComplexMatrix.MakeUnitaryHadamard(2);
            var I2 = ComplexMatrix.FromCellData(
                -1, 0, 0, 0,
                 0, 1, 0, 0,
                 0, 0, 1, 0,
                 0, 0, 0, 1);
            var D = H*I2*H;
            var U = ComplexMatrix.FromCellData(
                 1, 0, 0, 0,
                 0, 1, 0, 0,
                 0, 0, -1, 0,
                 0, 0, 0, 1);

            return CreateCircuitAnimation(
                600,
                20.Seconds(),
                new[] {
                    new CircuitOperationWithStyle {
                        Operation = U,
                        Description = "Unknown",
                        Width = 150,
                        MinWire = 0,
                        MaxWire = 1,
                        WireHints = new[] {"On", "Off"}
                    },
                    new CircuitOperationWithStyle {
                        Operation = D,
                        Description = "Diffusion",
                        Width = 150,
                        MinWire = 0,
                        MaxWire = 1
                    }
                },
                new CircuitInputWithStyle {
                    Color = Brushes.Blue.LerpToTransparent(0.7),
                    Value = new ComplexVector(new Complex[] { 0.5, 0.5, 0.5, 0.5 })
                }.Repeat(3),
                "Grover Step: Embiggens Amplitude Flipped by Unknown Gate");
        }
        public static Animation CreateFullGroverAnimation(int numWire) {
            var vectorFill = (Brush)Brushes.Blue.LerpToTransparent(0.7);
            var H = ComplexMatrix.MakeUnitaryHadamard(numWire);
            var I2 = ComplexMatrix.MakeSinglePhaseInverter(1 << numWire, 0);
            var D = H * I2 * H;
            var U = ComplexMatrix.MakeSinglePhaseInverter(1 << numWire, 1);
            var S = U*D;
            var input = new ComplexVector(new Complex[] {1}.Concat(Complex.Zero.Repeat((1 << numWire) - 1)).ToArray());

            var steps = (int)Math.Round(Math.PI/4*Math.Sqrt(1 << numWire));
            var GS = new CircuitOperationWithStyle {
                Description = "Step",
                MinWire = 0,
                MaxWire = numWire-1,
                Operation = S,
                Width = 80,
                WireHints=new[] {"Off"}.Concat("On".Repeat(numWire-1)).ToArray()
            };
            return CreateCircuitAnimation(
                900,
                20.Seconds(),
                new[] {
                    new CircuitOperationWithStyle {
                        Description="H",
                        MinWire=0,
                        MaxWire=numWire-1,
                        Operation=H,
                        Width=80
                    }
                }.Concat(GS.Repeat(steps)).ToArray(),
                ReadOnlyList.Repeat(
                    new CircuitInputWithStyle {
                        Value = input,
                        Color = vectorFill
                    },
                    steps+2),
                "Grover's Algorithm",
                showMatrix: false,
                wireSpace: numWire * 80 / 3.0);
        }
        public static Animation CreateInterferometerAnimation() {
            var s = Math.Sqrt(0.5);
            var z = s*Complex.ImaginaryOne;
            var splitter = ComplexMatrix.FromCellData(
                s, z,
                z, s);
            var mirror = ComplexMatrix.MakeIdentity(2)*Complex.ImaginaryOne;

            return CreateCircuitAnimation(
                700,
                20.Seconds(),
                new[] {
                    new CircuitOperationWithStyle {
                        Description = "Splitter",
                        MaxWire = 0,
                        MinWire = 0,
                        Operation = splitter,
                        Width = 80
                    },
                    new CircuitOperationWithStyle {
                        Description = "Mirror",
                        MaxWire = 0,
                        MinWire = 0,
                        Operation = mirror,
                        Width = 80
                    },
                    new CircuitOperationWithStyle {
                        Description = "Splitter",
                        MaxWire = 0,
                        MinWire = 0,
                        Operation = splitter,
                        Width = 80
                    }
                },
                ReadOnlyList.Repeat(
                    new CircuitInputWithStyle {
                        Value = new ComplexVector(new Complex[] {1, 0}),
                        Color = Brushes.Blue.LerpToTransparent(0.7)
                    },
                    4),
                "Mach–Zehnder Interferometer",
                new[] {
                    new[] {"A", "B"},
                    new[] {"C", "D"},
                    new[] {"E", "F"},
                    new[] {"G", "H"}
                }.Repeat(1));
        }
        public static Animation CreateInterferometerDetectorAnimation() {
            var s = Math.Sqrt(0.5);
            var z = s * Complex.ImaginaryOne;
            var splitter = ComplexMatrix.FromCellData(
                s, z, 0, 0,
                z, s, 0, 0,
                0, 0, s, z,
                0, 0, z, s);
            var mirror = ComplexMatrix.MakeIdentity(4) * Complex.ImaginaryOne;
            var detector = ComplexMatrix.FromCellData(
                0, 0, 1, 0,
                0, 1, 0, 0,
                1, 0, 0, 0,
                0, 0, 0, 1);

            var ef = new[] {"E", "F"};
            return CreateCircuitAnimation(
                800,
                20.Seconds(),
                new[] {
                    new CircuitOperationWithStyle {
                        Description = "Splitter",
                        MaxWire = 0,
                        MinWire = 0,
                        Operation = splitter,
                        Width = 80
                    },
                    new CircuitOperationWithStyle {
                        Description = "Mirror",
                        MaxWire = 0,
                        MinWire = 0,
                        Operation = mirror,
                        Width = 80
                    },
                    new CircuitOperationWithStyle {
                        Description = "Detector",
                        MaxWire = 1,
                        MinWire = 0,
                        Operation = detector,
                        Width = 100
                    },
                    new CircuitOperationWithStyle {
                        Description = "Splitter",
                        MaxWire = 0,
                        MinWire = 0,
                        Operation = splitter,
                        Width = 80
                    }
                },
                new CircuitInputWithStyle {
                    Value = new ComplexVector(new Complex[] {1, 0, 0, 0}),
                    Color = Brushes.Blue.LerpToTransparent(0.7)
                }.Repeat(5),
                "Mach–Zehnder Interferometer with Detector",
                new[] {
                    new[] {
                        new[] {"A", "B"},
                        new[] {"C", "D"},
                        ef,
                        ef,
                        new[] {"G", "H"}
                    },
                    new[] {"Off", "On"}.Repeat(5).ToArray()
                });
        }
        public static double SquaredMagnitude(this Complex c) {
            return c.Real*c.Real + c.Imaginary*c.Imaginary;
        }
        public struct CircuitInputWithStyle {
            public ComplexVector Value;
            public Brush Color;
        }
        public struct CircuitOperationWithStyle {
            public ComplexMatrix Operation;
            public string Description;
            public string[] WireHints;
            public int MinWire;
            public int MaxWire;
            public double Width;
        }
        public static Animation CreateCircuitAnimation(double span, TimeSpan duration, IReadOnlyList<CircuitOperationWithStyle> ops, IReadOnlyList<CircuitInputWithStyle> ins, string desc, IReadOnlyList<string[][]> wireLabels = null, string[] stateLabels = null, bool showMatrix = true, double wireSpace = 80) {
            var matrixFill = (Brush)Brushes.Orange.LerpToTransparent(0.5);

            var vals = ins.Select(e => ops.Stream(e.Value, (a, x) => a*x.Operation, streamSeed: true).ToArray()).ToArray();

            var numOp = ops.Count;
            var numIn = ins.Count;
            var maxIn = numOp + 1;
            var numState = ins.First().Value.Values.Count;
            var numWire = (int)Math.Round(Math.Log(numState, 2));
            var matrixWidth = 150.0;
            var cellRadius = matrixWidth / (vals[0][0].Values.Count + 2) / 2;
            var matrixRadius = matrixWidth / 2;
            var opXs = numOp.Range().Select(i => -cellRadius+span*(i+1.0)/maxIn).ToArray();

            var sweepX = Ani.Anon(t => new Point(t.DividedBy(duration).LerpTransition(-cellRadius * 2, span - cellRadius * 2), 0).Sweep(new Vector(0, 1000)));
            var opTs = opXs.Select(x => new {s = duration.Times((x - matrixRadius + cellRadius*2)/span), f = duration.Times((x + matrixRadius)/span)}).ToArray();

            var wireYs = numWire.Range().Select(i => numWire == 1 ? 20 + wireSpace/2 : (wireSpace / 2 + i * wireSpace / 2 / (numWire - 1))).ToArray();
            var animation = new Animation {
                // top description
                new TextDesc(desc,
                             new Point(span/2.0 - (numOp == 1 ? cellRadius : 0), 5),
                             new Point(0.5, 0),
                             fontSize: 15,
                             foreground: Brushes.Gray),
                // wires
                wireYs.Select(y => new LineSegmentDesc(new Point(0, y).Sweep(new Vector(1000, 0))))
            };
            if (showMatrix) {
                // static matrices
                animation.Add(
                    numOp.Range()
                         .Select(i => ShowMatrix(
                             new Rect(opXs[i] - matrixRadius, 100, matrixRadius*2, matrixRadius*2),
                             ops[i].Operation,
                             matrixFill,
                             Brushes.Black)));
            }

            var offsetTimelines =
                maxIn.Range()
                     .Select(
                         i => animation
                                  .Dilated(
                                      1.Seconds(),
                                      -duration.DividedBy(maxIn).Times(i))
                                  .Periodic(duration))
                     .ToArray();

            // sweep line
            foreach (var p in offsetTimelines.Take(numIn)) {
                p.Add(new LineSegmentDesc(sweepX, Brushes.Red, 0.5, 4));
            }

            // state labels
            var tts = opTs.Select(e => e.s.LerpTo(e.f, 0.4));
            var wd = numIn.Range().Select(inid => numWire.Range().Select(wid => (numOp + 1).Range().Select(tid => {
                var label = wireLabels == null ? new[] {"On", "Off"} : wireLabels[wid][tid];
                var p = numState.Range().Where(i => ((i >> wid) & 1) == 0).Select(i => vals[inid][tid].Values[i].SquaredMagnitude()).Sum();
                if (p < 0.001) return label[1];
                if (p < 0.01) return string.Format("~{0}", label[1]);
                if (p < 0.49) return string.Format("~{0}:{1:0}%", label[1], (1 - p) * 100);

                if (p > 0.999) return label[0];
                if (p > 0.99) return string.Format("~{0}", label[0]);
                if (p > 0.51) return string.Format("~{0}:{1:0}%", label[0], p * 100);

                return string.Format("{0}/{1}", label[0], label[1]);
            }).ToArray()).ToArray()).ToArray();
            foreach (var i in numIn.Range()) {
                foreach (var j in numWire.Range()) {
                    animation.Add(
                        new TextDesc(
                            Ani.Anon(t => (t + duration.DividedBy(maxIn).Times(i)).Mod(duration))
                               .Select(t => wd[i][j][tts.TakeWhile(c => t >= c).Count()]),
                            Ani.Anon(
                                t =>
                                new Point((t.DividedBy(duration) + i * 1.0 / maxIn).ProperMod(1).LerpTransition(-cellRadius * 2, span - cellRadius * 2), wireYs[j]))));
                }
            }

            // matrix multiplications
            if (showMatrix) {
                foreach (var i in numIn.Range()) {
                    foreach (var j in numOp.Range()) {
                        var p = offsetTimelines[i].LimitedNewTime(opTs[j].s, opTs[j].f);
                        var r = new Rect(opXs[j] - matrixRadius, 100, matrixRadius*2, matrixRadius*2);
                        p.Add(
                            ShowMatrixMultiplication(
                                opTs[j].f - opTs[j].s,
                                r,
                                ops[j].Operation,
                                vals[i][j],
                                matrixFill,
                                ins[i].Color,
                                Brushes.Black));
                    }
                }
            }

            // pushed values
            if (showMatrix) {
                foreach (var i in numIn.Range()) {
                    foreach (var s in maxIn.Range()) {
                        var p = offsetTimelines[i].LimitedSameTime(s == 0 ? 0.Seconds() : opTs[s - 1].f, s == maxIn - 1 ? duration : opTs[s].s);
                        p.Add(numState.Range().Select(
                            j => ShowComplex(
                                ins[i].Color,
                                Brushes.Black,
                                vals[i][s].Values[j],
                                sweepX.Select(e => new Point(e.LerpAcross(0.3).X + cellRadius, 100 + cellRadius) + new Vector(0, j*cellRadius*2)),
                                cellRadius)));
                    }
                }
            }

            // state labels
            if (showMatrix) {
                wireLabels = wireLabels ?? new[] {"On", "Off"}.Repeat(maxIn).ToArray().Repeat(numWire);
                stateLabels = stateLabels
                              ?? numState.Range().Select(
                                  i => numWire.Range().Select(
                                      w => maxIn.Range().Select(
                                          inid => wireLabels[w][inid][(i >> w) & 1]
                                               ).Distinct().StringJoin("")
                                           ).StringJoin(",")
                                     ).ToArray();
                foreach (var i in numState.Range()) {
                    animation.Add(
                        new TextDesc(
                            stateLabels[i] + ":",
                            new Point(0, 100 + cellRadius + i*2*cellRadius),
                            new Point(0, 0.5)));
                }
            }

            // circuit diagram
            foreach (var i in numOp.Range()) {
                var op = ops[i];
                var h = (numWire == 1 ? 40 : op.MaxWire - op.MinWire == numWire - 1 ? wireSpace : (wireYs[1] - wireYs[0]) * (op.MaxWire - op.MinWire + 1)) * 0.9;
                var r = new Rect(
                    opXs[i] - op.Width/2,
                    (wireYs[op.MaxWire] + wireYs[op.MinWire] - h)/2,
                    op.Width,
                    h);
                animation.Add(
                    new RectDesc(
                        r,
                        Brushes.Black,
                        Brushes.White,
                        1.0));
                animation.Add(
                    new TextDesc(
                        op.Description,
                        r.TopLeft + new Vector(r.Width, r.Height) / 2,
                        new Point(0.5, 0.5),
                        fontWeight: FontWeights.ExtraBold,
                        fontSize: 20));
                if (op.WireHints != null) {
                    foreach (var j in op.WireHints.Length.Range()) {
                        animation.Add(
                            new TextDesc(
                                op.WireHints[j],
                                new Point(r.Left+5, wireYs[op.MinWire + j]),
                                new Point(0, 0.5),
                                foreground: Brushes.Gray,
                                fontSize: 10));
                    }
                }
            }

            return animation;
        }
    }
}