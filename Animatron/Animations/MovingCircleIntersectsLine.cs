using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;
using Animatron;
using SnipSnap.Mathematics;
using TwistedOak.Util;
using LineSegment = SnipSnap.Mathematics.LineSegment;

namespace Animations {
    public static class MovingCircleIntersectsLine {
        public static Animation Animate(Lifetime life) {
            var animation = new Animation();

            var state = animation.Dynamic(step => {
                var t = step.NextTotalElapsedTime.TotalSeconds;
                var x = Math.Cos(t)*100 + 150;
                var y = Math.Sin(t)*100 + 150;
                var vx = Math.Cos(3*t);
                var vy = Math.Sin(3*t);

                var c = new Point(x, y);
                var v = new Vector(vx, vy);
                var r = 20;
                var li = new LineSegment(new Point(150 - 89, 150 - 89), new Point(150 + 50, 150 + 20));
                var h = GeometryUtilities.WhenMovingCircleWillIntersectLineSegment(c, r, v, li);

                return new {c, r, v, li, h};
            });

            animation.Lines.Add(new LineSegmentDesc(state.Select(e => e.li)), life);
            animation.Points.Add(
                new PointDesc(
                    state.Select(e => e.c),
                    Brushes.Black.ToSingletonObservable(),
                    Brushes.Gray.ToSingletonObservable(),
                    state.Select(e => (double)e.r),
                    1.0.ToSingletonObservable()),
                life);
            animation.Lines.Add(new LineSegmentDesc(state.Select(e => e.c.Sweep(e.v*1000)), Brushes.Red.ToSingletonObservable()), life);
            animation.Lines.Add(new LineSegmentDesc(state.Select(e => e.c.Sweep(e.v * 1000) + e.v.Perp() * e.r), Brushes.LightGray.ToSingletonObservable()), life);
            animation.Lines.Add(new LineSegmentDesc(state.Select(e => e.c.Sweep(e.v * 1000) - e.v.Perp() * e.r), Brushes.LightGray.ToSingletonObservable()), life);
            animation.Points.Add(
                new PointDesc(
                    state.Select(e => (e.c + e.v * e.h) ?? new Point(-10000, -10000)), 
                    Brushes.Gray.ToSingletonObservable(), 
                    Brushes.LightGray.ToSingletonObservable(), 
                    state.Select(e => (double)e.r), 
                    1.0.ToSingletonObservable()), 
                life);

            return animation;
        }
    }
}