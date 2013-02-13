using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Animatron;
using Strilanc.Angle;
using TwistedOak.Util;

public sealed class TextDesc {
    public readonly IObservable<Brush> Foreground;
    public readonly IObservable<Dir> Direction;
    public readonly IObservable<Point> Pos;
    public readonly IObservable<Point> Reference;
    public readonly IObservable<FontStyle> FontStyle;
    public readonly IObservable<FontFamily> FontFamily;
    public readonly IObservable<string> Text;
    public readonly IObservable<FontWeight> FontWeight;
    public readonly IObservable<double> FontSize;
    public TextDesc(IObservable<string> text,
                    IObservable<Point> pos,
                    IObservable<Point> reference = null,
                    IObservable<FontStyle> fontStyle = null,
                    IObservable<FontFamily> fontFamily = null,
                    IObservable<FontWeight> fontWeight = null,
                    IObservable<double> fontSize = null,
                    IObservable<Dir> direction = null,
                    IObservable<Brush> foreground = null) {
        if (text == null) throw new ArgumentNullException("text");
        if (pos == null) throw new ArgumentNullException("pos");
        this.Pos = pos;
        this.Text = text;
        this.Reference = reference ?? new Point(0, 1).ToSingletonObservable();
        this.FontStyle = fontStyle ?? FontStyles.Normal.ToSingletonObservable();
        this.FontFamily = fontFamily ?? new FontFamily().ToSingletonObservable();
        this.FontWeight = fontWeight ?? FontWeights.Normal.ToSingletonObservable();
        this.FontSize = fontSize ?? 12.0.ToSingletonObservable();
        this.Direction = direction ?? default(Dir).ToSingletonObservable();
        this.Foreground = foreground ?? Brushes.Black.ToSingletonObservable();
    }
    public void Link(TextBlock textBlock, Lifetime lifetime) {
        Text.DistinctUntilChanged().Subscribe(e => textBlock.Text = e, lifetime);
        var size = new ObservableValue<Size>(new Size(textBlock.ActualWidth, textBlock.ActualHeight));
        textBlock.SizeChanged += (sender, arg) => size.Update(arg.NewSize);
        var xy = Pos.CombineLatest(Reference, size, (p, r, s) => p - new Vector(r.X * s.Width, r.Y * s.Height));
        xy.Select(e => e.X).DistinctUntilChanged().Subscribe(e => textBlock.SetValue(Canvas.LeftProperty, e), lifetime);
        xy.Select(e => e.Y).DistinctUntilChanged().Subscribe(e => textBlock.SetValue(Canvas.TopProperty, e), lifetime);
        Reference.DistinctUntilChanged().Subscribe(e => textBlock.RenderTransformOrigin = e, lifetime);

        FontStyle.DistinctUntilChanged().Subscribe(e => textBlock.FontStyle = e, lifetime);
        FontFamily.DistinctUntilChanged().Subscribe(e => textBlock.FontFamily = e, lifetime);
        FontWeight.DistinctUntilChanged().Subscribe(e => textBlock.FontWeight = e, lifetime);
        FontSize.DistinctUntilChanged().Subscribe(e => textBlock.FontSize = e, lifetime);
        Foreground.DistinctUntilChanged().Subscribe(e => textBlock.Foreground = e, lifetime);
    }
}