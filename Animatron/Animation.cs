using System;
using System.Diagnostics;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using SnipSnap.Mathematics;
using TwistedOak.Collections;
using TwistedOak.Element.Env;
using TwistedOak.Util;

namespace Animatron {
    public sealed class Animation {
        public readonly PerishableCollection<Action<Step>> StepActions = new PerishableCollection<Action<Step>>(); 
        public readonly PerishableCollection<UIElement> Controls = new PerishableCollection<UIElement>();
        public readonly PerishableCollection<IObservable<PointDesc>> Points = new PerishableCollection<IObservable<PointDesc>>();
        public readonly PerishableCollection<IObservable<LineSegmentDesc>> Lines = new PerishableCollection<IObservable<LineSegmentDesc>>();
        public readonly PerishableCollection<IObservable<TextDesc>> Labels = new PerishableCollection<IObservable<TextDesc>>();

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
        public Animation() {
            Points.CurrentAndFutureItems().Subscribe(e => {
                var r = new Ellipse();
                e.Value.Subscribe(p => p.Draw(r), e.Lifetime);
                Controls.Add(r, e.Lifetime);
            });
            Lines.CurrentAndFutureItems().Subscribe(e => {
                var r = new Line();
                e.Value.Subscribe(p => p.Draw(r), e.Lifetime);
                Controls.Add(r, e.Lifetime);
            });
            Labels.CurrentAndFutureItems().Subscribe(e => {
                var r = new TextBlock();
                e.Value.Subscribe(p => p.Draw(r), e.Lifetime);
                Controls.Add(r, e.Lifetime);
            });
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
            Controls.CurrentAndFutureItems().Subscribe(
                e => {
                    canvas.Children.Add(e.Value);
                    e.Lifetime.WhenDead(() => canvas.Children.Remove(e.Value));
                },
                life);
        }
    }
}