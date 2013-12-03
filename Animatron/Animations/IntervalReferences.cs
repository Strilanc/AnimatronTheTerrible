using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Animatron;
using IntervalReferences;
using TwistedOak.Element.Env;
using System.Linq;
using LineSegment = SnipSnap.Mathematics.LineSegment;
using SnipSnap.Mathematics;

namespace Animations {
    public static class IntervalReferences {
        private struct Operation {
            public Interval? Interval;
            public int Key;
        }
        private static Animation RenderTree(Interval renderInterval, NestingDepthTreeNode root, HashSet<int> dealloced) {
            var v = renderInterval;
            var ani = new Animation();
            var r = 10;
            var query = new Dictionary<int, int>();
            var x = 100;
            var y = 350;

            for (var i = 0; i < v.Length; i++) {
                var h = NestingDepthTreeNode.QueryNestingDepthAt(root, i+v.Offset);
                query[i+v.Offset] = h;
                if (h== -1) throw new Exception();
                ani.Add(new RectDesc(new Rect(x + i * r, y - h * r - 1, r, h * r + 1), fill: Brushes.Black, stroke: Brushes.Black, strokeThickness: 0));
                if (dealloced.Contains(i+v.Offset)) {
                    ani.Add(new RectDesc(new Rect(x + i * r, y - 0.5 * r - 1, r, 1 * r + 1), fill: Brushes.Red, stroke: Brushes.Orange, strokeThickness: 1));
                }
            }

            var y2 = 125;
            Action<NestingDepthTreeNode, int> paintNode = null;
            paintNode = (n, l) => {
                if (n == null) return;

                var p = new Point(x + (n._offset-v.Offset)*r, y2 + l*r*5);
                if (n._parent != null) {
                    var q = new Point(x + (n._parent._offset - v.Offset) * r, y2 + (l - 1) * r * 5);
                    var b1 = false;
                    var b2 = false;
                    for (var i = Math.Min(n._offset, n._parent._offset); i < Math.Max(n._offset, n._parent._offset); i++) {
                        b1 |= query[i] != 0;
                        b2 |= query[i] == 0;
                    }
                    ani.Add(new LineSegmentDesc(new LineSegment(p, q), b1 && b2 ? Brushes.Red : b2 ? Brushes.Red : Brushes.Black, 1));
                }
                if (n._less != null) paintNode(n._less, l + 1);
                if (n._more != null) paintNode(n._more, l + 1);
                ani.Add(new RectDesc(new Rect(new Point(p.X - 18, p.Y - 22), new Size(36, 44)), n._fakeRefCount > 1 ? Brushes.Black : Brushes.Red, n._fakeRefCount > 0 ? Brushes.Gray : Brushes.Red, 1));
                var s = "d=" + n._adjust +Environment.NewLine + "t=" + n._subTreeTotalAdjust + Environment.NewLine + "m=" + n._subTreeRelativeMinimum;
                ani.Add(new TextDesc(s, new Point(p.X - 16, p.Y - 20), new Point(0, 0)));
                //var s = (n._adjust > 0 ? "+" : "") + n._adjust;
                //ani.Add(new PointDesc(p, n._fakeRefCount > 1 ? Brushes.Black : Brushes.Red, n._fakeRefCount > 0 ? Brushes.Gray : Brushes.Red, 15, 1));
                //ani.Add(new TextDesc(s, new Point(p.X - 8, p.Y - 8), new Point(0, 0)));
            };
            paintNode(root, 0);

            return ani;
        }
        private static Animation CreateAnimationOfOperations(Interval renderInterval, Interval targetInterval, params Operation[] operations) {
            var keyNodes = new Dictionary<int, Tuple<NestingDepthTreeNode, Interval, NestingDepthTreeNode>>();
            var activeOperation = default(Operation);

            var dt = 1.0;
            var ani = new Animation();

            ani.Add(new TextDesc("Using a Tree to Track Referenced Intervals", new Point(250, 5), new Point(0, 0), fontSize:20));
            ani.Add(new TextDesc("Referenced" + Environment.NewLine + "Intervals", new Point(10, 80)));
            ani.Add(new TextDesc("Tree", new Point(10, 220)));
            ani.Add(new TextDesc("Nesting" + Environment.NewLine + "Depth", new Point(10, 350)));
            ani.Add(new LineSegmentDesc(new Point(0, 30).Sweep(new Vector(10000, 0)), Brushes.Black, 1));
            ani.Add(new LineSegmentDesc(new Point(0, 100).Sweep(new Vector(10000, 0)), Brushes.Black, 1));
            ani.Add(new LineSegmentDesc(new Point(0, 300).Sweep(new Vector(10000, 0)), Brushes.Black, 1));
            ani.Add(new LineSegmentDesc(new Point(75, 0).Sweep(new Vector(0, 10000)), Brushes.Black, 1));

            var i = 0.0;
            var dealloced = new HashSet<int>();
            for (var i2 = 0; i2 < renderInterval.Length; i2++) {
                if (i2 + renderInterval.Offset < targetInterval.Offset || i2 + renderInterval.Offset >= targetInterval.Offset + targetInterval.Length) {
                    dealloced.Add(i2 + renderInterval.Offset);
                }
            }
            var x = 100;
            var y = 90;
            Func<double, Animation> addFrame = focus => {
                var roots = keyNodes.Select(e => NestingDepthTreeNode.RootOf(e.Value.Item1)).Distinct().ToArray();

                var a = new Animation();

                var s = 10;
                if (!double.IsNaN(focus) && !double.IsInfinity(focus)) {
                    var dx = focus - renderInterval.Offset;
                    a.Add(new LineSegmentDesc(new LineSegment(new Point(x + dx*s, 0), new Point(x + dx*s, 10000)), activeOperation.Interval.HasValue ? Brushes.Green : Brushes.Red, 1, 1.0));
                }

                foreach (var r in roots) a.Add(RenderTree(renderInterval, r, dealloced));
                ani.LimitedNewTime(i.Seconds(), (i + dt).Seconds()).Add(a);
                i += dt;

                var bb = 0;
                foreach (var e in keyNodes) {
                    Brush brush;
                    if (e.Key == activeOperation.Key && !double.IsInfinity(focus)) {
                        brush = activeOperation.Interval.HasValue ? Brushes.Green : Brushes.Red;
                    } else {
                        brush = Brushes.Yellow;
                    }
                    var inv = e.Value.Item2;
                    var dx = inv.Offset - renderInterval.Offset;
                    a.Add(new RectDesc(new Rect(x + dx * s, y - (bb+1) * s*1.1, s*inv.Length, s), fill: brush, stroke: Brushes.Black, strokeThickness: 1));

                    bb += 1;
                }

                return a;
            };

            foreach (var e in operations) {
                activeOperation = e;
                var affectedRoot =
                    e.Interval.HasValue ?
                    keyNodes.Select(n => NestingDepthTreeNode.RootOf(n.Value.Item1)).FirstOrDefault(n => NestingDepthTreeNode.GetInterval(n).Overlaps(e.Interval.Value))
                    : NestingDepthTreeNode.RootOf(keyNodes[e.Key].Item1);
                    
                if (e.Interval.HasValue) {
                    keyNodes.Add(e.Key, Tuple.Create((NestingDepthTreeNode)null, e.Interval.Value, (NestingDepthTreeNode)null));

                    addFrame(double.NaN);
                    addFrame(e.Interval.Value.Offset);

                    var a1 = NestingDepthTreeNode.Include(affectedRoot, e.Interval.Value.Offset, +1, +1);
                    a1.AdjustedNode._fakeRefCount += 2;
                    keyNodes[e.Key] = Tuple.Create(a1.AdjustedNode, e.Interval.Value, (NestingDepthTreeNode)null);
                    addFrame(e.Interval.Value.Offset);
                    
                    addFrame(e.Interval.Value.Offset + e.Interval.Value.Length);
                    var a2 = NestingDepthTreeNode.Include(a1.NewRoot, e.Interval.Value.Offset + e.Interval.Value.Length, -1, +1);
                    a2.AdjustedNode._fakeRefCount += 2;
                    keyNodes[e.Key] = Tuple.Create(a1.AdjustedNode, e.Interval.Value, a2.AdjustedNode);

                    addFrame(e.Interval.Value.Offset + e.Interval.Value.Length);
                    addFrame(double.PositiveInfinity);
                }
                else {
                    var xs = keyNodes[e.Key];

                    var r = NestingDepthTreeNode.RootOf(xs.Item1);
                    r = NestingDepthTreeNode.Include(r, xs.Item2.Offset + xs.Item2.Length, +1, 0).NewRoot;
                    r = NestingDepthTreeNode.Include(r, xs.Item2.Offset, -1, 0).NewRoot;
                    var hh = new HashSet<int>();
                    foreach (var ex in NestingDepthTreeNode.FindHolesIn(NestingDepthTreeNode.GetInterval(r), r)) {
                        for (var ii = ex.Offset; ii < ex.Offset + ex.Length; ii++) {
                            hh.Add(ii);
                        }
                    }
                    r = NestingDepthTreeNode.Include(r, xs.Item2.Offset + xs.Item2.Length, -1, 0).NewRoot;
                    r = NestingDepthTreeNode.Include(r, xs.Item2.Offset, +1, 0).NewRoot;

                    xs.Item3._fakeRefCount -= 1;
                    xs.Item3._fakeRefCount -= 1;
                    xs.Item1._fakeRefCount -= 1;
                    addFrame(xs.Item2.Offset + xs.Item2.Length);
                    var r1 = NestingDepthTreeNode.Include(r, xs.Item2.Offset + xs.Item2.Length, +1, -1);
                    addFrame(xs.Item2.Offset + xs.Item2.Length);
                    
                    xs.Item1._fakeRefCount -= 1;
                    addFrame(xs.Item2.Offset);
                    var r2 = NestingDepthTreeNode.Include(r1.NewRoot, xs.Item2.Offset, -1, -1);
                    keyNodes.Remove(e.Key);
                    addFrame(double.NaN);

                    foreach (var xxx in hh) {
                        dealloced.Add(xxx);
                    }

                    
                    NestingDepthTreeNode.PartitionAroundHoles(r2.NewRoot);


                    addFrame(double.NaN);
                }
            }

            var xx = new Animation();
            xx.Periodic(i.Seconds()).Add(ani);
            return xx;
        }

