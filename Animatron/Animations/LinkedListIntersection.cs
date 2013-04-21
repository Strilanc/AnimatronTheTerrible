using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Animatron;
using Strilanc.Value;
using Strilanc.LinqToCollections;
using System.Linq;
using TwistedOak.Element.Env;
using LineSegment = SnipSnap.Mathematics.LineSegment;

namespace Animations {
    public static class LinkedListIntersection {
        private sealed class Link<T> {
            public Link<T> Next;
            public T Value;
        }

        private static Link<T> FindEarliestIntersection<T>(this Link<T> h0, Link<T> h1) {
            // find *any* intersection, and the distances to it
            var node = new[] {h0, h1};
            var dist = new[] {0, 0};
            var isCycle = new[] {false, false};
            var stepSize = 1;
            while (node[0] != node[1]) {
                // stop when both nodes are cycling or at null
                var stuck = node.Zip(isCycle, (n, isCyc) => n == null || isCyc);
                if (stuck.All(e => e)) break;

                // advance each node progressively farther, watching for the other node
                foreach (var i in Enumerable.Range(0, 2)) {
                    var start = node[i];
                    foreach (var repeat in Enumerable.Range(0, stepSize)) {
                        if (node[i] == null) break;
                        if (node[0] == node[1]) break;
                        node[i] = node[i].Next;
                        dist[i] += 1;
                        isCycle[i] |= node[i] == start;
                    }
                    stepSize *= 2;
                }
            }

            // if the lists are disjoint and either has a cycle, there's no intersection
            // (not even at the null node)
            if (node[0] != node[1])
                throw new InvalidOperationException("Disjoint with cycles.");

            // align heads to be an equal distance from the intersection
            var r = dist[1] - dist[0];
            while (r < 0) {
                h0 = h0.Next;
                r += 1;
            }
            while (r > 0) {
                h1 = h1.Next;
                r -= 1;
            }

            // advance heads until they intersect (at the first intersection)
            while (h0 != h1) {
                h0 = h0.Next;
                h1 = h1.Next;
            }

            return h0;
        }
        private static Link<T> ToCyclicLinkedList<T>(this IEnumerable<T> items) {
            if (items == null) throw new ArgumentNullException("items");
            Link<T> head = null;
            Link<T> tail = null;
            foreach (var e in items.Reverse()) {
                tail = new Link<T> {Next = tail, Value = e};
                head = head ?? tail;
            }
            if (head == null) throw new ArgumentOutOfRangeException("items", "!items.Any()");
            head.Next = tail;
            return tail;
        }
        private static Link<T> ToLinkedList<T>(this IEnumerable<T> items, Link<T> tail = null) {
            if (items == null) throw new ArgumentNullException("items");
            return items.Reverse().Aggregate(tail, (current, e) => new Link<T> {Next = current, Value = e});
        }

