using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

public struct PointDesc {
    public readonly Point Pos;
    public readonly Brush Stroke;
    public readonly Brush Fill;
    public readonly double Radius;
    public readonly double StrokeThickness;
    public PointDesc(Point pos, Brush stroke, Brush fill, double radius, double strokeThickness) {
        this.Pos = pos;
        this.Stroke = stroke;
        this.Fill = fill;
        this.Radius = radius;
        this.StrokeThickness = strokeThickness;
    }
    public void Draw(Ellipse ellipse) {
        ellipse.SetValue(Canvas.LeftProperty, Pos.X - Radius);
        ellipse.SetValue(Canvas.TopProperty, Pos.Y - Radius);
        ellipse.Width = Radius * 2;
        ellipse.Height = Radius * 2;
        ellipse.Fill = Fill;
        ellipse.Stroke = Stroke;
        ellipse.StrokeThickness = StrokeThickness;
    }
}