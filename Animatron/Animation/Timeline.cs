using System;
using System.Collections;
using System.Collections.Generic;
using System.Reactive;
using System.Windows;
using TwistedOak.Collections;
using TwistedOak.Util;
using System.Reactive.Linq;

namespace Animatron {
    public sealed class Timeline : IHasThings, IEnumerable<Unit>, IControlDescription {
        public PerishableCollection<IControlDescription> Things { get; private set; }
        private readonly Func<TimeSpan, TimeSpan?> _timeTransformFromOutsideToInside;
        public Ani<double> Proper { get; private set; }
        public Timeline(Func<TimeSpan, TimeSpan?> timeTransformFromOutsideToInside, Ani<double> proper) {
            Things = new PerishableCollection<IControlDescription>();
            this._timeTransformFromOutsideToInside = timeTransformFromOutsideToInside;
            this.Proper = proper;
        }

        public void Add(IControlDescription immortalThing) {
            Things.Add(immortalThing, Lifetime.Immortal);
        }
        public void Add(params IControlDescription[] immortalThing) {
            foreach (var e in immortalThing)
                Add(e);
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