using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
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
        public MainWindow() {
            InitializeComponent();
            this.Loaded += (sender, arg) => Animate(Lifetime.Immortal);
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
                return t.TotalSeconds*100 + 100;
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
            public Measurement(string text, EndPoint v1, EndPoint v2, TimeSpan x1, TimeSpan x2, double? y) {
                Text = text;
                V1 = v1;
                V2 = v2;
                X1 = x1;
                X2 = x2;
                Y = y;
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
                var period = 6.Seconds();
                
                var r = step.NextTotalElapsedTime.DividedBy(period) % 1;
                var r2 = (r*2)%1;
                var t = ((1 - Math.Cos(r2 * Math.PI * 2)) / 4 * (r < 0.5 ? +1 : -1)).Seconds();

                var i = (int)Math.Floor(step.NextTotalElapsedTime.DividedBy(period)) % 1;
                var t1 = i == 0 ? t : 0.Seconds();
                var t2 = i == 2 ? -t : 0.Seconds();
                var t3 = i == 1 ? t : 0.Seconds();

                var client = new EndPoint("client", skew: 0.Seconds() + t1);
                var server = new EndPoint("server", skew: 0.Seconds());

                var graph = new EndPointGraph(
                    new[] { server, client }, 
                    new Dictionary<Tuple<EndPoint, EndPoint>, TimeSpan> {
                        {Tuple.Create(client, server), 0.5.Seconds() + t2},
                        {Tuple.Create(server, client), 0.5.Seconds() + t3}
                    });
                
                var m1 = new Message("Message #1", graph, client, server, client.Skew + 1.Seconds());
                var m2 = new Message("Message #2", graph, server, client, m1.ArrivalTime);
                var m3 = new Message("Message #3", graph, client, server, m2.ArrivalTime);

                var s1 = new Measurement("Delay (Client->Server)", m1.Source, m1.Destination, m1.SentTime, m1.ArrivalTime, 240);
                var s2 = new Measurement("Delay (Server->Client)", m2.Source, m2.Destination, m2.SentTime, m2.ArrivalTime, 60);
                var s3 = new Measurement("Round Trip Time", m1.Source, m2.Destination, m1.SentTime, m2.ArrivalTime, 20);
                var s4 = new Measurement("Clock Skew", client, server, client.Skew, server.Skew, null);

                return new GraphMessages(graph, new[] {m1, m2, m3}, new[] {s1, s2, s3, s4});
            });
            var state = new Subject<GraphMessages>();
            stateD.Subscribe(state, life);

            // end points
            foreach (var i in 2.Range()) {
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
            foreach (var i in 4.Range()) {
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
                animation.Labels.Add(px.Select(e => new TextDesc(
                    e.m.Text,
                    new Point(Math.Max(e.x1, e.x2) + 5, e.y - 2))), life);
                animation.Labels.Add(px.Select(e => new TextDesc(
                    string.Format("{0:0.00}s", (e.m.X2 - e.m.X1).TotalSeconds),
                    new Point(Math.Max(e.x1, e.x2) + 5, e.y + 12))), life);
            }

            // messages
            foreach (var i in 3.Range()) {
                var px = from s in state
                         let m = s.Messages[i]
                         select new {s, m};
                animation.Lines.Add(px.Select(e => new LineSegmentDesc(
                    e.m.Pos,
                    new SolidColorBrush(Colors.Black), 
                    1)), life);
                
                animation.Labels.Add(px.Select(e => new TextDesc(
                    "-> " + e.m.Text + " ->",
                    e.m.Pos.LerpAcross(0.1),
                    fontSize: 10,
                    direction: Dir.FromVector(e.m.Pos.Delta.X, e.m.Pos.Delta.Y),
                    foreground: new SolidColorBrush(Colors.Gray))), life);
            }

            await animation.Run(life);
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
    }
}
