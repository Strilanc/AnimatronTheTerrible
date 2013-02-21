using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Animatron;
using Strilanc.Angle;
using TwistedOak.Collections;
using TwistedOak.Util;

public sealed class TextDesc : IControlDescription<TextBlock> {
    public readonly Ani<Brush> Foreground;
    public readonly Ani<Dir> Direction;
    public readonly Ani<Point> Pos;
    public readonly Ani<Point> Reference;
    public readonly Ani<FontStyle> FontStyle;
    public readonly Ani<FontFamily> FontFamily;
    public readonly Ani<string> Text;
    public readonly Ani<FontWeight> FontWeight;
    public readonly Ani<double> FontSize;
    public TextDesc(Ani<string> text,
                    Ani<Point> pos,
                    Ani<Point> reference = null,
                    Ani<FontStyle> fontStyle = null,
                    Ani<FontFamily> fontFamily = null,
                    Ani<FontWeight> fontWeight = null,
                    Ani<double> fontSize = null,
                    Ani<Dir> direction = null,
                    Ani<Brush> foreground = null) {
        if (text == null) throw new ArgumentNullException("text");
        if (pos == null) throw new ArgumentNullException("pos");
        this.Pos = pos;
        this.Text = text;
        this.Reference = reference ?? new Point(0, 1);
        this.FontStyle = fontStyle ?? FontStyles.Normal;
        this.FontFamily = fontFamily ?? new FontFamily();
        this.FontWeight = fontWeight ?? FontWeights.Normal;
        this.FontSize = fontSize ?? 12;
        this.Direction = direction ?? default(Dir);
        this.Foreground = foreground ?? Brushes.Black;
    }
    public void Link(TextBlock textBlock, IObservable<TimeSpan> pulse, Lifetime life) {
        Text.Watch(life, pulse, e => textBlock.Text = e);
        var size = new AnonymousAni<Size>(t => new Size(textBlock.ActualWidth, textBlock.ActualHeight));
        var xy = Pos.Combine(Reference, size, (p, r, s) => p - new Vector(r.X * s.Width, r.Y * s.Height));
        xy.Select(e => e.X).Watch(life, pulse, e => textBlock.SetValue(Canvas.LeftProperty, e));
        xy.Select(e => e.Y).Watch(life, pulse, e => textBlock.SetValue(Canvas.TopProperty, e));
        Reference.Watch(life, pulse, e => textBlock.RenderTransformOrigin = e);

        var basis = Basis.FromDirectionAndUnits(Dir.AlongPositiveX, Basis.DegreesPerRotation, false);
        Direction.Watch(life, pulse, e => textBlock.RenderTransform = new RotateTransform(basis.DirToSignedAngle(e)));
        FontStyle.Watch(life, pulse, e => textBlock.FontStyle = e);
        FontFamily.Watch(life, pulse, e => textBlock.FontFamily = e);
        FontWeight.Watch(life, pulse, e => textBlock.FontWeight = e);
        FontSize.Watch(life, pulse, e => textBlock.FontSize = e);
        Foreground.Watch(life, pulse, e => textBlock.Foreground = e);
    }
    public void Link(PerishableCollection<UIElement> controls, IObservable<TimeSpan> pulse, Lifetime life) {
        var r = new TextBlock();
        Link(r, pulse, life);
        controls.Add(r, life);
    }
}