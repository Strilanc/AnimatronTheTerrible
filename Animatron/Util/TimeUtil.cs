using System;
using System.Threading;
using System.Threading.Tasks;

namespace TwistedOak.Element.Env {
    public static class TimeUtil {
        /// <summary>Returns a timespan with duration equal to the given number of seconds.</summary>
        public static TimeSpan Seconds(this int seconds) {
            return TimeSpan.FromSeconds(seconds);
        }
        /// <summary>Returns a timespan with duration equal to the given number of seconds.</summary>
        public static TimeSpan Seconds(this double seconds) {
            return TimeSpan.FromSeconds(seconds);
        }
        /// <summary>Returns a timespan with duration equal to the given number of milliseconds.</summary>
        public static TimeSpan Milliseconds(this int milliseconds) {
            return TimeSpan.FromMilliseconds(milliseconds);
        }
        /// <summary>Returns a timespan with duration equal to the given number of milliseconds.</summary>
        public static TimeSpan Milliseconds(this double milliseconds) {
            return TimeSpan.FromMilliseconds(milliseconds);
        }

        /// <summary>Scales the timespan by a factor.</summary>
        public static TimeSpan Times(this TimeSpan duration, double factor) {
            return new TimeSpan((long)(duration.Ticks * factor));
        }
        /// <summary>Scales the timespan by a factor.</summary>
        public static TimeSpan Times(this TimeSpan duration, long factor) {
            return new TimeSpan(duration.Ticks * factor);
        }
        /// <summary>Scales the timespan over a factor.</summary>
        public static TimeSpan DividedBy(this TimeSpan duration, double factor) {
            if (factor == 0) throw new DivideByZeroException();
            return new TimeSpan((long)(duration.Ticks/factor));
        }
        /// <summary>Scales the timespan over a factor.</summary>
        public static TimeSpan DividedBy(this TimeSpan duration, long factor) {
            return new TimeSpan(duration.Ticks / factor);
        }
        /// <summary>Returns the ratio between two time spans.</summary>
        public static double DividedBy(this TimeSpan duration, TimeSpan divisor) {
            if (divisor.Ticks == 0) throw new DivideByZeroException();
            return duration.Ticks / (double)divisor.Ticks;
        }
        ///<summary>Returns the largest duration that is not larger than the given value but is a multiple of the given multiple.</summary>
        public static TimeSpan FloorMultiple(this TimeSpan value, TimeSpan multiple) {
            return multiple.Times(Math.Floor(value.DividedBy(multiple)));
        }
        ///<summary>Returns the smallest duration that is not smaller than the given value but is a multiple of the given multiple.</summary>
        public static TimeSpan CeilingMultiple(this TimeSpan value, TimeSpan multiple) {
            return multiple.Times(Math.Ceiling(value.DividedBy(multiple)));
        }
        ///<summary>Returns the non-negative remainder of dividing one duration by another.</summary>
        public static TimeSpan Mod(this TimeSpan value, TimeSpan divisor) {
            return value - value.FloorMultiple(divisor);
        }
        ///<summary>Returns a remainder with smallest magnitude modulo equivalent to the remainder left over by dividing one duration by another.</summary>
        public static TimeSpan DifMod(this TimeSpan value, TimeSpan divisor) {
            var r = value.Mod(divisor);
            if (r.Times(2) > divisor) r -= divisor;
            return r;
        }
        /// <summary>Returns the positive magnitude of the timespan away from 0.</summary>
        public static TimeSpan Abs(this TimeSpan duration) {
            return duration > TimeSpan.Zero ? duration : -duration;
        }
    }
}