        public static Animation CreateAnimation() {
            var rng = new Random(12);

            var bigArraySize = 62;
            var ops = new List<Operation>();
            ops.Add(new Operation {Key = -1, Interval = new Interval(1, bigArraySize)});
            var remainingNumbers = Enumerable.Range(1, bigArraySize).ToList();
            
            var activeIntervals = new Dictionary<int, Interval>();
            activeIntervals[-1] = new Interval(1, bigArraySize);

            for (var i = 0; i < 50; i++) {
                if (activeIntervals.Count == 0) break;
                var rr = activeIntervals.ToArray();
                if (i < 4) { //} || rng.Next(5) == 0) {
                    var parent = activeIntervals[-1]; //rng.Next(activeIntervals.Count)].Value;
                    var rep = 0;
                    while (true) {
                        var v1 = rng.Next(parent.Length);
                        var v2 = rng.Next(parent.Length);
                        if (v1 == v2) continue;
                        rep += 1;
                        if (rep < 10 && (!remainingNumbers.Contains(v1) || !remainingNumbers.Contains(v2))) continue;
                        var offset = Math.Min(v2, v1);
                        var len = Math.Max(v2, v1) - offset + 1;
                        var x = new Interval(parent.Offset + offset, len/2);
                        ops.Add(new Operation {Key = i, Interval = x});
                        activeIntervals[i] = x;
                        break;
                    }
                } else {
                    var x = rng.Next(activeIntervals.Count);
                    var k = i == 4 ? -1 : rr[x].Key;
                    ops.Add(new Operation() {Key=k});
                    activeIntervals.Remove(k);
                }
                
            }
            return CreateAnimationOfOperations(new Interval(1 - 5, bigArraySize + 10), new Interval(1, bigArraySize), ops.ToArray());
        }
    }
}