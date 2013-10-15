using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SnipSnap.Mathematics;
using Animations;

public sealed class QuantumSuperPosition<T> {
    public readonly IReadOnlyDictionary<T, Complex> Dictionary;
    public QuantumSuperPosition(IReadOnlyDictionary<T, Complex> dictionary) {
        this.Dictionary = dictionary;

        // well-formed superpositions must add up to 100%:
        //var totalProbability = dictionary.Values.Select(e => e.SquaredMagnitude()).Sum();
        //if ((totalProbability-1).Abs() > 0.000001) throw new ArgumentOutOfRangeException("dictionary", "Squared magnitudes must add up to 1.");
    }
}
