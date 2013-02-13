﻿using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using SnipSnap.Mathematics;
using Strilanc.Angle;
using Strilanc.Value;
using TwistedOak.Collections;
using TwistedOak.Element.Env;
using TwistedOak.Util;

namespace Animatron {
    public static class Util {
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
        public static double SmoothCycle(this double t, params double[] stops) {
            var i = (int)Math.Floor(t).ProperMod(stops.Length);
            var i2 = (i + 1)%stops.Length;
            return stops[i].SmoothLerpTo(stops[i2], t.ProperMod(1));
        }
    }
}