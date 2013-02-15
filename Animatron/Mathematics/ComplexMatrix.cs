using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Strilanc.LinqToCollections;
using SnipSnap.Mathematics;

public struct ComplexMatrix {
    private readonly IReadOnlyList<IReadOnlyList<Complex>> _columns;
    private ComplexMatrix(IReadOnlyList<IReadOnlyList<Complex>> columns) {
        if (columns == null) throw new ArgumentNullException("columns");
        if (columns.Any(col => col.Count != columns.Count)) throw new ArgumentException("Not square");
        this._columns = columns;
    }

    public static ComplexMatrix FromColumns(IReadOnlyList<IReadOnlyList<Complex>> columns) {
        return new ComplexMatrix(columns);
    }
    public static ComplexMatrix FromCellData(params Complex[] cells) {
        var size = (int)Math.Sqrt(cells.Length);
        var cols = cells.Deinterleave(size);
        return FromColumns(cols);
    }

    public int Span { get { return _columns == null ? 0 : _columns.Count; } }
    public IReadOnlyList<IReadOnlyList<Complex>> Columns { get { return _columns ?? ReadOnlyList.Empty<IReadOnlyList<Complex>>(); } }
    public IReadOnlyList<IReadOnlyList<Complex>> Rows {
        get {
            var r = Columns;
            return new AnonymousReadOnlyList<IReadOnlyList<Complex>>(
                r.Count,
                row => new AnonymousReadOnlyList<Complex>(
                    r.Count,
                    col => r[col][row]));
        }
    }
    public static ComplexVector operator *(ComplexVector vector, ComplexMatrix matrix) {
        return new ComplexVector(matrix.Columns
            .Select(col => col.Zip(vector.Values, (e1, e2) => e1 * e2).Sum())
            .ToArray());
    }
}