using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Media;
using Strilanc.LinqToCollections;

namespace SnipSnap.Mathematics {
    ///<summary>Utility methods for working with numbers and related concepts.</summary>
    public static class MathUtilities {
        ///<summary>The non-negative absolute magnitude of a number.</summary>
        public static double Abs(this double value) {
            return Math.Abs(value);
        }
        ///<summary>The larger of two values.</summary>
        public static T Max<T>(this T value1, T value2) where T : IComparable<T> {
            return value1.CompareTo(value2) >= 0 ? value1 : value2;
        }
        ///<summary>The lesser of two values.</summary>
        public static T Min<T>(this T value1, T value2) where T : IComparable<T> {
            return value1.CompareTo(value2) <= 0 ? value1 : value2;
        }
        ///<summary>Clamps a value to be not-less-than a minimum and not-larger-than a maximum.</summary>
        public static T Clamp<T>(this T value, T min, T max) where T : IComparable<T> {
            if (max.CompareTo(min) < 0) throw new ArgumentOutOfRangeException("max", "max < min");
            return value.Max(min).Min(max);
        }
        public static double Ln(this double d) {
            if (d < 0.01) return -100;
            return Math.Log(d);
        }
        public static double Exp(this double d) {
            return Math.Exp(d);
        }
        public static Point LerpTo(this Point from, Point to, double p) {
            return new Point(from.X.LerpTo(to.X, p), from.Y.LerpTo(to.Y, p));
        }
        ///<summary>Whether a value is less than (-1), contained in (0), or greater than (1) a contiguous range defined by a minimum and a maximum.</summary>
        public static int RangeSign<T>(this T value, T min, T max) where T : IComparable<T> {
            if (max.CompareTo(min) < 0) throw new ArgumentOutOfRangeException("max", "max < min");
            if (value.CompareTo(min) < 0) return -1;
            if (value.CompareTo(max) > 0) return +1;
            return 0;
        }
        public static Complex Sum(this IEnumerable<Complex> values) {
            return values.Aggregate(Complex.Zero, (a, e) => a + e);
        }

        ///<summary>The smallest possible non-negative remainder from whole-dividing a value by a divisor.</summary>
        public static double ProperMod(this double value, double divisor) {
            var r = value % divisor;
            if (r < 0) r += divisor;
            return r;
        }
        ///<summary>The smallest possible absolute (positive or negative) delta to go from value2 to value1 (modulo the given divisor).</summary>
        public static double SignedModularDifference(this double value1, double value2, double divisor) {
            var dif = (value1 - value2).ProperMod(divisor);
            if (dif >= divisor / 2) dif -= divisor;
            return dif;
        }
        ///<summary>Flips the resulting velocity to face towards the range when the position is out of range.</summary>
        public static double RangeBounceVelocity(this double velocity, double position, double minPosition, double maxPosition) {
            var r = position.RangeSign(minPosition, maxPosition);
            if (r == 0) return velocity;
            return velocity.Abs() * -r;
        }
        ///<summary>Maps the continuous range [0, 1) linearly onto the discrete range [0, 256), clamping input values outside [0, 1) to be in range.</summary>
        public static byte ProportionToByte(this double proportion) {
            return (byte)Math.Floor(proportion * 256).Clamp(0, 255);
        }

        /// <summary>
        /// Returns the point where a linear cut path intersects a line segment whose ends points are following linear paths.
        /// Returns null if there is no such point.
        /// Also returns the normalized 'direction' of the cut.
        /// </summary>
        public static Tuple<Point, Vector> TryGetCut(this LineSegment cutPath, LineSegment endPath1, LineSegment endPath2) {
            var e = new[] {endPath1, endPath2}
                .Select(line => new LineSegment(line.Start, line.Delta - cutPath.Delta))
                .ToArray();
            var c = GeometryUtilities.LineDefinedByMovingEndPointsCrossesOrigin(e[0], e[1], origin: cutPath.Start);
            if (!c.HasValue) return null;
            var p = cutPath.LerpAcross(c.Value.T);
            var v = cutPath.Delta - endPath1.Delta.LerpTo(endPath2.Delta, c.Value.S);
            return Tuple.Create(p, v.Normal());
        }

        ///<summary>A color with a hue based on the given value, cycling around the color wheel with the given period.</summary>
        public static Color HueToApproximateColor(this double hue, double period) {
            var rgb = 3.Range().Select(i => 
                (1 - period/2 + hue.SignedModularDifference(i*period/3, period).Abs()).ProportionToByte()
            ).ToArray();
            return Color.FromRgb(rgb[0], rgb[1], rgb[2]);
        }

        public static double LerpTo(this double valueAt0, double valueAt1, double proportion) {
            return valueAt1 * proportion + valueAt0 * (1 - proportion);
        }
        public static Vector LerpTo(this Vector valueAt0, Vector valueAt1, double proportion) {
            return valueAt1 * proportion + valueAt0 * (1 - proportion);
        }
        public static double LerpTo(this int valueAt0, double valueAt1, double proportion) {
            return ((double)valueAt0).LerpTo(valueAt1, proportion);
        }
        public static Byte LerpTo(this byte valueAt0, byte valueAt1, double proportion) {
            return (byte)Math.Round(((double)valueAt0).LerpTo(valueAt1, proportion).Clamp(0, 255));
        }
        public static Color LerpTo(this Color valueAt0, Color valueAt1, double proportion) {
            return Color.FromArgb(
                valueAt0.A.LerpTo(valueAt1.A, proportion),
                valueAt0.R.LerpTo(valueAt1.R, proportion),
                valueAt0.G.LerpTo(valueAt1.G, proportion),
                valueAt0.B.LerpTo(valueAt1.B, proportion));
        }
        public static Color LerpToTransparent(this Color color, double proportion) {
            return color.LerpTo(Color.FromArgb(0, color.R, color.G, color.B) , proportion);
        }

        public static Point LerpAcross(this LineSegment line, double proportion) {
            return line.Start + line.Delta*proportion;
        }
        public static double LerpProjectOnto(this Point point, LineSegment line) {
            var b = point - line.Start;
            var d = line.Delta;
            return (b*d)/d.LengthSquared;
        }
    }
}
