AnimatronTheTerrible
====================

This is the *experimental* and *hacky* code I use to hack together visualizations for blog posts. It's not particularly efficient, and it's not particularly usable.

The general idea is to define and transform mappings from times to values, and link those mappings to controls on the canvas.

For example, adjusting the appropriate line in MainWindow to the following will show a blue line spinning like a clock hand:

'''C#
var animation = new Animation {
    new LineSegmentDesc(
        pos: Ani.Time.Select(t => new Point(100,100).Sweep(new Vector(Math.Cos(t.TotalSeconds), Math.Sin(t.TotalSeconds))*100)),
        stroke: Brushes.Blue)
};
'''

