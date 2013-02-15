﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;
using SnipSnap.Mathematics;
using Strilanc.Angle;
using Strilanc.Value;
using TwistedOak.Collections;
using TwistedOak.Element.Env;
using TwistedOak.Util;

namespace Animatron {
    public static class Util {
        public static string StringJoin<T>(this IEnumerable<T> items, string separator) {
            if (items == null) throw new ArgumentNullException("items");
            return string.Join(separator, items);
        }
        public static void AddAll<T>(this PerishableCollection<T> collection, IEnumerable<T> items, Lifetime life) {
            foreach (var e in items) collection.Add(e, life);
        }
        public static TimeSpan LerpTo(this TimeSpan dt1, TimeSpan dt2, double prop) {
            return dt1.Times(1 - prop) + dt2.Times(prop);
        }
        public static IObservable<T> Cache<T>(this IObservable<T> v, Lifetime life) {
            var r = new ObservableValue<May<T>>();
            v.Subscribe(e => r.Update(e, false), life);
            return r.Where(e => e.HasValue).Select(e => e.ForceGetValue());
        }
        public static Vector Rotate(this Vector vector, Dir angle) {
            var d = Dir.FromVector(vector.X, vector.Y) + (angle - Dir.AlongPositiveX);
            return new Vector(d.UnitX, d.UnitY) * vector.Length;
        }
        public static IObservable<T> ToSingletonObservable<T>(this T value) {
            return new AnonymousObservable<T>(observer => {
                observer.OnNext(value);
                observer.OnCompleted();
                return new AnonymousDisposable();
            });
        }
        public static double SmoothLerpTo(this double from, double to, double p) {
            p = p*2 - 1;
            p *= 1.5;
            var s = p / Math.Sqrt(Math.Sqrt(Math.Sqrt(1 + Math.Pow(p, 8))));
            return from.LerpTo(to, (s + 1) / 2);
        }
        public static Point SmoothLerpTo(this Point from, Point to, double p) {
            return new Point(from.X.SmoothLerpTo(to.X, p), from.Y.SmoothLerpTo(to.Y, p));
        }
        public static double LerpCycle(this double t, params double[] stops) {
            var i = (int)Math.Floor(t).ProperMod(stops.Length);
            var i2 = (i + 1) % stops.Length;
            return stops[i].LerpTo(stops[i2], t.ProperMod(1));
        }
        public static double SmoothCycle(this double t, params double[] stops) {
            var i = (int)Math.Floor(t).ProperMod(stops.Length);
            var i2 = (i + 1) % stops.Length;
            return stops[i].SmoothLerpTo(stops[i2], t.ProperMod(1));
        }
        public static Point SmoothCycle(this double t, params Point[] stops) {
            var i = (int)Math.Floor(t).ProperMod(stops.Length);
            var i2 = (i + 1) % stops.Length;
            return stops[i].SmoothLerpTo(stops[i2], t.ProperMod(1));
        }
        public static double SmoothTransition(this double t, params double[] stops) {
            return (t * (stops.Length - 1)).SmoothCycle(stops);
        }
        public static Point SmoothTransition(this double t, params Point[] stops) {
            return (t * (stops.Length - 1)).SmoothCycle(stops);
        }
        public static double LerpTransition(this double t, params double[] stops) {
            return (t * (stops.Length - 1)).LerpCycle(stops);
        }
        public static double Proportion(this byte b) {
            return (b + 0.499)/256.0;
        }
        public static byte ToProportionalByte(this double p) {
            return (byte)Math.Round(p*256).Clamp(0, 255);
        }
        public static byte LerpTo(this byte from, byte to, double p) {
            return from.Proportion().LerpTo(to.Proportion(), p).ToProportionalByte();
        }
        public static string ToPrettyString(this Complex c) {
            var r = c.Real;
            var i = c.Imaginary;
            if (i == 0) return String.Format("{0:0.###}", r);
            if (r == 0)
                return i == 1 ? "i"
                     : i == -1 ? "-i"
                     : String.Format("{0:0.###}i", i);
            return String.Format(
                "{0:0.###}{1}{2}",
                r == 0 ? (object)"" : r,
                i < 0 ? "-" : "+",
                i == 1 || i == -1 ? "i" : String.Format("{0:0.###}i", Math.Abs(i)));
        }
        public static SolidColorBrush LerpToTransparent(this SolidColorBrush start, double p) {
            if (p <= 0) return start;
            if (p >= 1) return Brushes.Transparent;
            return new SolidColorBrush(start.Color.LerpToTransparent(p));
        }
        public static SolidColorBrush LerpTo(this SolidColorBrush start, SolidColorBrush finish, double p) {
            if (p <= 0) return start;
            if (p >= 1) return finish;
            return new SolidColorBrush(start.Color.LerpTo(finish.Color, p));
        }
        public static Color LerpToTransparent(this Color start, double p) {
            return Color.FromArgb(
                start.A.LerpTo(0, p),
                start.R,
                start.G,
                start.B);
        }
        public static Color LerpTo(this Color start, Color finish, double p) {
            return Color.FromArgb(
                start.A.LerpTo(finish.A, p),
                start.R.LerpTo(finish.R, p),
                start.G.LerpTo(finish.G, p),
                start.B.LerpTo(finish.B, p));
        }
    }
}