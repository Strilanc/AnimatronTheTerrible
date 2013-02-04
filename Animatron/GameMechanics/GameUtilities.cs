using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SnipSnap.Mathematics;
using TwistedOak.Util;

namespace SnipSnap {
    ///<summary>Generic utility methods for working with the game.</summary>
    public static class GameUtilities {
        ///<summary>A lifetime that ends after the given duration has elapsed, in game time.</summary>
        public static Lifetime Delay(this Game game, TimeSpan duration) {
            var remaining = duration;
            var life = game.Life.CreateDependentSource();
            game.LoopActions.Add(
                step => {
                    remaining -= step.TimeStep;
                    if (remaining < TimeSpan.Zero) life.EndLifetime();
                },
                life.Lifetime);
            return life.Lifetime;
        }

        ///<summary>Given progress data about an animation during each iteration of the game loop.</summary>
        public delegate void AnimationCallback(Step step, double proportionCompleted, TimeSpan elapsed);

        /// <summary>
        /// Manages tracking the progress of an animation, running a callback with the information.
        /// Returns a lifetime that ends when the animation has expired.
        /// </summary>
        public static Lifetime AnimateWith(this Game game, TimeSpan duration, AnimationCallback callback, Lifetime? constraint = default(Lifetime?)) {
            var remaining = duration;
            var life = (constraint ?? game.Life).CreateDependentSource();
            game.LoopActions.Add(
                step => {
                    remaining -= step.TimeStep;
                    if (remaining >= TimeSpan.Zero) {
                        callback(step, 1 - remaining.TotalSeconds / duration.TotalSeconds, duration - remaining);
                    } else {
                        life.EndLifetime();
                    }
                },
                life.Lifetime);
            return life.Lifetime;
        }

        ///<summary>A lifetime then ends after the lifetime created by a function (triggered when the given lifetime ends) ends.</summary>
        public static Lifetime ThenResurrect(this Lifetime lifetime, Func<Lifetime> resurrectedLifetimeFunc) {
            var r = new LifetimeSource();
            lifetime.WhenDead(() => resurrectedLifetimeFunc().WhenDead(r.EndLifetime));
            return r.Lifetime;
        }
    }
}