        private static Animation AnimateX<T>(Link<T> h0, Link<T> h1, TimeSpan dt, double y1, double y2, double w, double sp, double x, double border) {
            var t = TimeSpan.Zero;

            var touch = new[] {new Dictionary<Link<T>, TimeSpan>(), new Dictionary<Link<T>, TimeSpan>()};
            var discard = new Dictionary<Link<T>, TimeSpan>();
            var states = new List<dynamic>();
            
            // find *any* intersection, and the distances to it
            var node = new[] { h0, h1 };
            var dist = new[] { 0, 0 };
            var isCycle = new[] { false, false };
            var stepSize = 1;
            while (node[0] != node[1]) {
                // stop when both nodes are cycling or at null
                var stuck = node.Zip(isCycle, (n, isCyc) => n == null || isCyc);
                if (stuck.All(e => e)) break;

                // advance each node progressively farther, watching for the other node
                foreach (var i in Enumerable.Range(0, 2)) {
                    var start = node[i];
                    foreach (var repeat in Enumerable.Range(0, stepSize)) {
                        if (node[i] == null) break;
                        states.Add(new {
                            p0 = node[i],
                            p1 = node[1 - i],
                            d0 = dist[0],
                            d1 = dist[1],
                            s = string.Format("Advancing {0}: {1}/{2}", i == 0 ? "Top" : "Bottom", repeat, stepSize)
                        });
                        t += dt;
                        if (node[0] == node[1]) break;
                        if (node[i] != null && !touch[i].ContainsKey(node[i])) {
                            touch[i][node[i]] = t;
                        }
                        node[i] = node[i].Next;
                        dist[i] += 1;
                        isCycle[i] |= node[i] == start;
                    }
                    if (node[0] == node[1]) break;
                    states.Add(new {
                        p0 = node[i],
                        p1 = node[1 - i],
                        d0 = dist[0],
                        d1 = dist[1],
                        s = string.Format("Advancing {0}: {1}/{2}", i == 0 ? "Top" : "Bottom", stepSize, stepSize)
                    });
                    t += dt;
                    stepSize = (int)Math.Ceiling(stepSize * 1.5);
                }
            }

            states.Add(new {
                p0 = node[0],
                p1 = node[1],
                d0 = dist[0],
                d1 = dist[1],
                s = "Found a reference node..."
            });
            t += dt;
            states.Add(new {
                p0 = node[0],
                p1 = node[1],
                d0 = dist[0],
                d1 = dist[1],
                s = "Found a reference node..."
            });
            t += dt;
            states.Add(new {
                p0 = node[0],
                p1 = node[1],
                d0 = dist[0],
                d1 = dist[1],
                s = "Found a reference node..."
            });
            t += dt;

            // if the lists are disjoint and either has a cycle, there's no intersection
            // (not even at the null node)
            if (node[0] != node[1])
                throw new InvalidOperationException("Disjoint with cycles.");

            var loop0 = (Link<T>)null;
            var loop1 = (Link<T>)null;
            var a = new Animation();
            var f1 = new List<Link<T>>();
            var nn1 = h0;
            while (true) {
                if (nn1 == null) break;
                f1.Add(nn1);
                if (f1.Contains(nn1.Next)) {
                    loop0 = nn1;
                    loop1 = nn1.Next;
                    break;
                }
                nn1 = nn1.Next;
            }
            var f2 = new HashSet<Link<T>>();
            var nn2 = h1;
            while (true) {
                if (nn2 == null) break;
                f2.Add(nn2);
                if (f2.Contains(nn2.Next))
                    break;
                nn2 = nn2.Next;
            }

            var nnn0 = f1.TakeWhile(e => !f2.Contains(e)).Count();
            var nnn1 = f2.TakeWhile(e => !f1.Contains(e)).Count();
            var dmax = Math.Max(nnn0, nnn1);
            var pos = new Dictionary<Link<T>, Point>();
            var iii = 0;
            foreach (var e in f1.Except(f2)) {
                pos[e] = new Point(x + (iii+dmax-nnn0) * w * sp, y1);
                iii += 1;
            }
            iii = 0;
            foreach (var e in f2.Except(f1)) {
                pos[e] = new Point(x + (iii + dmax - nnn1) * w * sp, y2);
                iii += 1;
            }
            iii = 0;
            foreach (var e in f2.Intersect(f1)) {
                pos[e] = new Point(x + (iii + dmax) * w * sp, (y1+y2)/2);
                iii += 1;
            }

            if (loop0 != null) {
                a.Add(new LineSegmentDesc(new LineSegment(pos[loop0], pos[loop0] + new Vector(0, -w))));
                a.Add(new LineSegmentDesc(new LineSegment(pos[loop0] + new Vector(0, -w), pos[loop1] + new Vector(0, -w))));
                a.Add(new LineSegmentDesc(new LineSegment(pos[loop1], pos[loop1] + new Vector(0, -w))));
            }

            var zz = dist.Max();
            var ii = 0;

            zz = 0;
            // align heads to be an equal distance from the intersection
            var r = dist[1] - dist[0];
            while (r < 0) {
                discard[h0] = t;
                states.Add(new { p0 = h0, p1 = h1, d0 = dist[0] - zz, d1 = dist[1], s = string.Format("Discarding {0}-{1}={2} from Top", dist[0]-zz, dist[1], -r) });
                zz += 1;
                t += dt;
                h0 = h0.Next;
                r += 1;
            }
            while (r > 0) {
                discard[h1] = t;
                states.Add(new { p0 = h0, p1 = h1, d0 = dist[0], d1 = dist[1] - zz, s = string.Format("Discarding {0}-{1}={2} from Bottom", dist[1] - zz, dist[0], r) });
                zz += 1;
                t += dt;
                h1 = h1.Next;
                r -= 1;
            }

            states.Add(new { p0 = h0, p1 = h1, d0 = dist[0], d1 = dist[1] - zz, s = string.Format("Discarding {0}-{1}={2} from Bottom", dist.Min(), dist.Min(), r) });
            t += dt;

            var xx = zz;
            states.Add(new { p0 = h0, p1 = h1, d0 = dist.Min() - zz + xx, d1 = dist.Min() - zz + xx, s = "Done Discarding. Equal Distance to Intersection." });
            t += dt;
            states.Add(new { p0 = h0, p1 = h1, d0 = dist.Min() - zz + xx, d1 = dist.Min() - zz + xx, s = "Done Discarding. Equal Distance to Intersection." });
            t += dt;
            states.Add(new { p0 = h0, p1 = h1, d0 = dist.Min() - zz + xx, d1 = dist.Min() - zz + xx, s = "Done Discarding. Equal Distance to Intersection." });
            t += dt;
            states.Add(new { p0 = h0, p1 = h1, d0 = dist.Min() - zz + xx, d1 = dist.Min() - zz + xx, s = "Done Discarding. Equal Distance to Intersection." });
            t += dt;

            // advance heads until they intersect (at the first intersection)
            while (h0 != h1) {
                zz += 1;
                discard[h0] = t;
                discard[h1] = t;
                states.Add(new { p0 = h0, p1 = h1, d0 = dist.Min() - zz + xx, d1 = dist.Min() - zz + xx, s = "Advancing Together" });
                t += dt;
                h0 = h0.Next;
                h1 = h1.Next;
            }

            states.Add(new { p0 = h0, p1 = h1, d0 = dist.Min() - zz + xx, d1 = dist.Min() - zz + xx, s = "Found Intersection!" });
            t += dt;
            states.Add(new { p0 = h0, p1 = h1, d0 = dist.Min() - zz + xx, d1 = dist.Min() - zz + xx, s = "Found Intersection!" });
            t += dt;
            states.Add(new { p0 = h0, p1 = h1, d0 = dist.Min() - zz + xx, d1 = dist.Min() - zz + xx, s = "Found Intersection!" });
            t += dt;
            states.Add(new { p0 = h0, p1 = h1, d0 = dist.Min() - zz + xx, d1 = dist.Min() - zz + xx, s = "Found Intersection!" });
            t += dt;
            states.Add(new { p0 = h0, p1 = h1, d0 = dist.Min() - zz + xx, d1 = dist.Min() - zz + xx, s = "Found Intersection!" });
            t += dt;

            ii = 0;
            var start0 = h0;
            while (h0 != null) {
                pos[h0] = new Point(x + (zz + ii) * w * sp, (y1 + y2)/2);
                h0 = h0.Next;
                ii++;
                if (h0 == start0) break;
            }

            var bxx = a.Periodic(t);
            foreach (var n in pos.Keys) {
                var b = Ani.Anon(tx => {
                    if (discard.ContainsKey(n) && tx > discard[n]) return Brushes.White;
                    var bb = Brushes.White;
                    if (touch[0].ContainsKey(n) && tx >= touch[0][n]) bb = bb.LerpTo(Brushes.Red, 0.5);
                    if (touch[1].ContainsKey(n) && tx >= touch[1][n]) bb = bb.LerpTo(Brushes.Blue, 0.5);
                    return (Brush)bb;
                });
                bxx.Add(new RectDesc(new Rect(pos[n] - new Vector(w / 2, w / 2), new Size(w, w)), Brushes.Black, b, border));
                //bxx.Add(new TextDesc(n.Value+"", pos[n], new Point(0.5, 0.5)));
            }
            var ts = Ani.Anon(tx => states[(int)Math.Floor(tx.DividedBy(dt))]);
            bxx.Add(new TextDesc(
                ts.Select(e => "Reds: " + (string)e.d0.ToString() + ", Blues: " + (string)e.d1.ToString()),
                new Point(0, 25),
                new Point(0, 0),
                fontSize: 16));
            bxx.Add(new TextDesc(
                ts.Select(e => (string)e.s),
                new Point(0, 0),
                new Point(0, 0),
                fontSize: 20));
            bxx.Add(new RectDesc(ts.Select(e => (Link<T>)e.p0 == null ? default(Rect) : new Rect(pos[(Link<T>)e.p0] - new Vector(w / 2, w / 2), new Size(w, w))), Brushes.Black, Brushes.Transparent, 4));
            bxx.Add(new RectDesc(ts.Select(e => (Link<T>)e.p1 == null ? default(Rect) : new Rect(pos[(Link<T>)e.p1] - new Vector(w / 2, w / 2), new Size(w, w))), Brushes.Black, Brushes.Transparent, 4));
            return a;
        }
        public static Animation ShowLinearIntersection() {
            var com = 21.Range().ToLinkedList();
            var x1 = 5.Range().ToLinkedList(com);
            var x2 = 11.Range().ToLinkedList(com);

            return AnimateX(x1, x2, TimeSpan.FromMilliseconds(50), 65, 90, 20, 1.0, 10, 1);
        }
    }
}
