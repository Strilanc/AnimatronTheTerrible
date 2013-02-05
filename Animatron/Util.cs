using System;
using System.Collections.Generic;
using System.Windows;
using Strilanc.Angle;
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
        public static Vector Rotate(this Vector vector, Dir angle) {
            var d = Dir.FromVector(vector.X, vector.Y) + (angle - Dir.AlongPositiveX);
            return new Vector(d.UnitX, d.UnitY) * vector.Length;
        }
    }
}