using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Animatron;
using SnipSnap.Mathematics;
using Strilanc.Value;
using TwistedOak.Element.Env;
using Strilanc.LinqToCollections;

namespace Animations {
    public static class Concurrency {
        private delegate Tuple<Animation, Ani<Size>> Animator<T>(Ani<T> value, Ani<bool> visible, Ani<Point> topLeft);

        public static Ani<Rect> WithTopLeft(this Ani<Size> size, Ani<Point> point) {
            return size.Combine(point, (s, p) => new Rect(p, s));
        }
        public static Ani<Rect> SweepRightAndDown(this Ani<Point> point, Ani<Size> size) {
            return size.Combine(point, (s, p) => new Rect(p, s));
        }
        public static Ani<Rect> BoundingBox(this Ani<Rect> rect1, Ani<Rect> rect2) {
            return rect1.Combine(rect2,
                                 (r1, r2) => new Rect(
                                     new Point(r1.X.Min(r2.X), r1.Y.Min(r2.Y)), 
                                     new Point(r1.Right.Max(r2.Right), r1.Bottom.Max(r2.Bottom))));
        }

        private static Animator<string> MakeTextAnimator(Ani<Brush> foreground = null, Ani<double> fontSize = null, Ani<FontWeight> fontWeight = null) {
            var t = new TextBlock();
            return (value, visible, topLeft) => Tuple.Create(
                new Animation {
                    new TextDesc(
                        value.Combine(visible, (v1, v2) => v2 ? (v1 ?? "") : ""), 
                        topLeft, 
                        new Point(0,0),
                        fontSize: fontSize, 
                        fontWeight: fontWeight, 
                        foreground: foreground)
                },
                value.Select(e => {
                    t.Text = e;
                    t.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    return t.DesiredSize;
                }));
        }
        struct MemoryPoint {
            public May<int> X;
            public May<int> Y;
        }
        private static Animation MakeLinkAnimation(Ani<Rect> src, Ani<Rect?> dst) {
            var p1 = src.Select(e => new Point(e.Right, e.Top.LerpTo(e.Bottom, 0.5)));
            var p4 = src.Combine(dst, (s, d) => {
                var cy = d.GetValueOrDefault().Top.LerpTo(d.GetValueOrDefault().Bottom, 0.5);
                var sy = s.Top.LerpTo(s.Bottom, 0.5);
                
                return new Point(
                    d.GetValueOrDefault().Right,
                    cy + (sy-cy)/10);
            });
            var xmax = p1.Combine(p4, (e1, e2) => e1.X.Max(e2.X) + 10);

            var p2 = p1.Combine(xmax, (e, x) => new Point(x, e.Y));
            var p3 = p4.Combine(xmax, (e, x) => new Point(x, e.Y));
            var p3a = p4.Select(e => e + new Vector(2, 2));
            var p3b = p4.Select(e => e + new Vector(2, -2));
            var stroke = dst.Select(e => e.HasValue ? (Brush)Brushes.Black : Brushes.Transparent);
            return new Animation {
                new LineSegmentDesc(p1.Combine(p2, (e1, e2) => e1.To(e2)), stroke: stroke),
                new LineSegmentDesc(p2.Combine(p3, (e1, e2) => e1.To(e2)), stroke: stroke),
                new LineSegmentDesc(p3.Combine(p4, (e1, e2) => e1.To(e2)), stroke: stroke),
                new LineSegmentDesc(p3a.Combine(p4, (e1, e2) => e1.To(e2)), stroke: stroke),
                new LineSegmentDesc(p3b.Combine(p4, (e1, e2) => e1.To(e2)), stroke: stroke),
            };
        }
        private static Animator<string> MakeBoxValueAnimator(string name) {
            return (value, visible, topLeft) => {
                var brush = value.Select(e =>
                    e == null ? (Brush)Brushes.Red
                    : Brushes.Black);
                var rx = MakeTextAnimator(brush, fontWeight: value.Select(e => e != null ? FontWeights.Normal : FontWeights.Bold))(
                    value.Select(e => e ?? "???"),
                    visible,
                    topLeft.Select(e => new Point(e.X + 5, e.Y + 5)));
                var size = rx.Item2.Select(e => new Size(e.Width.Max(50), e.Height + 8));
                var box = size.WithTopLeft(topLeft);
                var boxAnim = new RectDesc(
                    box,
                    stroke: visible.Select(e => e ? (Brush)Brushes.Black : Brushes.Transparent),
                    strokeThickness: 1);
                return Tuple.Create(
                    new Animation {
                        new TextDesc(name, topLeft, new Point(0, 1)),
                        rx.Item1, 
                        boxAnim,
                    },
                    size);
            };
        }
        private static Animator<Tuple<int, Rect>> MakePointerAnimator(string name) {
            return (value, visible, topLeft) => {
                var brush = value.Select(e => 
                    e == null ? (Brush)Brushes.Red
                    : e.Item1 == 0 ? Brushes.DarkGray
                    : Brushes.Black);
                var rx = MakeTextAnimator(brush, fontWeight: value.Select(e => e != null && e.Item1 != 0 ? FontWeights.Normal : FontWeights.Bold))(
                    value.Select(e => 
                        e == null ? "???" 
                        : e.Item1 == 0 ? "null" 
                        : "0x" + e.Item1.ToString("x")), 
                    visible, 
                    topLeft.Select(e => new Point(e.X + 5, e.Y + 5)));
                var size = rx.Item2.Select(e => new Size(e.Width.Max(50), e.Height + 8));
                var box = size.WithTopLeft(topLeft);
                var boxAnim = new RectDesc(
                    box, 
                    stroke: visible.Select(e => e ? (Brush)Brushes.Black : Brushes.Transparent), 
                    strokeThickness: 1);
                return Tuple.Create(
                    new Animation {
                        new TextDesc(name, topLeft, new Point(0, 1)),
                        rx.Item1, 
                        boxAnim,
                        MakeLinkAnimation(box, value.Select(e => e == null || e.Item1 == 0 ? (Rect?)null : e.Item2))
                    },
                    size);
            };
        }
        private static Animator<MemoryPoint> MakePointMemoryAnimator(int address) {
            return (value, visible, topLeft) => {
                var rx = MakeTextAnimator(value.Select(e => e.X.HasValue ? (Brush)Brushes.Black : Brushes.Red), fontWeight: value.Select(e => e.X.HasValue ? FontWeights.Normal : FontWeights.Bold))(
                    value.Select(e => e.X.Select(f => "X = " + f).Else("X = ???")),
                    visible,
                    topLeft.Select(e => new Point(e.X + 5, e.Y + 5)));
                var ry = MakeTextAnimator(value.Select(e => e.Y.HasValue ? (Brush)Brushes.Black : Brushes.Red), fontWeight: value.Select(e => e.Y.HasValue ? FontWeights.Normal : FontWeights.Bold))(
                    value.Select(e => e.Y.Select(f => "Y = " + f).Else("Y = ???")),
                    visible,
                    topLeft.Combine(rx.Item2, (p, s) => new Point(p.X + 5, p.Y + s.Height + 5)));
                var box = rx.Item2.Combine(ry.Item2, (xx, yy) => new Size(xx.Width.Max(yy.Width).Max(50), xx.Height + yy.Height + 8));
                var boxAnim = new RectDesc(
                    box.WithTopLeft(topLeft),
                    stroke: visible.Select(e => e ? (Brush)Brushes.Black : Brushes.Transparent),
                    strokeThickness: 1);
                return Tuple.Create(
                    new Animation {
                        new TextDesc("0x" + address.ToString("x"), topLeft, new Point(0, 1)),
                        rx.Item1, 
                        ry.Item1, 
                        boxAnim
                    },
                    box);
            };
        }
        public static Animation CreateDoubleCheckedLockingAnimation() {
            var ani = new Animation();
            var address1 = 0x100;

            var t = ani.Periodic(8.Seconds());

            var n = 14.0;
            var t_writeP1 = 1/n;
            var t_writeX =  2/n;
            var t_writeY =  3/n;
            var t_writeS =  4/n;
            var t_cacheY =  5/n;
            var t_cacheX =  6/n;
            var t_cacheS =  7/n;
            var t_uncacheS = 8 / n;
            var t_checkS = 9 / n;
            var t_readX = 10/n;
            var t_garbage = 11/n;

            var thread1Value = t.Proper.Select(e =>
                e < t_writeX ? new MemoryPoint { X = May<int>.NoValue, Y = May<int>.NoValue }
                : e < t_writeY ? new MemoryPoint { X = 1, Y = May<int>.NoValue }
                : new MemoryPoint { X = 1, Y = 1 });
            var cacheValue = t.Proper.Select(e =>
                e < t_cacheY ? new MemoryPoint { X = May<int>.NoValue, Y = May<int>.NoValue }
                : e < t_cacheX ? new MemoryPoint { X = May<int>.NoValue, Y = 1 }
                : new MemoryPoint { X = 1, Y = 1 });
            var thread2Value = t.Proper.Select(e =>
                new MemoryPoint { X = May<int>.NoValue, Y = May<int>.NoValue });

            var sideMargin = 10;
            var thread1X = sideMargin;
            var cacheX = 107;
            var thread2X = 210;
            var thread2X2 = 210+50;
            var rightX = thread2X2 + sideMargin*2;
            //var centerX = rightX / 2;

            //t.Add(new TextDesc(
            //    "No Acquire ⇒ Garbage",
            //    new Point(centerX, 5),
            //    new Point(0.5, 0),
            //    //fontWeight: FontWeights.SemiBold,
            //    fontSize: 18));
            var mainY = 16;
            var sectionLabelY = -15 + mainY;
            var dividerY3 = -16 + mainY;
            var dividerY1 = mainY;
            var pointerY = 15 + mainY;
            var valueY = 55 + mainY;
            var localPointerY = 110 + mainY;
            var resultY = 110 + mainY;
            var dividerY2 = 140 + mainY;
            var topInstructionY = 145 + mainY;

            var p1 = new Point(thread1X, valueY);
            var p2 = new Point(cacheX, valueY);
            var p3 = new Point(thread2X, valueY);

            var v1 = MakePointMemoryAnimator(address1)(thread1Value, true, p1);
            var v2 = MakePointMemoryAnimator(address1)(cacheValue, true, p2);
            var v3 = MakePointMemoryAnimator(address1)(thread2Value, true, p3);

            var b1 = v1.Item2.WithTopLeft(p1);
            var b2 = v2.Item2.WithTopLeft(p2);
            var b3 = v3.Item2.WithTopLeft(p3);

            var thread1LocalPointer = t.Proper.Combine(b1, (e, b) =>
                e < t_writeP1 ? Tuple.Create(0, b)
                : Tuple.Create(address1, b));
            var thread1Pointer = t.Proper.Combine(b1, (e, b) =>
                e < t_writeS ? Tuple.Create(0, b)
                : Tuple.Create(address1, b));
            var cachePointer = t.Proper.Combine(b2, (e, b) =>
                e < t_cacheS ? Tuple.Create(0, b)
                : Tuple.Create(address1, b));
            var thread2Pointer = t.Proper.Combine(b3, (e, b) =>
                e < t_uncacheS ? Tuple.Create(0, b)
                : Tuple.Create(address1, b));

            var thread2CopyX = t.Proper.Select(
                e => e < t_readX ? "-" : null);

            var t1LocPtrPt = new Point(thread1X, localPointerY);
            var t1PtrPt = new Point(thread1X, pointerY);
            var cachePtrPt = new Point(cacheX, pointerY);
            var t2PtrPt = new Point(thread2X, pointerY);
            var t2RetPt = new Point(thread2X, resultY);

            var v4 = MakePointerAnimator("p")(thread1LocalPointer, true, t1LocPtrPt);
            var v6 = MakePointerAnimator("s")(thread1Pointer, true, t1PtrPt);
            var v7 = MakePointerAnimator("s")(cachePointer, true, cachePtrPt);
            var v8 = MakePointerAnimator("s")(thread2Pointer, true, t2PtrPt);
            var v9 = MakeBoxValueAnimator("returned")(thread2CopyX, true, t2RetPt);

            var t1LocPtrR = v4.Item2.WithTopLeft(t1LocPtrPt);
            var t1PtrR = v6.Item2.WithTopLeft(t1PtrPt);
            var cachePtrR = v7.Item2.WithTopLeft(cachePtrPt);
            var t2PtrR = v8.Item2.WithTopLeft(t2PtrPt);
            var t2RetR = v9.Item2.WithTopLeft(t2RetPt);

            var times = new[] {
                Tuple.Create(t_writeP1, t1LocPtrR, b1, "p = new Point()", 4),
                Tuple.Create(t_writeX, b1, (Ani<Rect>)null, "p.X = 1", 5),
                Tuple.Create(t_writeY, b1, (Ani<Rect>)null, "p.Y = 1", 6),
                Tuple.Create(t_writeS, t1PtrR, t1LocPtrR, "s = p", 7),
                Tuple.Create(t_cacheY, b2, b1, "[write] s.Y", 8),
                Tuple.Create(t_cacheX, b2, b1, "[write] s.X", 8),
                Tuple.Create(t_cacheS, cachePtrR, t1PtrR, "[write] s", 8),
                Tuple.Create(t_uncacheS, t2PtrR, cachePtrR, "[read] s", -1),
                Tuple.Create(t_checkS, t2PtrR, (Ani<Rect>)null, "s != null", -1),
                Tuple.Create(t_readX, t2RetR, b3, "return s.X", -10),
                Tuple.Create(t_garbage, t2RetR, (Ani<Rect>)null, "[garbage]", 0),
                Tuple.Create(1.01, t2RetR, (Ani<Rect>)null, "[read] s.X", 0),
                Tuple.Create(1.01, t2RetR, (Ani<Rect>)null, "[read] s.Y", 0)
            };

            //var cur1 = 4;
            //var cur2 = 0;
            foreach (var i in times.Length.Range()) {
                var visible = t.Proper.Select(e => {
                    //var prevT = i == 0 ? 0 : times[i - 1].Item1;
                    var nextT = i == times.Length - 1 ? 1.01 : times[i + 1].Item1;
                    var curT = times[i].Item1;
                    var fade = 0.01;
                    if (e < curT-fade) return 0;
                    if (e >= curT && e < nextT - fade) return 1.0;
                    if (e > nextT) return 0;
                    if (e < curT) return (e - curT + fade) / fade;
                    return (nextT - e) / fade;
                });
                var margin = 0;

                //if (times[i].Item5 > 0) cur1 = times[i].Item5;
                //if (times[i].Item5 < 0) cur2 = -times[i].Item5;

                //var inst1 = cur1;
                //var inst2 = cur2;

                t.Add(new RectDesc(
                    times[i].Item2.Select(e => new Rect(e.X - margin, e.Y - margin, e.Width + margin * 2, e.Height + margin * 2)),
                    fill: visible.Select(e => (Brush)Brushes.Red.LerpToTransparent(0.5 + 0.5 * (1 - e)))));
                if (times[i].Item3 != null) {
                    var line = times[i].Item2.Combine(times[i].Item3, (r1, r2) => {
                        var c1 = r1.TopLeft.LerpTo(r1.BottomRight, 0.5);
                        var c2 = r2.TopLeft.LerpTo(r2.BottomRight, 0.5);
                        return c1.LerpTo(c2, 0.1).To(c2.LerpTo(c1, 0.1));
                    });
                    t.Add(
                        new LineSegmentDesc(
                            line,
                            visible.Select(e => (Brush)Brushes.Black.LerpToTransparent(1-e)),
                            1));
                    t.Add(
                        new LineSegmentDesc(
                            line.Select(e => e.Start.Sweep((e.Delta + e.Delta.PerpClockwise()).Normal()*10)),
                            visible.Select(e => (Brush)Brushes.Black.LerpToTransparent(1 - e)),
                            1));
                    t.Add(
                        new LineSegmentDesc(
                            line.Select(e => e.Start.Sweep((e.Delta + e.Delta.PerpCounterClockwise()).Normal() * 10)),
                            visible.Select(e => (Brush)Brushes.Black.LerpToTransparent(1 - e)),
                            1));

                    t.Add(new RectDesc(
                        times[i].Item3.Select(e => new Rect(e.X - margin, e.Y - margin, e.Width + margin * 2, e.Height + margin * 2)),
                        fill: visible.Select(e => (Brush)Brushes.Blue.LerpToTransparent(0.75+(1-e)*0.25))));
                }

                var localinsty = (i <= 6 ? i : i - 7) * 15 + topInstructionY;
                t.Add(
                    new TextDesc(times[i].Item4,
                        times[i].Item2.Select(e => new Point(e.X == thread2X ? thread2X2 : thread1X, localinsty)),
                        times[i].Item2.Select(e => new Point(e.X == thread2X ? 1 : 0, 0)),
                        fontSize: 12,
                        foreground: Brushes.Black));
                t.Add(
                    new RectDesc(
                        times[i].Item2.Select(e => new Rect(e.X == thread2X ? thread2X2 - 52 : thread1X, localinsty, e.X == thread2X ? 52 : 83, 15)),
                        fill: visible.Select(e => e > 0.5 ? (Brush)(Brushes.Black).LerpToTransparent(0.75) : Brushes.Transparent)));

                //if (cur1 > 0) {
                //    t.Add(new RectDesc(
                //        new Rect(cacheX, 200-15+inst1*15, 100, 15),
                //        fill: visible.Select(e => e > 0.5 ? (Brush)Brushes.Red.LerpToTransparent(0.5) : Brushes.Transparent)));
                //}
                //if (cur2 > 0) {
                //    t.Add(new RectDesc(
                //        new Rect(cacheX, 200 - 15 + inst2 * 15, 100, 15),
                //        fill: visible.Select(e => e > 0.5 ? (Brush)Brushes.Blue.LerpToTransparent(0.5) : Brushes.Transparent)));
                //}
            }

            //var codeY = 200;
            //var codeN = 0;
            //Func<Point> makeCodeP = () => new Point(cacheX, codeY + codeN++*15);

            t.Add(
                v1.Item1,
                v2.Item1,
                v3.Item1,
                v4.Item1,
                v6.Item1,
                v7.Item1,
                v8.Item1,
                v9.Item1,

                //new TextDesc("var p = s", makeCodeP(), new Point(0, 0)),
                //new TextDesc("if (p == nil) {", makeCodeP(), new Point(0, 0)),
                //new TextDesc("  lock {", makeCodeP(), new Point(0, 0)),
                //new TextDesc("    p = new Point()", makeCodeP(), new Point(0, 0)),
                //new TextDesc("    p.X = 1", makeCodeP(), new Point(0, 0)),
                //new TextDesc("    p.Y = 1", makeCodeP(), new Point(0, 0)),
                //new TextDesc("    s = p", makeCodeP(), new Point(0, 0)),
                //new TextDesc("  }", makeCodeP(), new Point(0, 0)),
                //new TextDesc("}", makeCodeP(), new Point(0, 0)),
                //new TextDesc("return p.X", makeCodeP(), new Point(0, 0)),

                new TextDesc("Producer", new Point(thread1X, sectionLabelY), new Point(0, 0), fontWeight: FontWeights.SemiBold, fontSize: 12),
                new TextDesc("Memory", new Point(cacheX + 28, sectionLabelY), new Point(0.5, 0), fontWeight: FontWeights.SemiBold, fontSize: 12),
                new TextDesc("Consumer", new Point(thread2X2, sectionLabelY), new Point(1, 0), fontWeight: FontWeights.SemiBold, fontSize: 12),

                new LineSegmentDesc(new Point(0, dividerY3).Sweep(new Vector(rightX, 0)), Brushes.Black, 1),
                new LineSegmentDesc(new Point(0, dividerY2).Sweep(new Vector(rightX, 0)), Brushes.Black, 1),
                new LineSegmentDesc(new Point(0, dividerY1).Sweep(new Vector(rightX, 0)), Brushes.Black, 1)
            );
            return ani;
        }
    }
}