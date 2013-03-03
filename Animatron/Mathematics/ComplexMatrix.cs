using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Strilanc.LinqToCollections;
using Animatron;

[DebuggerDisplay("{ToString()}")]
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
    public static ComplexMatrix MakeIdentity(int power) {
        return FromCellData((from i in power.Range()
                             from j in power.Range()
                             select i == j ? Complex.One : 0).ToArray());
    }
    public static ComplexMatrix MakeHadamard(int power) {
        if (power == 0) return FromCellData(1);
        var h = MakeHadamard(power - 1);
        return FromMatrixData(h, h, h, -h);
    }
    public static ComplexMatrix MakeUnitaryHadamard(int power) {
        return MakeHadamard(power)*Math.Pow(0.5, 0.5*power);
    }
    public static ComplexMatrix FromMatrixData(params ComplexMatrix[] cells) {
        var inSize = cells.First().Span;
        var outSize = (int)Math.Sqrt(cells.Length);
        var size = inSize * outSize;
        return FromColumns(size.Range().Select(c => size.Range().Select(r => cells.Deinterleave(outSize)[c / inSize][r / inSize].Columns[c % inSize][r % inSize]).ToArray()).ToArray());
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
        return new ComplexVector(
            matrix.Rows
            .Select(r => new ComplexVector(r) * vector)
            .ToArray());
    }
    public static ComplexMatrix operator -(ComplexMatrix matrix) {
        return matrix * -1;
    }
    public static ComplexMatrix operator *(ComplexMatrix matrix, Complex scale) {
        return FromColumns(matrix.Columns.Select(e => e.Select(c => c * scale).ToArray()).ToArray());
    }
    public static ComplexMatrix operator *(Complex scale, ComplexMatrix matrix) {
        return matrix*scale;
    }
    public static ComplexMatrix operator /(ComplexMatrix matrix, Complex scale) {
        return FromColumns(matrix.Columns.Select(e => e.Select(c => c / scale).ToArray()).ToArray());
    }
    public static ComplexMatrix operator *(ComplexMatrix left, ComplexMatrix right) {
        return new ComplexMatrix(left.Columns.Select(c => (new ComplexVector(c) * right).Values).ToArray());
    }
    public override string ToString() {
        return Rows.Select(r => r.Select(c => "| " + c.ToPrettyString().PadRight(6)).StringJoin("") + " |").StringJoin(Environment.NewLine);
    }
}