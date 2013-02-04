using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Strilanc.Angle;

public struct TextDesc {
    public readonly Brush Foreground;
    public readonly Dir Direction;
    public readonly Point Pos;
    public readonly Point Reference;
    public readonly FontStyle FontStyle;
    public readonly FontFamily FontFamily;
    public readonly string Text;
    public readonly FontWeight FontWeight;
    public readonly double FontSize;
    public TextDesc(string text, Point pos, Point? reference = null, FontStyle? fontStyle = null, FontFamily fontFamily = null, FontWeight? fontWeight = null, double fontSize = 12, Dir direction = default(Dir), Brush foreground = null) {
        this.Pos = pos;
        this.Reference = reference ?? new Point(0, 1);
        this.FontStyle = fontStyle ?? FontStyles.Normal;
        this.FontFamily = fontFamily ?? new FontFamily();
        this.Text = text;
        this.FontWeight = fontWeight ?? FontWeights.Normal;
        this.FontSize = fontSize;
        this.Direction = direction;
        this.Foreground = foreground ?? new SolidColorBrush(Colors.Black);
    }
    public void Draw(TextBlock textBlock) {
        textBlock.Text = Text;
        textBlock.SetValue(Canvas.LeftProperty, Pos.X - Reference.X * textBlock.ActualWidth);
        textBlock.SetValue(Canvas.TopProperty, Pos.Y - Reference.Y * textBlock.ActualHeight);
        textBlock.RenderTransformOrigin = Reference;
        textBlock.RenderTransform = new RotateTransform(Direction.GetSignedAngle(Basis.FromDirectionAndUnits(Dir.AlongPositiveX, Basis.DegreesPerRotation, false)));
        textBlock.FontStyle = FontStyle;
        textBlock.FontFamily = FontFamily;
        textBlock.FontWeight = FontWeight;
        textBlock.FontSize = FontSize;
        textBlock.Foreground = Foreground;
    }
}