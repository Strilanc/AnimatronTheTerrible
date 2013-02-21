using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive;
using System.Windows;
using TwistedOak.Collections;
using TwistedOak.Util;
using System.Reactive.Linq;
using TwistedOak.Element.Env;

namespace Animatron {
    public sealed class Timeline : IEnumerable<Unit>, IControlDescription {
        public readonly PerishableCollection<IControlDescription> Things = new PerishableCollection<IControlDescription>();
        private readonly Func<TimeSpan, TimeSpan?> _timeTransformFromOutsideToInside;
        public Timeline(Func<TimeSpan, TimeSpan?> timeTransformFromOutsideToInside) {
            this._timeTransformFromOutsideToInside = timeTransformFromOutsideToInside;
        }
        public static Timeline Where(Func<TimeSpan, bool> predicate) {
            return new Timeline(e => predicate(e) ? (TimeSpan?)e : null);
        }
        public static Timeline Limited(TimeSpan start, TimeSpan finish) {
            return new Timeline(e => e < start || e > finish ? null : (TimeSpan?)(e - start));
        }
        public static Timeline Periodic(TimeSpan period) {
            return new Timeline(e => e.Mod(period));
        }
        public static Timeline Dilated(TimeSpan oneSecond, TimeSpan zero = default(TimeSpan)) {
            return new Timeline(e => ((e - zero).DividedBy(oneSecond)).Seconds());
        }

        public void Add(IControlDescription immortalThing) {
            Things.Add(immortalThing, Lifetime.Immortal);
        }
        public void Add(IEnumerable<IControlDescription> immortalThing) {
            foreach (var e in immortalThing)
                Add(e);
        }
        public IEnumerator<Unit> GetEnumerator() {
            throw new NotSupportedException();
        }
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
        public void Link(PerishableCollection<UIElement> controls, IObservable<TimeSpan> pulse, Lifetime life) {
            LifetimeSource active = null;
            var v = new ObservableValue<TimeSpan>();
            pulse.Select(_timeTransformFromOutsideToInside).Subscribe(e => {
                if (active == null && e.HasValue) {
                    active = life.CreateDependentSource();
                    Things.CurrentAndFutureItems().Subscribe(x => x.Value.Link(controls, v, x.Lifetime.Min(active.Lifetime)), active.Lifetime);
                } else if (active != null && !e.HasValue) {
                    active.EndLifetime();
                    active = null;
                }
                if (e.HasValue) v.Update(e.Value);
            }, life);
        }
    }
}