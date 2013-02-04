using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SnipSnap.Mathematics;
using Strilanc.Angle;
using Strilanc.LinqToCollections;
using TwistedOak.Collections;
using TwistedOak.Element.Env;
using TwistedOak.Util;
using System.Reactive.Linq;
using LineSegment = SnipSnap.Mathematics.LineSegment;

namespace Animatron {
    public partial class MainWindow {
        public bool Recording = false;
        public MainWindow() {
            InitializeComponent();
            txtPath.Text = string.IsNullOrWhiteSpace(Properties.Settings.Default.path) ? 
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GifRecordings") :
                Properties.Settings.Default.path;
            txtPath.TextChanged += (sender, arg) => {
                Properties.Settings.Default.path = txtPath.Text;
                Properties.Settings.Default.Save();
            };
            btnRecord.Click += (sender, arg) => {
                if (!Recording) {
                    if (!Directory.Exists(txtPath.Text)) {
                        try {
                            Directory.CreateDirectory(txtPath.Text);
                        } catch {
                            MessageBox.Show("Directory doesn't exist and couldn't be created!");
                            return;
                        }
                    }
                    txtPath.IsEnabled = false;
                    btnRecord.Content = "Stop Recording";
                    focus.Visibility = Visibility.Hidden;
                    Recording = true;
                } else {
                    Recording = false;
                    focus.Visibility = Visibility.Visible;
                    btnRecord.Content = "Start Recording";
                    txtPath.IsEnabled = true;
                }
            };
            Action updateFocus = () => {
                Properties.Settings.Default.focusLeft = 0;
                Properties.Settings.Default.focusTop = 0;
                Properties.Settings.Default.focusHeight = Math.Max(50, Properties.Settings.Default.focusHeight);
                Properties.Settings.Default.focusWidth = Math.Max(50, Properties.Settings.Default.focusWidth);
                focus.Margin = new Thickness(
                    Properties.Settings.Default.focusLeft,
                    Properties.Settings.Default.focusTop,
                    grid.ActualWidth - Properties.Settings.Default.focusWidth - Properties.Settings.Default.focusLeft,
                    grid.ActualHeight - Properties.Settings.Default.focusHeight - Properties.Settings.Default.focusTop);
                Properties.Settings.Default.Save();
            };
            this.Loaded += (sender, arg) => {
                Animate(Lifetime.Immortal);
                updateFocus();
            };
            var oldPos = (Point?)null;
            this.SizeChanged += (sender, arg) => updateFocus();
            var mx = 0;
            var my = 0;
            focus.MouseMove += (sender, arg) => {
                var p = arg.GetPosition(focus);
                if (arg.LeftButton == MouseButtonState.Released) {
                    var x = Math.Sign(((int)Math.Floor(p.X / focus.ActualWidth * 5) - 2) / 2);
                    var y = Math.Sign(((int)Math.Floor(p.Y / focus.ActualHeight * 5) - 2) / 2);
                    Mouse.SetCursor(
                        x != 0 && y != 0 ? (x == y ? Cursors.SizeNWSE : Cursors.SizeNESW) :
                        x != 0 ? Cursors.SizeWE :
                        y != 0 ? Cursors.SizeNS :
                        Cursors.SizeAll);
                    mx = x;
                    my = y;
                } else if (mx != 2) {
                    Mouse.SetCursor(
                        mx != 0 && my != 0 ? (mx == my ? Cursors.SizeNWSE : Cursors.SizeNESW) :
                        mx != 0 ? Cursors.SizeWE :
                        my != 0 ? Cursors.SizeNS :
                        Cursors.SizeAll);

                }
            };
            focus.MouseLeave += (sender, arg) => Mouse.SetCursor(Cursors.Arrow);
            this.MouseUp += (sender, arg) => Mouse.SetCursor(Cursors.Arrow);
            this.MouseMove += (sender, arg) => {
                var p = arg.GetPosition(grid);
                var d = p - oldPos;
                oldPos = p;
                if (d.HasValue && arg.LeftButton == MouseButtonState.Pressed) {
                    var e = d.Value;
                    if (mx == -1) {
                        Properties.Settings.Default.focusLeft += e.X;
                        Properties.Settings.Default.focusWidth -= e.X;
                    }
                    if (mx == 1) Properties.Settings.Default.focusWidth += e.X;
                    if (my == -1) {
                        Properties.Settings.Default.focusTop += e.Y;
                        Properties.Settings.Default.focusHeight -= e.Y;
                    }
                    if (my == 1) Properties.Settings.Default.focusHeight += e.Y;
                    if (mx == 0 && my == 0) {
                        Properties.Settings.Default.focusLeft += e.X;
                        Properties.Settings.Default.focusTop += e.Y;
                    }
                    updateFocus();
                }
            };
        }

