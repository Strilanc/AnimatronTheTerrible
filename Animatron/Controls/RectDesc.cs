using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Strilanc.Angle;
using TwistedOak.Collections;
using TwistedOak.Util;
using Animatron;
using SnipSnap.Mathematics;

public sealed class RectDesc : IControlDescription<Rectangle> {
    public readonly Ani<Rect> Pos;
    public readonly Ani<Brush> Stroke;
    public readonly Ani<Brush> Fill;
    public readonly Ani<double> StrokeThickness;
    public readonly Ani<double> Dashed;
    public readonly Ani<Turn> Rotation;
    public readonly Ani<Point> RotationOrigin;
    public RectDesc(Ani<Rect> pos,
                    Ani<Brush> stroke = null,
                    Ani<Brush> fill = null,
                    Ani<double> strokeThickness = null,
                    Ani<double> dashed = null,
                    Ani<Turn> rotation = null,
                    Ani<Point> rotationOrigin = null) {
        if (pos == null) throw new ArgumentNullException("pos");
        this.Pos = pos;
        this.Stroke = stroke ?? Brushes.Transparent;
        this.Fill = fill ?? Brushes.Transparent;
        this.StrokeThickness = strokeThickness ?? 0;
        this.Dashed = dashed ?? 0;
        this.Rotation = rotation ?? Turn.FromNaturalAngle(0.0);
        this.RotationOrigin = rotationOrigin ?? new Point(0, 0);
    }
    public void Link(Rectangle rectangle, IObservable<TimeSpan> pulse, Lifetime life) {
        var basis = Basis.FromDirectionAndUnits(Dir.AlongPositiveX, Basis.DegreesPerRotation, false);

        Pos.Select(e => e.X).Watch(life, pulse, e => rectangle.SetValue(Canvas.LeftProperty, e));
        Pos.Select(e => e.Y).Watch(life, pulse, e => rectangle.SetValue(Canvas.TopProperty, e));
        Pos.Select(e => e.Width).Watch(life, pulse, e => rectangle.Width = e);
        Pos.Select(e => e.Height).Watch(life, pulse, e => rectangle.Height = e);
        Fill.Watch(life, pulse, e => rectangle.Fill = e);
        Stroke.Watch(life, pulse, e => rectangle.Stroke = e);
        StrokeThickness.Watch(life, pulse, e => rectangle.StrokeThickness = e);
        RotationOrigin.Watch(life, pulse, e => rectangle.RenderTransformOrigin = new Point(e.X.Clamp(double.MinValue, double.MaxValue), e.Y.Clamp(double.MinValue, double.MaxValue)));
        Rotation.Watch(life, pulse, e => rectangle.RenderTransform = new RotateTransform(e.GetAngle(basis)));
        Dashed.Watch(life, pulse, e => {
            rectangle.StrokeDashArray.Clear();
            if (e != 0) rectangle.StrokeDashArray.Add(e);
        });
    }
    public void Link(PerishableCollection<UIElement> controls, IObservable<TimeSpan> pulse, Lifetime life) {
        var r = new Rectangle();
        Link(r, pulse, life);
        controls.Add(r, life);
    }
}