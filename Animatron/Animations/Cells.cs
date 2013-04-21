using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Animatron;
using SnipSnap.Mathematics;
using Strilanc.Angle;
using Strilanc.LinqToCollections;
using TwistedOak.Element.Env;
using TwistedOak.Util;
using LineSegment = SnipSnap.Mathematics.LineSegment;

namespace Animations {
    class Liqs {
        public Point P;
        public Vector V;
        public double Charge;
        public Liqs Partner;
        public Vector InnerForceTowards(Point p2) {
            var d = p2 - P;
            var e = d.Normal()*20;
            var de = d - e;
            return de*5;
        }
        public Vector ChargeForceTowards(Liqs other) {
            var d = other.P - P;
            var s = d.LengthSquared;
            if (s > 10000) return default(Vector);
            s = s.Max(5);
            var x = Charge*other.Charge;
            if (Charge == 1 && other.Charge == 1) x = -.1;
            if (Charge == -1 && other.Charge == -1) x = -.01;
            //if (x == -1) x = 1;
            if (x == 0 && Charge + other.Charge == 1) x = 1;
            if (s < 100 || x == 0) return -d.Normal() / s * 100;
            return d.Normal()/s*x*100;
        }
        public Vector ForceTowards(Liqs other) {
            if (other == Partner) return InnerForceTowards(other.P);
            return ChargeForceTowards(other);
        }
    }
    public static class Cells {
        public static Animation CreateCellAnimation() {
            var animation = new Animation();

            var liqs = new List<Liqs>();
            var rng = new Random();
            var n = 300;
            var w = 400.0;
            foreach (var i in n.Range()) {
                var p = new Point(rng.NextDouble()*w, rng.NextDouble()*w);
                var a = i*2*Math.PI/n;
                var v = new Vector(Math.Sin(a), Math.Cos(a));
                //p = new Point(w/2,w/2) + (w/8+rng.NextDouble()*5)* v;
                var l1 = new Liqs {
                    P = p,
                    V = new Vector(rng.NextDouble() - 0.5, rng.NextDouble() - 0.5) * 25,
                    Charge = -1 //(rng.Next(2) * 2 - 1)
                };
                var l2 = new Liqs {
                    P = p + v*20*(rng.Next(3)==0?-1:1) + 8*new Vector(rng.NextDouble() * -0.5, rng.NextDouble() * -0.5),
                    V = new Vector(rng.NextDouble()-0.5, rng.NextDouble()-0.5)*10,
                    Partner =  l1,
                    Charge = -l1.Charge
                };
                l1.Partner = l2;
                liqs.Add(l1);
                liqs.Add(l2);
                //animation.Add(new LineSegmentDesc(Ani.Anon(t => l1.P.To(l2.P)), Brushes.Black, 1.0, 3));
                animation.Add(new PointDesc(Ani.Anon(t => l1.P), Brushes.Transparent, l1.Charge == -1 ? Brushes.Red : Brushes.Blue, 3, 0.0));
                animation.Add(new PointDesc(Ani.Anon(t => l2.P), Brushes.Transparent, l2.Charge == -1 ? Brushes.Red : Brushes.Blue, 3, 0.0));
            }
            foreach (var i in (n*4).Range()) {
                var p = new Point((rng.NextDouble() * 2 - 1) *w / 2 + w / 2, (rng.NextDouble() * 2 - 1) * w / 2 + w / 2);
                var a = i * 2 * Math.PI / n;
                var v = new Vector(Math.Sin(a), Math.Cos(a));
                //p = new Point(w / 2, w / 2) + ((rng.Next(12) == 0 ? w / 32 : w * 0.25 * (rng.NextDouble() * 0.2 + 0.9)) + rng.NextDouble() * 5) * v;
                var l1 = new Liqs {
                    P = p,
                    Charge = 0
                };
                liqs.Add(l1);
                animation.Add(new PointDesc(Ani.Anon(t => l1.P), Brushes.Transparent, Brushes.DarkGreen, 2, 0.0));
            }

            Action<TimeSpan> sstep = dt => {
                var ds = dt.TotalSeconds;
                foreach (var p in liqs) {
                    var a = new Vector(0, 0);
                    foreach (var q in liqs) {
                        if (q == p) continue;
                        a += p.ForceTowards(q) * 1;
                    }
                    var f = 5;
                    if (p.P.X > w-10) a -= f*new Vector(1, 0);
                    if (p.P.X < 10) a -= f * new Vector(-1, 0);
                    if (p.P.Y > w - 10) a -= f * new Vector(0, 1);
                    if (p.P.Y < 10) a -= f * new Vector(0, -1);
                    var t = 1.0;
                    //foreach (var q in liqs) {
                    //    if (q == p || q == p.Partner) continue;
                    //    var r = GeometryUtilities.LineDefinedByMovingEndPointsCrossesOrigin(q.P.To(q.P - p.V * ds * t), q.Partner.P.To(q.Partner.P - p.V * ds * t), p.P);
                    //    if (r.HasValue) t *= r.Value.T;
                    //}
                    //foreach (var q in liqs) {
                    //    if (q == p || q == p.Partner) continue;
                    //    var r = GeometryUtilities.LineDefinedByMovingEndPointsCrossesOrigin(p.P.To(p.P + p.V*ds*t), p.Partner.P.To(p.Partner.P + p.V*ds*t), q.P);
                    //    if (r.HasValue) t *= r.Value.T;
                    //}
                    p.P += p.V * ds * t;
                    p.P += new Vector(rng.NextDouble() - 0.5, rng.NextDouble() - 0.5);
                    p.V += a * ds;
                    p.V *= Math.Pow(0.5, ds);
                }
            };
            animation.Add(new RectDesc(new Rect(10,10,w-20,w-20), Brushes.Black, Brushes.Transparent, 1, 2));

            animation.StepActions.Add(step => {
                sstep(step.TimeStep);
            }, Lifetime.Immortal);
            return animation;
        }
    }
}