using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SnipSnap.Mathematics {
    ///<summary>Contains utility methods for working with geometric types (points, vectors, line segments and convex polygons).</summary>
    ///<remarks>Attempts to be simple and correct.</remarks>
    public static class GeometryUtilities {
        ///<summary>Returns the unit vector pointing in the same direction as the vectory, or else the zero vector.</summary>
        public static Vector Normal(this Vector vector) {
            vector.Normalize();
            if (double.IsNaN(vector.X)) return default(Vector);
            return vector;
        }
        ///<summary>Returns the cross product of two vectors in 2d.</summary>
        public static double Cross(this Vector vector1, Vector vector2) {
            return vector1.X * vector2.Y - vector1.Y * vector2.X;
        }
        ///<summary>Returns the length along a given direction that a vector travels.</summary>
        public static double ScalarProjectOnto(this Vector vector, Vector direction) {
            return vector * direction.Normal();
        }

        public static Vector Perp(this Vector v) {
            return new Vector(-v.Y, v.X);
        }
        ///<summary>Creates a line segment based on the given end points.</summary>
        public static LineSegment To(this Point endPoint1, Point endPoint2) {
            return new LineSegment(endPoint1, endPoint2);
        }
        ///<summary>Creates a line segment starting at a given point and extending by a given delta.</summary>
        public static LineSegment Sweep(this Point start, Vector delta) {
            return new LineSegment(start, delta);
        }

        public struct IntersectionParameters {
            public double T;
            public double S;
        }
        public static IntersectionParameters? LineDefinedByMovingEndPointsCrossesOrigin(LineSegment endPath1,
                                                                            LineSegment endPath2,
                                                                            Point origin = default(Point)) {
            var b = endPath1.Start - origin;
            var d = endPath2.Start - endPath1.Start;
            var db = endPath1.Delta;
            var dd = endPath2.Delta - endPath1.Delta;

            return (from t in QuadraticRoots(dd.Cross(db),
                                             d.Cross(db) + dd.Cross(b),
                                             d.Cross(b))
                    where t >= 0 && t <= 1
                    let p0 = endPath1.LerpAcross(t)
                    let p1 = endPath2.LerpAcross(t)
                    let s = origin.LerpProjectOnto(p0.To(p1))
                    where s >= 0 && s <= 1
                    select (IntersectionParameters?)new IntersectionParameters {T = t, S = s}
                   ).FirstOrDefault();
        }
        public static Vector ProjectOnto(this Vector v, Vector p) {
            return p * ((v * p) / (p * p));
        }
        public static Vector PerpOnto(this Vector v, Vector p) {
            return v - v.ProjectOnto(p);
        }
        public static double DistanceFrom(this Point p1, Point p2) {
            var d = p1 - p2;
            return Math.Sqrt(d * d);
        }
        public static double DistanceFrom(this Point p, LineSegment line) {
            var s = p.LerpProjectOnto(line);
            if (s < 0) return p.DistanceFrom(line.Start);
            if (s > 1) return p.DistanceFrom(line.End);
            return p.DistanceFrom(line.LerpAcross(s));
        }
        ///<summary>Returns the first non-negative time, if any, where a moving circle will intersect a fixed line segment.</summary>
        public static double? WhenMovingCircleWillIntersectLineSegment(Point center, double radius, Vector velocity, LineSegment line) {
            var epsilon = 0.00001;

            // use whatever's earliest and is actually touching
            return new[] {
                WhenMovingCircleWillIntersectExtendedLine(center, radius, velocity, line.Start, line.Delta),
                WhenMovingCircleWillIntersectPoint(center, radius, velocity, line.Start),
                WhenMovingCircleWillIntersectPoint(center, radius, velocity, line.End)
            }.OrderBy(e => e)
             .FirstOrDefault(t => t.HasValue && (center + velocity * t.GetValueOrDefault()).DistanceFrom(line) <= radius + epsilon);
        }
        ///<summary>Returns the first non-negative time, if any, where a moving circle will intersect a fixed extended line.</summary>
        public static double? WhenMovingCircleWillIntersectExtendedLine(Point center, double radius, Vector velocity, Point pointOnLine, Vector displacementAlongLine) {
            var a = (center - pointOnLine).PerpOnto(displacementAlongLine);
            if (a * a - radius * radius <= 0) return 0; // already touching at t=0?
            var b = velocity.PerpOnto(displacementAlongLine);
            var kissTime = QuadraticRoots(b * b, a * b * 2, a * a - radius * radius)
                .Where(e => e >= 0)
                .Cast<double?>()
                .FirstOrDefault();
            return kissTime;
        }
        ///<summary>Returns the first non-negative time, if any, where a moving circle will intersect a fixed point.</summary>
        public static double? WhenMovingCircleWillIntersectPoint(Point center, double radius, Vector velocity, Point point) {
            var a = center - point;
            if (a * a - radius * radius <= 0) return 0; // already touching at t=0?
            var b = velocity;
            var kissTime = QuadraticRoots(b * b, a * b * 2, a * a - radius * radius)
                .Where(e => e >= 0)
                .Cast<double?>()
                .FirstOrDefault();
            return kissTime;
        }
        public static IEnumerable<double> QuadraticRoots(double a, double b, double c) {
            // degenerate case (0x^2 + bc + c == 0)
            if (a == 0) {
                // double-degenerate case (0x^2 + 0x + c == 0)
                if (b == 0) {
                    // triple-degenerate case (0x^2 + 0x + 0 == 0)
                    if (c == 0) yield return 0; // every other real number is also a solution, but hopefully one example will be fine
                    yield break;
                }

                yield return -c / b;
                yield break;
            }

            // ax^2 + bx + c == 0
            // x = (-b +- sqrt(b^2 - 4ac)) / 2a

            var d = b*b - 4*a*c;
            if (d < 0) yield break; // no real roots
            
            var s0 = -b/(2*a);
            var sd = Math.Sqrt(d)/(2*a);
            yield return s0 - sd;
            if (sd == 0) yield break; // unique root

            yield return s0 + sd;
        }

        ///<summary>The point on a line segment that is closest to a target point.</summary>
        public static Point ClosestPointOn(this Point point, LineSegment line) {
            return line.Start + line.Delta.Normal() * (point - line.Start).ScalarProjectOnto(line.Delta).Clamp(0, line.Delta.Length);
        }
    }
}
