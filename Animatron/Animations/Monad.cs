using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Animatron;
using SnipSnap.Mathematics;
using Strilanc.Angle;
using Strilanc.Value;
using TwistedOak.Util;
using Strilanc.LinqToCollections;
using LineSegment = SnipSnap.Mathematics.LineSegment;
using System.Linq;
using TwistedOak.Element.Env;

namespace Animations {
    public static class Monad {
        private delegate Tuple<Animation, Ani<Size>> Animator<T>(Ani<T> value, Ani<bool> visible, Ani<Point> topLeft);
        
        private static Animator<string> MakeTextAnimator() {
            var t = new TextBlock();
            return (value, visible, topLeft) => Tuple.Create(
                new Animation {
                    new TextDesc(value.Combine(visible, (v1, v2) => v2 ? (v1 ?? "") : ""), topLeft, new Point(0,0))
                },
                value.Select(e => {
                    t.Text = e;
                    t.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    return t.DesiredSize;
                }));
        }
        private static Animator<int> MakeIntAnimator() {
            var r = MakeTextAnimator();
            return (value, visible, topLeft) => r(value.Select(e => e + ""), visible, topLeft);
        }
        private static Animation ShowCheckBox(Ani<Rect> box, Ani<bool> check, Ani<bool> vis) {
            var lc = box.Select(e => e.BottomLeft - new Vector(-2, e.Height/3+e.Height/6));
            var bc = box.Select(e => e.BottomLeft + new Vector(e.Width / 3, -e.Height / 6));
            var bc2 = box.Select(e => e.BottomLeft + new Vector(e.Width / 3+2, -e.Height / 6+2));
            var tr = box.Select(e => e.TopRight + new Vector(-2, e.Height/3-e.Height/6));
            var v = check.Select(e => (Brush)(e ? Brushes.Green : Brushes.Transparent));
            var v2 = vis.Select(e => (Brush)(e ? Brushes.Black : Brushes.Transparent));
            return new Animation {
                new LineSegmentDesc(lc.Combine(bc2, GeometryUtilities.To), v, 5),
                new LineSegmentDesc(tr.Combine(bc, GeometryUtilities.To), v, 5),
                new RectDesc(box, v2, strokeThickness: 1)
            };
        }
        private static Animator<May<T>> MakeMayAnimator<T>(Animator<T> subAnimator) {
            var s = 5;
            var b = 20;
            return (value, visible, topLeft) => {
                var hasVal = value.Select(e => e.HasValue).Combine(visible, (v1, v2) => v1 && v2);
                var r = subAnimator(value.Select(e => e.Else(default(T))), hasVal, topLeft.Select(e => e + new Vector(s + b + 40, s + b + s)));
                var size = r.Item2.Combine(value, (e, v) => v.HasValue ? new Size(Math.Max(20, e.Width) + s * 2 + b + 40, Math.Max(b, e.Height) + s * 3 + b) : new Size(s * 2 + b + 60, b + s * 3 + b));
                return Tuple.Create(
                    new Animation {
                        ShowCheckBox(topLeft.Select(e => new Rect(e.X + s, e.Y + s, b, b)), value.Select(e => !e.HasValue).Combine(visible, (v1, v2) => v1 && v2), visible),
                        ShowCheckBox(topLeft.Select(e => new Rect(e.X + s, e.Y + s + b + s, b, b)), hasVal, visible),
                        new TextDesc(visible.Select(e => e ? "No Value" : ""), topLeft.Select(e => e + new Vector(s+b+s, s)), new Point(0, 0)),
                        new TextDesc(visible.Combine(value, (e,v) => e ? (v.HasValue ? "Value: " : "Value: -") : ""), topLeft.Select(e => e + new Vector(s+b+s, s+b+s)), new Point(0, 0)),
                        r.Item1,
                        new RectDesc(size.Combine(topLeft, (sx,t) => new Rect(t, sx)), visible.Select(e => e ? (Brush)Brushes.Black : Brushes.Transparent), strokeThickness: 1)
                    },
                    size);
            };
        }
        public static Animation ShowMayMay() {
            var r1 = MakeIntAnimator();
            var r2 = MakeMayAnimator(r1);
            var r3 = MakeMayAnimator(r2);
            var ani = new Animation();
            var t = ani.Periodic(5.Seconds());
            t.Add(
                r3(t.Proper.Select(e => e < 0.333 ? 5.Maybe().Maybe() : e < 0.666 ? May<int>.NoValue.Maybe() : May<May<int>>.NoValue), true, new Point(50, 50)).Item1
            );
            return ani;
        }
    }
}