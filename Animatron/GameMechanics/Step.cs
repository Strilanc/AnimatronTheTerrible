using System;
using System.Diagnostics;

[DebuggerDisplay("{ToString()}")]
public struct Step {
    public readonly TimeSpan PreviousTotalElapsedTime;
    public readonly TimeSpan TimeStep;
    public TimeSpan NextTotalElapsedTime { get { return PreviousTotalElapsedTime + TimeStep; } }
    public Step(TimeSpan previousTotalElapsedTime, TimeSpan timeStep) {
        this.TimeStep = timeStep;
        this.PreviousTotalElapsedTime = previousTotalElapsedTime;
    }
    public override string ToString() {
        return TimeStep.TotalMilliseconds + "ms";
    }
}