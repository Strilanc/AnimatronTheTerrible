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
