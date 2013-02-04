using System.Windows.Media;
using System.Windows.Shapes;
using LineSegment = SnipSnap.Mathematics.LineSegment;

public struct LineSegmentDesc {
    public readonly LineSegment Pos;
    public readonly Brush Stroke;
    public readonly double Thickness;
    public readonly bool Dashed;
    public LineSegmentDesc(LineSegment pos, Brush stroke, double thickness, bool dashed = false) {
        this.Pos = pos;
        this.Stroke = stroke;
        this.Thickness = thickness;
        this.Dashed = dashed;
    }
    public void Draw(Line line) {
        line.X1 = Pos.Start.X;
        line.X2 = Pos.End.X;
        line.Y1 = Pos.Start.Y;
        line.Y2 = Pos.End.Y;
        line.Stroke = Stroke;
        line.StrokeThickness = Thickness;
        line.StrokeDashArray.Clear();
        if (Dashed) line.StrokeDashArray.Add(1.0);
    }
}