        private sealed class EndPoint {
            public readonly string Name;
            public readonly TimeSpan Skew;
            public EndPoint(string name, TimeSpan skew) {
                Name = name;
                Skew = skew;
            }
        }
        private sealed class Message {
            public readonly string Text;
            public readonly EndPointGraph System;
            public readonly EndPoint Source;
            public readonly EndPoint Destination;
            public readonly TimeSpan SentTime;
            public TimeSpan ArrivalTime { get { return SentTime + System.Delays[Tuple.Create(Source, Destination)]; } }
            public Message(string text, EndPointGraph system, EndPoint source, EndPoint destination, TimeSpan sentTime) {
                Text = text;
                System = system;
                Source = source;
                Destination = destination;
                SentTime = sentTime;
            }
            public Point PosSourcePoint { get { return new Point(System.GetX(SentTime), System.GetY(Source)); } }
            public Point PosEndPoint { get { return new Point(System.GetX(ArrivalTime), System.GetY(Destination)); } }
            public LineSegment Pos { get { return new LineSegment(PosSourcePoint, PosEndPoint); } }
        }
        private sealed class EndPointGraph {
            public readonly IReadOnlyList<EndPoint> EndPoints;
            public readonly IReadOnlyDictionary<Tuple<EndPoint, EndPoint>, TimeSpan> Delays;
            public EndPointGraph(IReadOnlyList<EndPoint> endPoints, IReadOnlyDictionary<Tuple<EndPoint, EndPoint>, TimeSpan> delays) {
                EndPoints = endPoints;
                Delays = delays;
            }
            public double GetX(TimeSpan t) {
                return t.TotalSeconds*100 + 150;
            }
            public double GetY(EndPoint endPoint) {
                for (var i = 0; i < EndPoints.Count; i++)
                    if (Equals(EndPoints[i], endPoint)) return i*100 + 100;
                throw new InvalidOperationException();
            }
        }
        private sealed class Measurement {
            public readonly string Text;
            public readonly EndPoint V1;
            public readonly EndPoint V2;
            public readonly TimeSpan X1;
            public readonly TimeSpan X2;
            public readonly double? Y;
            public readonly Dir Angle;
            public Measurement(string text, EndPoint v1, EndPoint v2, TimeSpan x1, TimeSpan x2, double? y, Dir angle = default(Dir)) {
                Text = text;
                V1 = v1;
                V2 = v2;
                X1 = x1;
                X2 = x2;
                Y = y;
                Angle = angle;
            }
        }
        private sealed class GraphMessages {
            public readonly EndPointGraph Graph;
            public readonly IReadOnlyList<Message> Messages;
            public readonly IReadOnlyList<Measurement> Measurements;
            public GraphMessages(EndPointGraph graph, IReadOnlyList<Message> messages, IReadOnlyList<Measurement> measurements) {
                Graph = graph;
                Messages = messages;
                Measurements = measurements;
            }
        }
        private async Task Animate(Lifetime life) {
            var animation = new Animation();
            animation.LinkToCanvas(canvas, life);

            var stateD = animation.Dynamic(step => {
                var period = 3.Seconds();

                var t1 = Math.Sin(step.NextTotalElapsedTime.TotalSeconds).Seconds();
                var t2 = Math.Sin(step.NextTotalElapsedTime.TotalSeconds / 2).Seconds();
                var t3 = 0.Seconds();// Math.Sin(step.NextTotalElapsedTime.TotalSeconds / 3).Seconds();

                var client1 = new EndPoint("Client A", skew: 0.Seconds() + t1);
                var server = new EndPoint("Server", skew: 0.Seconds() + t2);
                var client2 = new EndPoint("Client B", skew: 0.Seconds() + t3);

                var graph = new EndPointGraph(
                    new[] { client1, server, client2 }, 
                    new Dictionary<Tuple<EndPoint, EndPoint>, TimeSpan> {
                        {Tuple.Create(client1, server), 0.5.Seconds() + t2 - t1},
                        {Tuple.Create(server, client1), 0.5.Seconds() + t1 - t2},
                        {Tuple.Create(client2, server), 0.5.Seconds() - t3 + t2},
                        {Tuple.Create(server, client2), 0.5.Seconds() + t3 - t2},
                        {Tuple.Create(client2, client1), 0.5.Seconds() + t1 - t3},
                        {Tuple.Create(client1, client2), 0.5.Seconds() + t3 - t1}
                    });
                
                var m1 = new Message("A1", graph, client1, server, client1.Skew + 1.Seconds());
                var m2 = new Message("A2", graph, server, client2, m1.ArrivalTime);
                var m3 = new Message("A3", graph, client2, client1, m2.ArrivalTime);
                
                var m4 = new Message("B1", graph, client2, server, client2.Skew + 4.Seconds());
                var m5 = new Message("B2", graph, server, client1, m4.ArrivalTime);
                var m6 = new Message("B3", graph, client1, client2, m5.ArrivalTime);

                var s1 = new Measurement("Delay (A->S)", m1.Source, m1.Destination, m1.SentTime, m1.ArrivalTime, 60);
                var s3 = new Measurement("Delay (B->A)", m3.Source, m3.Destination, m3.SentTime, m3.ArrivalTime, 20);
                var s2 = new Measurement("Delay (S->B)", m2.Source, m2.Destination, m2.SentTime, m2.ArrivalTime, 340);
                
                var s5 = new Measurement("Delay (S->A)", m5.Source, m5.Destination, m5.SentTime, m5.ArrivalTime, 60);
                var s6 = new Measurement("Delay (A->B)", m6.Source, m6.Destination, m6.SentTime, m6.ArrivalTime, 340);
                var s4 = new Measurement("Delay (B->S)", m4.Source, m4.Destination, m4.SentTime, m4.ArrivalTime, 380);
                var s7 = new Measurement("Skew (A)", client1, server, client1.Skew, server.Skew, null);
                var s8 = new Measurement("Skew (B)", client2, server, client2.Skew, server.Skew, null);

                return new GraphMessages(graph, new[] {m1, m2, m3,m4,m5,m6}, new[] {s1, s2, s3, s4,s5,s6,s7,s8});
            });
            var state = new Subject<GraphMessages>();
            stateD.Subscribe(state, life);

            // end points
            foreach (var i in 3.Range()) {
                // timeline
                animation.Lines.Add(state.Select(e => new LineSegmentDesc(
                    new Point(e.Graph.GetX(e.Graph.EndPoints[i].Skew), e.Graph.GetY(e.Graph.EndPoints[i])).Sweep(new Vector(1000, 0)),
                    new SolidColorBrush(Colors.Black),
                    3)), life);
                // label
                animation.Labels.Add(state.Select(e => new TextDesc(
                    e.Graph.EndPoints[i].Name,
                    new Point(e.Graph.GetX(e.Graph.EndPoints[i].Skew), e.Graph.GetY(e.Graph.EndPoints[i])),
                    fontWeight: FontWeights.Bold,
                    reference: new Point(1.1, 0.5))), life);
                // tick marks
                foreach (var j in 10.Range()) {
                    animation.Lines.Add(state.Select(e => new LineSegmentDesc(
                        new Point(e.Graph.GetX(e.Graph.EndPoints[i].Skew + j.Seconds()), e.Graph.GetY(e.Graph.EndPoints[i]) - 5).Sweep(new Vector(0, 10)),
                        new SolidColorBrush(Colors.Black),
                        2)), life);
                    // labels
                    animation.Labels.Add(state.Select(e => new TextDesc(
                        j + "s",
                        new Point(e.Graph.GetX(e.Graph.EndPoints[i].Skew + j.Seconds()), e.Graph.GetY(e.Graph.EndPoints[i]) - 5) + new Vector(0, -2),
                        fontSize: 10)), life);
                }
            }
            
            // measurements
            foreach (var i in 8.Range()) {
                var px = from s in state
                         let m = s.Measurements[i]
                         let x1 = s.Graph.GetX(m.X1)
                         let y1 = s.Graph.GetY(m.V1)
                         let x2 = s.Graph.GetX(m.X2)
                         let y2 = s.Graph.GetY(m.V2)
                         let y = m.Y ?? ((y1 + y2)/2)
                         select new {s, m, x1, y1, x2, y2, y};
                animation.Lines.Add(px.Select(e => new LineSegmentDesc(
                    new LineSegment(new Point(e.x1, e.y), new Point(e.x2, e.y)),
                    new SolidColorBrush(Colors.Black),
                    2,
                    true)), life);
                animation.Lines.Add(px.Select(e => new LineSegmentDesc(
                    new LineSegment(new Point(e.x1, e.y1), new Point(e.x1, e.y)),
                    new SolidColorBrush(Colors.Red),
                    1,
                    true)), life);
                animation.Lines.Add(px.Select(e => new LineSegmentDesc(
                    new LineSegment(new Point(e.x2, e.y2), new Point(e.x2, e.y)),
                    new SolidColorBrush(Colors.Red),
                    1,
                    true)), life);
                var off1 = new Vector(5, -5);
                var off2 = new Vector(5, 12);
                animation.Labels.Add(px.Select(e => new TextDesc(
                    e.m.Text,
                    new Point(Math.Max(e.x1, e.x2), e.y) + off1.Rotate(e.m.Angle),
                    direction: e.m.Angle)), life);
                animation.Labels.Add(px.Select(e => new TextDesc(
                    string.Format("{0:0.00}s", (e.m.X2 - e.m.X1).TotalSeconds),
                    new Point(Math.Max(e.x1, e.x2), e.y) + off2.Rotate(e.m.Angle),
                    direction: e.m.Angle)), life);
            }

            // messages
            foreach (var i in 6.Range()) {
                var px = from s in state
                         let m = s.Messages[i]
                         select new {s, m};
                animation.Lines.Add(px.Select(e => new LineSegmentDesc(
                    e.m.Pos,
                    new SolidColorBrush(Colors.Black), 
                    1)), life);

                animation.Points.Add(px.Select(e => new PointDesc(
                    e.m.PosEndPoint,
                    Brushes.Transparent,
                    Brushes.Black,
                    3,
                    0)), life);
                animation.Points.Add(px.Select(e => new PointDesc(
                    e.m.PosSourcePoint,
                    Brushes.Transparent,
                    Brushes.Black,
                    3,
                    0)), life);

                animation.Labels.Add(px.Select(e => new TextDesc(
                    "-> " + e.m.Text + " ->",
                    e.m.Pos.LerpAcross(0.1),
                    fontSize: 10,
                    direction: Dir.FromVector(e.m.Pos.Delta.X, e.m.Pos.Delta.Y),
                    foreground: new SolidColorBrush(Colors.Gray))), life);
            }

            var wasRecording = false;
            var encoder = new GifBitmapEncoder();
            var stepdt = 50.Milliseconds();
            for (var t = 0.Seconds(); t < 500.Seconds(); t += stepdt) {
                if (Recording) {
                    var rtb = new RenderTargetBitmap(
                        (int)Math.Ceiling(Properties.Settings.Default.focusWidth),
                        (int)Math.Ceiling(Properties.Settings.Default.focusHeight), 
                        96, 
                        96, 
                        PixelFormats.Pbgra32);
                    rtb.Render(this);
                    encoder.Frames.Add(BitmapFrame.Create(rtb));
                }
                if (wasRecording && !Recording) {
                    using (var f = new FileStream(Path.Combine(txtPath.Text, DateTime.Now.Ticks + ".gif"), FileMode.CreateNew)) {
                        encoder.Save(f);
                        encoder = new GifBitmapEncoder();
                    }
                }
                wasRecording = Recording;

                var step = new Step(
                    previousTotalElapsedTime: t,
                    timeStep: stepdt);

                foreach (var e in animation.StepActions.CurrentItems())
                    e.Value.Invoke(step);

                await Task.Delay(stepdt);
            }

            //await animation.Run(life, 50.Milliseconds());
        }
    }
    public static class Util {
        public static void AddAll<T>(this PerishableCollection<T> collection, IEnumerable<T> items, Lifetime life) {
            foreach (var e in items) collection.Add(e, life);
        }
        public static TimeSpan DividedBy(this TimeSpan duration, long divisor) {
            return new TimeSpan(duration.Ticks / divisor);
        }
        public static TimeSpan LerpTo(this TimeSpan dt1, TimeSpan dt2, double prop) {
            return dt1.Times(1 - prop) + dt2.Times(prop);
        }
        public static Vector Rotate(this Vector vector, Dir angle) {
            var d = Dir.FromVector(vector.X, vector.Y) + (angle - Dir.AlongPositiveX);
            return new Vector(d.UnitX, d.UnitY) * vector.Length;
        }
    }
}
