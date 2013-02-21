using System;
using System.Collections.Generic;
using TwistedOak.Util;
using System.Reactive.Linq;
using System.Linq;
using Strilanc.LinqToCollections;

namespace Animatron {
    public interface IAni<out T> {
        T ValueAt(TimeSpan t);
    }
    public abstract class Ani<T> : IAni<T> {
        public abstract T ValueAt(TimeSpan t);

        public static implicit operator Ani<T>(T v) {
            return new ConstantAni<T>(v);
        }
        
        public static Ani<T> operator +(Ani<T> value1, Ani<T> value2) {
            return value1.Combine(value2, (e1, e2) => (T)((dynamic)e1 + e2));
        }
        public static Ani<T> operator -(Ani<T> value1, Ani<T> value2) {
            return value1.Combine(value2, (e1, e2) => (T)((dynamic)e1 - e2));
        }
        public static Ani<T> operator -(Ani<T> value) {
            return value.Select(e => (T)(-(dynamic)e));
        }
        public static Ani<T> operator *(Ani<T> value1, Ani<T> value2) {
            return value1.Combine(value2, (e1, e2) => (T)((dynamic)e1 * e2));
        }
        public static Ani<T> operator /(Ani<T> value1, Ani<T> value2) {
            return value1.Combine(value2, (e1, e2) => (T)((dynamic)e1 / e2));
        }
        public Ani<R> Cast<R>() {
            return this.Select(e => (R)(object)e);
        }
    }

    public static class Ani {
        public static Ani<T> Anon<T>(this Func<TimeSpan, T> func) {
            return new AnonymousAni<T>(func);
        }
        public static Ani<TOut> Select<TIn, TOut>(this Ani<TIn> ani, Func<TIn, TOut> projection) {
            return new AnonymousAni<TOut>(t => projection(ani.ValueAt(t)));
        }
        public static Ani<IEnumerable<TOut>> LiftSelect<TIn, TOut>(this Ani<IEnumerable<TIn>> ani, Func<TIn, TOut> projection) {
            return ani.Select(r => r.Select(projection));
        }
        public static Ani<IReadOnlyList<TOut>> LiftSelect<TIn, TOut>(this Ani<IReadOnlyList<TIn>> ani, Func<TIn, TOut> projection) {
            return ani.Select(r => r.Select(projection));
        }
        public static Ani<TOut> Combine<TIn1, TIn2, TOut>(this Ani<TIn1> ani, Ani<TIn2> ani2, Func<TIn1, TIn2, TOut> projection) {
            return new AnonymousAni<TOut>(t => projection(ani.ValueAt(t), ani2.ValueAt(t)));
        }
        public static Ani<TOut> Combine<TIn1, TIn2, TIn3, TOut>(this Ani<TIn1> ani, Ani<TIn2> ani2, Ani<TIn3> ani3, Func<TIn1, TIn2, TIn3, TOut> projection) {
            return new AnonymousAni<TOut>(t => projection(ani.ValueAt(t), ani2.ValueAt(t), ani3.ValueAt(t)));
        }
        public static Ani<TOut> Combine<TIn1, TIn2, TIn3, TIn4, TOut>(this Ani<TIn1> ani, Ani<TIn2> ani2, Ani<TIn3> ani3, Ani<TIn4> ani4, Func<TIn1, TIn2, TIn3, TIn4, TOut> projection) {
            return new AnonymousAni<TOut>(t => projection(ani.ValueAt(t), ani2.ValueAt(t), ani3.ValueAt(t), ani4.ValueAt(t)));
        }
        public static Ani<TOut> Combine<TIn1, TIn2, TIn3, TIn4, TIn5, TOut>(this Ani<TIn1> ani, Ani<TIn2> ani2, Ani<TIn3> ani3, Ani<TIn4> ani4, Ani<TIn5> ani5, Func<TIn1, TIn2, TIn3, TIn4, TIn5, TOut> projection) {
            return new AnonymousAni<TOut>(t => projection(ani.ValueAt(t), ani2.ValueAt(t), ani3.ValueAt(t), ani4.ValueAt(t), ani5.ValueAt(t)));
        }
        public static Ani<TOut> Combine<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut>(this Ani<TIn1> ani, Ani<TIn2> ani2, Ani<TIn3> ani3, Ani<TIn4> ani4, Ani<TIn5> ani5, Ani<TIn6> ani6, Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut> projection) {
            return new AnonymousAni<TOut>(t => projection(ani.ValueAt(t), ani2.ValueAt(t), ani3.ValueAt(t), ani4.ValueAt(t), ani5.ValueAt(t), ani6.ValueAt(t)));
        }
        public static void Watch<T>(this Ani<T> v, Lifetime life, IObservable<TimeSpan> pulse, Action<T> difAction, IEqualityComparer<T> eq = null) {
            if (v == null) throw new ArgumentNullException("v");
            if (pulse == null) throw new ArgumentNullException("pulse");
            if (difAction == null) throw new ArgumentNullException("difAction");
            pulse.DistinctUntilChanged()
                 .Select(v.ValueAt)
                 .DistinctUntilChanged(eq ?? EqualityComparer<T>.Default)
                 .Subscribe(difAction, life);
        }
    }

    public sealed class ConstantAni<T> : Ani<T> {
        private readonly T _value;
        public ConstantAni(T value) {
            _value = value;
        }
        public override T ValueAt(TimeSpan t) {
            return _value;
        }
    }
    public sealed class AnonymousAni<T> : Ani<T> {
        private readonly Func<TimeSpan, T> _valueAt;
        private TimeSpan? _lastIn;
        private T _lastOut;
        
        public AnonymousAni(Func<TimeSpan, T> valueAt) {
            if (valueAt == null) throw new ArgumentNullException("valueAt");
            _valueAt = valueAt;
        }
        public override T ValueAt(TimeSpan t) {
            if (_lastIn != t) {
                _lastIn = t;
                _lastOut = _valueAt(t);
            }
            return _lastOut;
        }
    }
}