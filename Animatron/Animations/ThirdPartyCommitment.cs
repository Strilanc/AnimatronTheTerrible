using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Animatron;
using Strilanc.LinqToCollections;
using SnipSnap.Mathematics;
using TwistedOak.Element.Env;
using LineSegment = SnipSnap.Mathematics.LineSegment;
using Strilanc.Value;

namespace Animations {
    public static class ThirdPartyCommitment {
        private static double GetX(int index, int size, Rect rect) {
            var w = rect.Width / size;
            return rect.Left + w * index + w / 2;
        }
        private static double GetY(int index, int size, Rect rect) {
            var w = rect.Height / size;
            return rect.Bottom - w * index - w / 2;
        }
        private static Point GetP(int indexX, int indexY, int size, Rect rect) {
            return new Point(GetX(indexX, size, rect), GetY(indexY, size, rect));
        }
        private static Animation CreateGrid(int size, Rect rect) {
            var ani = new Animation();
            for (var i = 0; i < size; i++) {
                ani.Add(new LineSegmentDesc(new Point(GetX(i, size, rect), rect.Top).Sweep(new Vector(0, rect.Height)), Brushes.Gray, 1));
                ani.Add(new LineSegmentDesc(new Point(rect.Left, GetY(i, size, rect)).Sweep(new Vector(rect.Width, 0)), Brushes.Gray, 1));
            }
            return ani;
        }
        private static LineSegment? ClipLine(LineSegment line, Rect rect) {
            var left = new Point(rect.Left, rect.Top - 100000).To(new Point(rect.Left, rect.Bottom + 100000));
            var right = new Point(rect.Right, rect.Top - 100000).To(new Point(rect.Right, rect.Bottom + 100000));
            var top = new Point(rect.Left - 100000, rect.Top).To(new Point(rect.Right + 100000, rect.Top));
            var bottom = new Point(rect.Left - 100000, rect.Bottom).To(new Point(rect.Right + 100000, rect.Bottom));

            var p1 = line.TryIntersectionPointInside(left);
            var p2 = line.TryIntersectionPointInside(right);
            var p3 = line.TryIntersectionPointInside(top);
            var p4 = line.TryIntersectionPointInside(bottom);

            var es = new[] {line.Start, line.End};

            if (p1.HasValue) return ClipLine(p1.Value.To(es.MayMaxBy(e => e.X).ForceGetValue()), rect);
            if (p2.HasValue) return ClipLine(p2.Value.To(es.MayMaxBy(e => -e.X).ForceGetValue()), rect);
            if (p3.HasValue) return ClipLine(p3.Value.To(es.MayMaxBy(e => e.Y).ForceGetValue()), rect);
            if (p4.HasValue) return ClipLine(p4.Value.To(es.MayMaxBy(e => -e.Y).ForceGetValue()), rect);

            if (!rect.Contains(line.Mid)) return null;
            return line;
        }
        private static Animation CreateLine(int y0, int slope, int size, Rect rect) {
            if (slope > size / 2) slope -= size;
            var ani = new Animation();
            var s = new Vector(rect.Width, -slope*rect.Height);
            var c = new Point(GetX(0, size, rect), GetY(y0, size, rect));
            var L = (c - s).To(c + s);
            var w = slope == 0 ? 0 : rect.Width / Math.Abs(slope);
            for (var i = -3; i <= Math.Abs(slope)+2; i++) {
                var Li = ClipLine(L + new Vector(w, 0)*i, rect);
                if (Li.HasValue) {
                    ani.Add(new LineSegmentDesc(Li.Value, Brushes.Black, 2));
                }
                if (slope == 0) break;
            }
            return ani;
        }
        private static Animation CreateVaryYSolveSlopeAnimation(int size, Rect rect, int xq, int yq) {
            var ani = new Animation { CreateGrid(size, rect) };

            var dt = 50.Milliseconds();
            var p = ani.Periodic(dt.Times(size));
            for (var y0 = 0; y0 < size; y0++) {
                var slope = size.Range().Single(s => (y0 + s * xq) % size == yq);
                var q = p.LimitedNewTime(dt.Times(y0), dt.Times(y0 + 1));
                q.Add(CreateLine(y0, slope, size, rect));
                q.Add(new TextDesc(string.Format("Target = ({0}, {1}), Slope = {2}, Y-Intercept = {3}", xq, yq, slope, y0),
                    new Point(rect.Left + 67, rect.Bottom + 5),
                    new Point(0, 0), fontSize: 14));
                q.Add(new PointDesc(GetP(xq, yq, size, rect), fill: Brushes.Blue, radius: 8));
                q.Add(new PointDesc(GetP(0, y0, size, rect), fill: Brushes.Red, radius: 8));
            }

            ani.Add(new TextDesc("Varying Y-Intercept, Solving Slope", new Point(rect.Left + rect.Width / 2, rect.Top - 30), new Point(0.5, 0), fontSize: 18));

            return ani;
        }
        private static Animation CreateVaryQSolveSlopeAnimation(Rect rect) {
            var size = 13;
            var yq = 10;
            var y0 = 5;

            var ani = new Animation { CreateGrid(size, rect) };
            var dt = 50.Milliseconds();
            var p = ani.Periodic(dt.Times(size - 1));
            for (var xq = 1; xq < size; xq++) {
                var slope = size.Range().Single(s => (y0 + s * xq) % size == yq);
                var q = p.LimitedNewTime(dt.Times(xq - 1), dt.Times(xq));
                q.Add(CreateLine(y0, slope, size, rect));
                q.Add(new PointDesc(GetP(0, y0, size, rect), fill: Brushes.Red, radius: 8));
                q.Add(new TextDesc(string.Format("Target = ({0}, {1}), Slope = {2}, Y-Intercept = {3}", xq, yq, slope, y0),
                    new Point(rect.Left + 67, rect.Bottom + 5),
                    new Point(0, 0), fontSize: 14));
                q.Add(new PointDesc(GetP(xq, yq, size, rect), fill: Brushes.Blue, radius: 8));
            }

            ani.Add(new TextDesc("Varying Target, Solving Slope", new Point(rect.Left + rect.Width / 2, rect.Top - 30), new Point(0.5, 0), fontSize: 18));

            return ani;
        }
        private static Animation CreateVarySlopeSolveYAnimation(int size, Rect rect, int xq, int yq) {
            var ani = new Animation { CreateGrid(size, rect) };

            var dt = 50.Milliseconds();
            var p = ani.Periodic(dt.Times(size));
            for (var slope = 0; slope < size; slope++) {
                var y0 = size.Range().Single(y => (y + slope * xq) % size == yq);
                var q = p.LimitedNewTime(dt.Times(slope), dt.Times(slope + 1));
                q.Add(CreateLine(y0, slope, size, rect));
                q.Add(new PointDesc(GetP(0, y0, size, rect), fill: Brushes.Red, radius: 8));
                q.Add(new TextDesc(string.Format("Target = ({0}, {1}), Slope = {2}, Y-Intercept = {3}", xq, yq, slope, y0),
                    new Point(rect.Left + 67, rect.Bottom + 5), 
                    new Point(0, 0), fontSize: 14));
                q.Add(new PointDesc(GetP(xq, yq, size, rect), fill: Brushes.Blue, radius: 8));
            }

            ani.Add(new TextDesc("Varying Slope, Solving Y-Intercept", new Point(rect.Left + rect.Width / 2, rect.Top - 30), new Point(0.5, 0), fontSize: 18));

            return ani;
        }

        public static Animation CreateAnimation() {
            var r = new Rect(20, 30, 390, 390);
            return new Animation {
                //CreateVaryYSolveSlopeAnimation(13, r, 4, 5),
                //CreateVaryQSolveSlopeAnimation(r),
                CreateVarySlopeSolveYAnimation(13, r, 4, 5),
            };
        }
    }
}