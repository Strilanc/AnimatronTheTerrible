using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SnipSnap.Mathematics;
using TwistedOak.Collections;
using TwistedOak.Element.Env;
using TwistedOak.Util;
using System.Linq;

namespace Animatron {
    public sealed class Animation : IHasThings, IEnumerable<Unit>, IControlDescription {
        public readonly PerishableCollection<Action<Step>> StepActions = new PerishableCollection<Action<Step>>(); 
        public readonly PerishableCollection<UIElement> Controls = new PerishableCollection<UIElement>();
        public PerishableCollection<IControlDescription> Things { get; private set; }

        public Ani<double> Proper { get { return Ani.Anon(t => 1 - Math.Exp(-t.TotalSeconds)); } }
        public void LinkMany(IAni<IEnumerable<IControlDescription>> values, Lifetime life) {
            LinkMany(values, Things, life);
        }
        private void LinkMany<T>(IAni<IEnumerable<T>> values, PerishableCollection<T> col, Lifetime life) {
            var d = new Dictionary<T, LifetimeSource>();
            NextElapsedTime().SubscribeLife(
                t => {
                    var cur = values.ValueAt(t).Distinct().ToArray();
                    var stale = d.Keys.Except(cur).ToArray();
                    var fresh = cur.Except(d.Keys).ToArray();
                    foreach (var e in fresh) {
                        d.Add(e, life.CreateDependentSource());
                        col.Add(e, d[e].Lifetime);
                    }
                    foreach (var e in stale) {
                        d[e].EndLifetime();
                        d.Remove(e);
                    }
                },
                life);
        }

        public IObservable<TimeSpan> NextElapsedTime() {
            return Dynamic(step => step.NextTotalElapsedTime);
        }
        public IObservable<T> Dynamic<T>(Func<Step, T> stepper) {
            return new AnonymousObservable<T>(observer => {
                var life = new DisposableLifetime();
                StepActions.Add(
                    step => observer.OnNext(stepper(step)),
                    life.Lifetime);
                return life;
            });
        }
        public IObservable<T> Dynamic<T>(T seed, Func<T, Step, T> stepper) {
            return new AnonymousObservable<T>(observer => {
                var cur = seed;
                return Dynamic(step => cur = stepper(cur, step)).Subscribe(observer);
            });
        }
        public void Add(IControlDescription immortalThing) {
            Things.Add(immortalThing, Lifetime.Immortal);
        }
        public void Add(IEnumerable<IControlDescription> immortalThing) {
            foreach (var e in immortalThing)
                Add(e);
        }
        public Animation() {
            Things = new PerishableCollection<IControlDescription>();
            Things.CurrentAndFutureItems().Subscribe(e => e.Value.Link(Controls, NextElapsedTime(), e.Lifetime));
        }
        public async Task Run(Lifetime life, TimeSpan? delayTime = default(TimeSpan?)) {
            var clock = new Stopwatch();
            clock.Start();

            var lastTime = clock.Elapsed;
            var lostTime = 0.Seconds();
            while (!life.IsDead) {
                var dt = clock.Elapsed - lastTime;
                var cdt = dt.Clamp(TimeSpan.Zero, TimeSpan.FromSeconds(1));
                lostTime += dt - cdt;

                var step = new Step(
                    previousTotalElapsedTime: lastTime, 
                    timeStep: cdt);
                lastTime += cdt;

                foreach (var e in StepActions.CurrentItems())
                    e.Value.Invoke(step);

                await Task.Delay(delayTime ?? 30.Milliseconds());
            }
        }
        public void LinkToCanvas(Canvas canvas, Lifetime life) {
            Controls.CurrentAndFutureItems().SubscribeLife(
                e => {
                    canvas.Children.Add(e.Value);
                    e.Lifetime.WhenDead(() => canvas.Children.Remove(e.Value));
                },
                life);
        }
        public IEnumerator<Unit> GetEnumerator() {
            throw new NotSupportedException();
        }
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
        public void Link(PerishableCollection<UIElement> controls, IObservable<TimeSpan> pulse, Lifetime life) {
            Things.CurrentAndFutureItems().Subscribe(e => e.Value.Link(controls, pulse, e.Lifetime.Min(life)));
        }
    }
}