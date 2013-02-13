using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Media;
using Animatron;
using SnipSnap.Mathematics;
using Strilanc.Angle;
using Strilanc.LinqToCollections;
using TwistedOak.Collections;
using TwistedOak.Element.Env;
using TwistedOak.Util;
using LineSegment = SnipSnap.Mathematics.LineSegment;

namespace Animations {
    public static class NetworkSequenceDiagram {
        public static Animation CreateCounterExample1(Lifetime life) {
            var animation = new Animation();

            var state = animation.Dynamic(step => {
                var t = (step.NextTotalElapsedTime.TotalSeconds * 8).SmoothCycle(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1);

                var t1 = TimeSpan.Zero;
                var t2 = t.Seconds();

                var ra = new EndPoint("Robot A", skew: 0.Seconds() + t1);
                var rb = new EndPoint("Robot B", skew: 0.Seconds() + t2);

                var graph = new EndPointGraph(
                    new[] { ra, rb },
                    new Dictionary<Tuple<EndPoint, EndPoint>, TimeSpan> {
                        {Tuple.Create(ra, rb), 2.Seconds() + t2 - t1},
                        {Tuple.Create(rb, ra), 2.Seconds() + t1 - t2},
                    });

                var m1 = new Message("CA0=0s", graph, ra, rb, ra.Skew + 0.Seconds());
                var m2 = new Message("CA1=1s", graph, rb, ra, rb.Skew + 1.Seconds());
                var m3 = new Message("CB0=2s", graph, rb, ra, m1.ArrivalTime);
                var m4 = new Message("CB1=3s", graph, ra, rb, m2.ArrivalTime);

                var s1 = new Measurement("CA0", ra, ra, ra.Skew, m1.SentTime, 60);
                var s2 = new Measurement("CA1", rb, rb, rb.Skew, m2.SentTime, 240);
                var s3 = new Measurement("CB0", rb, rb, rb.Skew, m1.ArrivalTime, 280);
                var s4 = new Measurement("CB1", ra, ra, ra.Skew, m2.ArrivalTime, 20);
                var s5 = new Measurement("RTT/2*(CA1-CA0)/(CB1-CB0)", m2.Source, m1.Destination, m4.ArrivalTime, m4.ArrivalTime + (m1.ArrivalTime - rb.Skew - m1.SentTime + ra.Skew).DividedBy(m2.ArrivalTime - ra.Skew - m2.SentTime + rb.Skew).Seconds().Times(2), 240);
                var s6 = new Measurement("RTT/2*(CB1-CB0)/(CA1-CA0)", m1.Source, m2.Destination, m3.ArrivalTime, m3.ArrivalTime + (m2.ArrivalTime - ra.Skew - m2.SentTime + rb.Skew).DividedBy(m1.ArrivalTime - rb.Skew - m1.SentTime + ra.Skew).Seconds().Times(2), 60);

                return new GraphMessages(graph, new[] { m1, m2, m3, m4 }, new[] { s1, s2, s3, s4, s5, s6 });
            });

            return CreateNetworkAnimation(animation, state, life);
        }
        public static Animation CreateCounterExample2(Lifetime life) {
            var animation = new Animation();

            var state = animation.Dynamic(step => {
                var t = (step.NextTotalElapsedTime.TotalSeconds * 8).SmoothCycle(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1);

                var t1 = TimeSpan.Zero;
                var t2 = t.Seconds();

                var ra = new EndPoint("Robot A", skew: 0.Seconds() + t1);
                var rb = new EndPoint("Robot B", skew: 0.Seconds() + t2);

                var graph = new EndPointGraph(
                    new[] { ra, rb },
                    new Dictionary<Tuple<EndPoint, EndPoint>, TimeSpan> {
                        {Tuple.Create(ra, rb), 2.Seconds() + t2 - t1},
                        {Tuple.Create(rb, ra), 2.Seconds() + t1 - t2},
                    });

                var m1 = new Message("I think it's t=0s.", graph, ra, rb, ra.Skew + 0.Seconds());
                var m2 = new Message("Received at t=2s", graph, rb, ra, m1.ArrivalTime);

                var s1 = new Measurement("Apparent Time Mistake = 2s+2s", ra, ra, m2.ArrivalTime, m2.ArrivalTime + 4.Seconds(), 60);
                var s2 = new Measurement("Time mistake = RTT - 4s", ra, ra, m2.ArrivalTime + 4.Seconds(), m2.ArrivalTime + 4.Seconds(), 140);

                return new GraphMessages(graph, new[] { m1, m2}, new[] { s1, s2});
            });

            return CreateNetworkAnimation(animation, state, life);
        }
        public static Animation CreateTwoPlayerVaryingNetworkAnimation(Lifetime life) {
            var animation = new Animation();

            var state = animation.Dynamic(step => {
                var t = step.NextTotalElapsedTime.TotalSeconds;

                var peerA = new EndPoint("Peer A", skew: 0.Seconds());
                var peerB = new EndPoint("Peer B", skew: 0.Seconds());

                var r = new Random(43257);
                var md = 5.Range().Select(j => (t * 4).SmoothCycle(20.Range().Select(i => r.NextDouble()).ToArray())).ToArray();
                var ix = 0;
                var graph = new EndPointGraph(
                    new[] { peerB, peerA },
                    new Dictionary<Tuple<EndPoint, EndPoint>, Func<TimeSpan>> {
                        {Tuple.Create(peerA, peerB), () => 0.5.Seconds() + 0.4.Seconds().Times(md[ix++])},
                        {Tuple.Create(peerB, peerA), () => 0.5.Seconds()},
                    }.SelectValue(e => e.Value()));

                var messages = 5.Range().SelectMany(i => graph.Delays.Keys.Select(e => new Message("tick", graph, e.Item1, e.Item2, (i).Seconds()))).ToArray();
                var s = new Measurement("Tick Period", peerB, peerB, messages[0].ArrivalTime, messages[0].ArrivalTime + 1.Seconds(), 20);
                var s2 = new Measurement("Jitter", peerB, peerB, messages[0].ArrivalTime + 1.Seconds(), messages[2].ArrivalTime, 60);

                return new GraphMessages(graph, messages, new[] {s, s2});
            });

            return CreateNetworkAnimation(animation, state, life);
        }
        public static Animation CreateWobblyThreePlayerNetworkAnimation(Lifetime life) {
            var animation = new Animation();

            var state = animation.Dynamic(step => {
                var t1 = Math.Sin(step.NextTotalElapsedTime.TotalSeconds).Seconds().DividedBy(3);
                var t2 = Math.Sin(step.NextTotalElapsedTime.TotalSeconds * 3).Seconds().DividedBy(3);
                var t3 = Math.Sin(step.NextTotalElapsedTime.TotalSeconds * 2).Seconds().DividedBy(3);

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
                var m2a = new Message("A2", graph, server, client1, m1.ArrivalTime);
                var m3 = new Message("A3", graph, client2, client1, m2.ArrivalTime);

                var m4 = new Message("B1", graph, client2, server, client2.Skew + 4.Seconds());
                var m5 = new Message("B2", graph, server, client1, m4.ArrivalTime);
                var m5a = new Message("B2", graph, server, client2, m4.ArrivalTime);
                var m6 = new Message("B3", graph, client1, client2, m5.ArrivalTime);

                var measurements = new[] {
                    new Measurement("Delay (A->S)", m1.Source, m1.Destination, m1.SentTime, m1.ArrivalTime, 20),
                    new Measurement("Delay (B->A)", m3.Source, m3.Destination, m3.SentTime, m3.ArrivalTime, 60),
                    new Measurement("Delay (S->B)", m2.Source, m2.Destination, m2.SentTime, m2.ArrivalTime, 340),
                    new Measurement("Delay (S->A)", m5.Source, m5.Destination, m5.SentTime, m5.ArrivalTime, 60),
                    new Measurement("Delay (A->B)", m6.Source, m6.Destination, m6.SentTime, m6.ArrivalTime, 340),
                    new Measurement("Delay (B->S)", m4.Source, m4.Destination, m4.SentTime, m4.ArrivalTime, 380),
                    new Measurement("Skew (A)", client1, server, client1.Skew, server.Skew, null),
                    new Measurement("Skew (B)", client2, server, client2.Skew, server.Skew, null)
                };

                return new GraphMessages(
                    graph,
                    new[] {m1, m2, m3, m4, m5, m6, m2a, m5a},
                    measurements);
            });

            return CreateNetworkAnimation(animation, state, life);
        }
        private static IObservable<Perishable<IObservable<T>>> ForList<T>(this IObservable<IEnumerable<T>> items) {
            return new AnonymousObservable<Perishable<IObservable<T>>>(observer => {
                var list = new List<Tuple<Subject<T>, LifetimeSource>>();
                var d = new DisposableLifetime();
                items.Subscribe(e => {
                    var i = 0;
                    foreach (var x in e) {
                        if (list.Count == i) {
                            var o = Tuple.Create(new Subject<T>(), d.Lifetime.CreateDependentSource());
                            list.Add(o);
                            observer.OnNext(new Perishable<IObservable<T>>(o.Item1, o.Item2.Lifetime));
                        }
                        list[i].Item1.OnNext(x);
                        i += 1;
                    }
                    while (list.Count > i) {
                        list[list.Count - 1].Item2.EndLifetime();
                        list[list.Count - 1].Item1.OnCompleted();
                        list.RemoveAt(list.Count - 1);
                    }
                }, d.Lifetime);
                return d;
            });
        }
        private static Animation CreateNetworkAnimation(Animation animation, IObservable<GraphMessages> stateD, Lifetime life) {
            var state = stateD.Cache(life);

            // end points
            state.Select(e => e.Graph.EndPoints.Select(f => new { s = e, m = f })).ForList().Subscribe(v => {
                // timeline
                animation.Lines.Add(
                    new LineSegmentDesc(
                        v.Value.Select(e => new Point(e.s.Graph.GetX(e.m.Skew), e.s.Graph.GetY(e.m)).Sweep(new Vector(1000, 0))),
                        Brushes.Black.ToSingletonObservable(),
                        3.0.ToSingletonObservable()),
                    v.Lifetime);
                
                // label
                animation.Labels.Add(
                    new TextDesc(
                        text: v.Value.Select(e => e.m.Name),
                        pos: v.Value.Select(e => new Point(e.s.Graph.GetX(e.m.Skew), e.s.Graph.GetY(e.m))),
                        fontWeight: FontWeights.Bold.ToSingletonObservable(),
                        reference: new Point(1.1, 0.5).ToSingletonObservable()),
                    v.Lifetime);
                
                // tick marks
                foreach (var j in 10.Range()) {
                    animation.Lines.Add(
                        new LineSegmentDesc(
                            v.Value.Select(e =>
                                new Point(e.s.Graph.GetX(e.m.Skew + j.Seconds()), e.s.Graph.GetY(e.m) - 5).Sweep(new Vector(0, 10))),
                            Brushes.Black.ToSingletonObservable(),
                            2.0.ToSingletonObservable()),
                        v.Lifetime);

                    // labels
                    animation.Labels.Add(
                        new TextDesc(
                            text: (j + "s").ToSingletonObservable(),
                            pos: v.Value.Select(e => new Point(e.s.Graph.GetX(e.m.Skew + j.Seconds()), e.s.Graph.GetY(e.m) - 5) + new Vector(0, -2)),
                            fontSize: 10.0.ToSingletonObservable()),
                        v.Lifetime);
                }
            }, life);

            // measurements
            state.Select(e => e.Measurements.Select(f => new {s = e, m = f})).ForList().Subscribe(v => {
                var px = (from s in v.Value
                          let m = s.m
                          let x1 = s.s.Graph.GetX(m.X1)
                          let y1 = s.s.Graph.GetY(m.V1)
                          let x2 = s.s.Graph.GetX(m.X2)
                          let y2 = s.s.Graph.GetY(m.V2)
                          let y = m.Y ?? ((y1 + y2)/2)
                          select new {s, m, x1, y1, x2, y2, y}).Cache(life);
                animation.Lines.Add(
                    new LineSegmentDesc(
                        px.Select(e => new LineSegment(new Point(e.x1, e.y), new Point(e.x2, e.y))),
                        Brushes.Black.ToSingletonObservable(),
                        2.0.ToSingletonObservable(),
                        true.ToSingletonObservable()),
                    v.Lifetime);
                animation.Lines.Add(
                    new LineSegmentDesc(
                        px.Select(e => new LineSegment(new Point(e.x1, e.y1), new Point(e.x1, e.y))),
                        Brushes.Red.ToSingletonObservable(),
                        dashed: true.ToSingletonObservable()),
                    v.Lifetime);
                animation.Lines.Add(
                    new LineSegmentDesc(
                        px.Select(e => new LineSegment(new Point(e.x2, e.y2), new Point(e.x2, e.y))),
                        Brushes.Red.ToSingletonObservable(),
                        dashed: true.ToSingletonObservable()),
                    v.Lifetime);
                var off1 = new Vector(5, -5);
                var off2 = new Vector(5, 12);
                animation.Labels.Add(
                    new TextDesc(
                        text: px.Select(e => e.m.Text),
                        pos: px.Select(e => new Point(Math.Max(e.x1, e.x2), e.y) + off1.Rotate(e.m.Angle)),
                        direction: px.Select(e => e.m.Angle)),
                    v.Lifetime);
                animation.Labels.Add(
                    new TextDesc(
                        text: px.Select(e => string.Format("{0:0.00}s", (e.m.X2 - e.m.X1).TotalSeconds)),
                        pos: px.Select(e => new Point(Math.Max(e.x1, e.x2), e.y) + off2.Rotate(e.m.Angle)),
                        direction: px.Select(e => e.m.Angle)),
                    v.Lifetime);
            });

            // messages
            state.Select(e => e.Messages.Select(f => new {s = e, m = f})).ForList().Subscribe(v => {
                animation.Lines.Add(
                    new LineSegmentDesc(
                        v.Value.Select(e => e.m.Pos),
                        Brushes.Black.ToSingletonObservable()),
                    v.Lifetime);

                animation.Points.Add(
                    new PointDesc(
                        v.Value.Select(e => e.m.PosEndPoint),
                        Brushes.Transparent.ToSingletonObservable(),
                        Brushes.Black.ToSingletonObservable(),
                        3.0.ToSingletonObservable(),
                        0.0.ToSingletonObservable()),
                    v.Lifetime);
                animation.Points.Add(
                    new PointDesc(
                        v.Value.Select(e => e.m.PosSourcePoint),
                        Brushes.Transparent.ToSingletonObservable(),
                        Brushes.Black.ToSingletonObservable(),
                        3.0.ToSingletonObservable(),
                        0.0.ToSingletonObservable()),
                    v.Lifetime);

                animation.Labels.Add(
                    new TextDesc(
                        text: v.Value.Select(e => "-> " + e.m.Text + " ->"),
                        pos: v.Value.Select(e => e.m.Pos.LerpAcross(0.1)),
                        fontSize: 10.0.ToSingletonObservable(),
                        direction: v.Value.Select(e => Dir.FromVector(e.m.Pos.Delta.X, e.m.Pos.Delta.Y)),
                        foreground: Brushes.Gray.ToSingletonObservable()),
                    v.Lifetime);
            });

            return animation;
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
            public TimeSpan ArrivalTime { get; private set; }
            public Message(string text, EndPointGraph system, EndPoint source, EndPoint destination, TimeSpan sentTime) {
                Text = text;
                System = system;
                Source = source;
                Destination = destination;
                SentTime = sentTime;
                ArrivalTime = SentTime + System.Delays[Tuple.Create(Source, Destination)];
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
                return t.TotalSeconds * 100 + 100;
            }
            public double GetY(EndPoint endPoint) {
                for (var i = 0; i < EndPoints.Count; i++)
                    if (Equals(EndPoints[i], endPoint)) return i * 100 + 100;
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
    }
}