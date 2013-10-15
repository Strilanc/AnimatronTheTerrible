using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Media;
using Animatron;
using SnipSnap.Mathematics;
using Strilanc.Angle;
using Strilanc.LinqToCollections;
using TwistedOak.Element.Env;

namespace Animations {
    public static class SuperpositionVisualization {
        private static Animation MakeSuperpositionAnimation(Ani<Point> aniCenter, IReadOnlyDictionary<string, Ani<Complex>> vector, Brush[] colors) {
            var tau = 2*Math.PI;
            var aniVector =
                (from keyVal in vector
                 select from component in keyVal.Value
                        select new {
                            label = keyVal.Key,
                            value = new Complex(component.Real, -component.Imaginary),
                            scale = component.Magnitude < 0.1 ? component.Magnitude * 10 : 1
                }).ToArray()
                 .AniAll();
            var aniStartingPhase = from component in aniVector
                                   select (from e in component
                                           where e.value.Magnitude > 0
                                           select -e.scale * e.value / e.value.Magnitude
                                          ).Sum()
                                           .Phase;

            var aniBars =
                from component in aniVector
                from startingPhase in aniStartingPhase
                select component.Select((e, i) => new {
                    Label = e.label,
                    Width = 20*e.scale,
                    Height = e.value.Magnitude * (e.scale < 0.001 ? 0 : (100 / e.scale)),
                    Angle = e.value.Phase.ProperMod(tau),
                    Color = colors[i%colors.Length]
                }).OrderBy(e => (e.Angle - startingPhase).ProperMod(tau)).ToArray();

            var ani = new Animation();
            var c = 0;

            Func<double, Vector> angleToVector = theta => new Vector(Math.Cos(theta), Math.Sin(theta));

            var aniBorderDisplacements = from bars in aniBars
                                         select bars.Stream(
                                             default(Vector),
                                             (acc, bar) => acc + angleToVector(bar.Angle + tau/4)*bar.Width,
                                             streamSeed: true).ToArray();
            var aniAvgDisplacement = from borderDisplacements in aniBorderDisplacements
                                     select borderDisplacements.Average();

            var aniBorderPoints = from center in aniCenter
                                  from avgDisplacement in aniAvgDisplacement
                                  from borderDisplacements in aniBorderDisplacements
                                  select (from borderDisplacement in borderDisplacements
                                          select center + borderDisplacement - avgDisplacement).ToArray();
            foreach (var i in vector.Count.Range()) {
                var aniPt = from borderPoints in aniBorderPoints
                            select borderPoints[i];
                var aniBar = from bars in aniBars
                             select bars[i];
                var dw = from bar in aniBar
                         select angleToVector(bar.Angle + tau/4)*bar.Width;
                var dh = from bar in aniBar
                         select angleToVector(bar.Angle)*bar.Height;
                ani.Add(new PolygonDesc(pos: from pt in aniPt
                                             from w in dw
                                             from h in dh
                                             select new[] {
                                                 pt + w*c,
                                                 pt + w*(1 - c),
                                                 pt + w*(1 - c) + h,
                                                 pt + w*c + h
                                             }.AsEnumerable(),
                                        fill: from bar in aniBar
                                              select bar.Color));
                ani.Add(new TextDesc(text: from bar in aniBar
                                           select bar.Label,
                                     pos: from h in dh
                                          from w in dw
                                          from pt in aniPt
                                          select pt + h + w/2 + h.Normal()*5 - w.Normal()*2,
                                     reference: new Point(0, 0.5),
                                     fontSize: from bar in aniBar
                                               select bar.Width*1.2,
                                     direction: from bar in aniBar
                                                select Dir.FromNaturalAngle(bar.Angle)));
            }
            ani.Add(new PolygonDesc(aniBorderPoints.Select(e => e.AsEnumerable()), Brushes.Gray, 1, 2));
            return ani;
        }

        public static Animation CreateTelepathyAnimation() {
            var i = Complex.ImaginaryOne;
            var beamSplit = ComplexMatrix.FromSquareData(
                1, i,
                i, 1) / Math.Sqrt(2);
            var swap = ComplexMatrix.FromSquareData(
                1, 0, 0, 0,
                0, 0, 1, 0,
                0, 1, 0, 0,
                0, 0, 0, 1);
            var controlledNot2When1 = ComplexMatrix.FromSquareData(
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 0, 1,
                0, 0, 1, 0);
            var H = ComplexMatrix.MakeUnitaryHadamard(1);

            var v = new ComplexVector(new Complex[] { 0.5, 0, 0, 0, 0, 0.5, 0, 0, 0, 0, 0.5, 0, 0, 0, 0, 0.5 });
            var controlledNot1When2 = ComplexMatrix.FromSquareData(
                1, 0, 0, 0,
                0, 0, 0, 1,
                0, 0, 1, 0,
                0, 1, 0, 0);
            var gates = new[] {
                H.TensorProduct(ComplexMatrix.MakeIdentity(2)).TensorProduct(ComplexMatrix.MakeIdentity(4)), //alice bottom 1
                ComplexMatrix.MakeIdentity(4).TensorProduct(controlledNot1When2), // bob left 1
                ComplexMatrix.MakeIdentity(4).TensorProduct(beamSplit.TensorSquare()), // bob left 2
                controlledNot2When1.TensorProduct(ComplexMatrix.MakeIdentity(4)), //alice bottom 2
                ComplexMatrix.MakeIdentity(4).TensorProduct(swap), // bob left 3
                H.TensorProduct(ComplexMatrix.MakeIdentity(2)).TensorProduct(ComplexMatrix.MakeIdentity(4)) //alice bottom 3
            };
            var ackGates = gates.Stream(ComplexMatrix.MakeIdentity(16), (a, m) => m * a, streamSeed: true).ToArray();
            var start = new Point(200, 200);
            return new Animation {
                MakeTransformAnimation(
                    start,
                    v.Values,
                    Ani.Anon(t => {
                        var p = t.TotalSeconds%(ackGates.Length + 4) - 1;
                        var pi = (int)Math.Floor(p);
                        var pd = p - pi;
                        pd = (pd*2 - 1).Clamp(0, 1);

                        var g1 = ackGates[pi.Clamp(0, ackGates.Length - 1)];
                        var g2 = ackGates[(pi + 1).Clamp(0, ackGates.Length - 1)];
                        return (g1*(1 - pd) + g2*pd);
                    }),
                    new[] {
                        "c₁ (win)", "c₂ (win)", "c₃", "c₄", "c₅", "c₆", "c₇ (win)", "c₈ (win)", "c₉", "c₁₀", "c₁₁ (win)", "c₁₂ (win)", "c₁₃ (win)", "c₁₄ (win)",
                        "c₁₅", "c₁₆"
                    }),
                MakeAxies(new Point(350, 295)),
                new TextDesc("Quantum Pseudotelepathy", new Point(400/2, 5), new Point(0.5, 0), fontSize: 20),
                new TextDesc("Applying Alice's and Bob's Gates for Bottom Left Case", new Point(400/2, 30), new Point(0.5, 0)),
                new TextDesc("(makes about as much sense as [insert political joke])", new Point(400/2, 50), new Point(0.5, 0), fontSize: 8),
            };
        }
        private static Animation MakeTransformAnimation(Point center, IEnumerable<Complex> input, Ani<ComplexMatrix> transform, string[] labels = null) {
            var vector = new ComplexVector(input);
            var aniValues = from m in transform
                            select (m * vector).Values;
            var valuesAni = vector.Values.Count.Range().Select(i => from values in aniValues
                                                                    select values[i]);
            var colors = new[] { 0x3366CC, 0xdc3912, 0xff9900, 0x109618, 0x990099, 0x0099c6, 0xdd4477, 0x66aa00,
                                 0xb82e2e, 0x316395, 0x994499, 0x22aa99, 0xaaaa11, 0x6633cc, 0xe67300, 0x8b0707}
                .Select(e => Color.FromRgb((byte)(e >> 16), (byte)(e >> 8), (byte)(e >> 0)))
                .Select(e => new SolidColorBrush(e))
                .Cast<Brush>()
                .ToArray();

            labels = labels ?? new[] { "c₁", "c₂", "c₃", "c₄", "c₅", "c₆", "c₇", "c₈", "c₉", "c₁₀", "c₁₁", "c₁₂", "c₁₃", "c₁₄", "c₁₅", "c₁₆" };
            var labelledAniValues = labels
                .Zip(valuesAni, (label, v) => new KeyValuePair<string, Ani<Complex>>(label, v))
                .ToDictionary(e => e.Key, e => e.Value);
            var ani = new Animation {
                MakeSuperpositionAnimation(
                    center,
                    labelledAniValues,
                    colors)
            };
            return ani;
        }
        private const double CenterX = 281.0/2;
        private static Animation MakeRotationAnimation(Point center, IEnumerable<Complex> input, TimeSpan dt, Point? rotationTextPos = null) {
            var aniTheta = Ani.Anon(t => (t.DividedBy(dt)*Math.PI*2).ProperMod(Math.PI*2));
            var aniMatrix = from theta in aniTheta
                            select ComplexMatrix.FromSquareData(Math.Cos(theta), Math.Sin(theta), -Math.Sin(theta), Math.Cos(theta));
            var ani = new Animation {
                MakeTransformAnimation(center, input, aniMatrix),
            };
            if (rotationTextPos.HasValue) {
                ani.Add(new TextDesc(text: from theta in aniTheta
                                           select string.Format("Current Rotation = {0:000}° =        ", theta*180/Math.PI),
                                     pos: rotationTextPos.Value,
                                     reference: new Point(0.5, 1)));
                ani.Add(QuantumCircuit.ShowComplex(
                   Brushes.Transparent, 
                   Brushes.Black, 
                   from theta in aniTheta select Complex.FromPolarCoordinates(1, theta), 
                   rotationTextPos.Value + new Vector(70, -5),
                   10,
                   sweepFill: Brushes.Yellow,
                   valueGuideStroke: Brushes.Transparent,
                   sweepScale: 1.0));
            }
            return ani;
        }
        private static Animation MakeScalingAnimation(Point center, IEnumerable<Complex> input, Ani<Complex> scale, Point? scalingTextPos = null) {
            var n = input.Count();
            var I = ComplexMatrix.MakeIdentity(n);
            var ani = new Animation {
                MakeTransformAnimation(center,
                                       input,
                                       from s in scale
                                       select I*s)
            };
            if (scalingTextPos.HasValue) {
                ani.Add(new TextDesc(text: from s in scale
                                           select string.Format("Current Scale Factor =        = {0}",
                                                                s.Imaginary == 0
                                                                    ? s.Real.ToString("0.0")
                                                                    : s.ToPrettyString("0.0")),
                                     pos: scalingTextPos));
                ani.Add(new RectDesc(
                    new Rect(scalingTextPos.Value + new Vector(130, -5) + new Vector(-10, -5), new Size(20, 10)),
                    stroke: from s in scale select s.Imaginary != 0 ? (Brush)Brushes.Transparent : Brushes.Black,
                    strokeThickness: 1,
                    dashed: 3));
                ani.Add(new RectDesc(
                    from s in scale select new Rect(scalingTextPos.Value + new Vector(130, -5) + new Vector(s.Real.Min(0)*10, -5), new Size(s.Real.Abs()*10, 10)),
                    fill: from s in scale select s.Imaginary != 0 ? (Brush)Brushes.Transparent : Brushes.Green));
                ani.Add(QuantumCircuit.ShowComplex(
                    Brushes.Transparent,
                    from s in scale select s.Imaginary == 0 ? (Brush)Brushes.Transparent : Brushes.Black,
                    scale,
                    scalingTextPos.Value + new Vector(130, -5),
                    10,
                    sweepFill: from s in scale select s.Imaginary == 0 ? (Brush)Brushes.Transparent : Brushes.Green,
                    valueGuideStroke: from s in scale select s.Imaginary == 0 ? (Brush)Brushes.Transparent : Brushes.Black));
            }
            return ani;
        }
        private static Animation MakeStaticAnimation(Point center, IEnumerable<Complex> input) {
            return MakeScalingAnimation(center, input, Complex.One);
        }
        private static Animation MakeAxies(Point center) {
            var d = 20;
            var dx = new Vector(d, 0);
            var dy = new Vector(0, d);
            var c = d/3.0;
            var s = 0.1;
            return new Animation {
                new LineSegmentDesc((center-dx).Sweep(2*dx), Brushes.Black, 1, 2),
                new LineSegmentDesc((center-dy).Sweep(2*dy), Brushes.Black, 1, 2),

                new LineSegmentDesc((center+dx).Sweep((dy-dx)/c), Brushes.Black, 1),
                new LineSegmentDesc((center+dx).Sweep((-dy-dx)/c), Brushes.Black, 1),
                
                new LineSegmentDesc((center-dx).Sweep((dy+dx)/c), Brushes.Black, 1),
                new LineSegmentDesc((center-dx).Sweep((-dy+dx)/c), Brushes.Black, 1),
                
                new LineSegmentDesc((center+dy).Sweep((dx-dy)/c), Brushes.Black, 1),
                new LineSegmentDesc((center+dy).Sweep((-dx-dy)/c), Brushes.Black, 1),
                
                
                new LineSegmentDesc((center-dy).Sweep((dx+dy)/c), Brushes.Black, 1),
                new LineSegmentDesc((center-dy).Sweep((-dx+dy)/c), Brushes.Black, 1),
                
                new TextDesc("+1", center+dx, new Point(-s, 0.5)),
                new TextDesc("-1", center-dx, new Point(1+s, 0.5)),
                new TextDesc("+i", center-dy, new Point(0.5, 1+s)),
                new TextDesc("-i", center+dy, new Point(0.5, -s)),
            };
        }

        public static Animation CreateStaticExampleAnimation() {
            var i = Complex.ImaginaryOne;
            return new Animation {
                new TextDesc("Representation of <1,i,(i-1)/3>", new Point(CenterX, 5), new Point(0.5, 0), fontSize: 20),
                MakeStaticAnimation(new Point(119, 200), new[] {1, i, (i-1)/3}),
                MakeAxies(new Point(240, 295))
            };
        }
        public static Animation CreateRealScalingAnimation() {
            var i = Complex.ImaginaryOne;
            return new Animation {
                new TextDesc("Scaling <1,i,(i-1)/3>", new Point(CenterX, 5), new Point(0.5, 0), fontSize: 20),
                MakeScalingAnimation(new Point(140, 185), new[] {1, i, (i-1)/3}, Ani.Anon(t => (Complex)Math.Cos(t.TotalSeconds*2*Math.PI / 3)), new Point(70, 45)),
                MakeAxies(new Point(240, 295))
            };
        }
        public static Animation CreateComplexScalingAnimation() {
            var i = Complex.ImaginaryOne;
            return new Animation {
                new TextDesc("Scaling <1,i,(i-1)/3> (Complex)", new Point(CenterX, 5), new Point(0.5, 0), fontSize: 20),
                MakeScalingAnimation(new Point(140, 185), new[] {1, i, (i-1)/3}, Ani.Anon(t => Complex.FromPolarCoordinates(1, t.TotalSeconds*2*Math.PI / 3+0.01)), new Point(40, 45)),
                MakeAxies(new Point(240, 295))
            };
        }
        public static Animation CreateNonScalingAnimation() {
            var i = Complex.ImaginaryOne;
            return new Animation {
                new TextDesc("Transforming <1,i,(1-i)/3>", new Point(CenterX, 5), new Point(0.5, 0), fontSize: 20),
                new TextDesc("(non-scaling operations look confusing)", new Point(CenterX, 30), new Point(0.5, 0)),
                MakeTransformAnimation(new Point(140, 185), new[] {1, i, (1-i)/3}, Ani.Anon(t => {
                    var theta = t.TotalSeconds*2*Math.PI / 3;
                    var s = Math.Sin(theta);
                    var c = Math.Cos(theta);
                    return ComplexMatrix.FromSquareData(c, s, 0, -s, c*c, s, 0, -s, c);
                })),
                MakeAxies(new Point(240, 295))
            };
        }
        public static Animation CreateXRotationAnimation() {
            return new Animation {
                new TextDesc("Rotating <1,0>", new Point(CenterX, 5), new Point(0.5, 0), fontSize: 20),
                MakeRotationAnimation(new Point(140, 185), new Complex[] {1, 0}, 3.Seconds(), new Point(CenterX, 45)),
                MakeAxies(new Point(240, 295))
            };
        }
        public static Animation CreateYRotationAnimation() {
            return new Animation {
                new TextDesc("Rotating <0,1>", new Point(CenterX, 5), new Point(0.5, 0), fontSize: 20),
                MakeRotationAnimation(new Point(140, 185), new Complex[] {0, 1}, 3.Seconds(), new Point(CenterX, 45)),
                MakeAxies(new Point(240, 295))
            };
        }
        public static Animation CreateImagXRotationAnimation() {
            var i = Complex.ImaginaryOne;
            return new Animation {
                new TextDesc("Rotating <i,0>", new Point(CenterX, 5), new Point(0.5, 0), fontSize: 20),
                MakeRotationAnimation(new Point(140, 185), new[] {i, 0}, 3.Seconds(), new Point(CenterX, 45)),
                MakeAxies(new Point(240, 295))
            };
        }
        public static Animation CreateEigenAnimation() {
            var i = Complex.ImaginaryOne;
            return new Animation {
                new TextDesc("Rotating <1,i>", new Point(CenterX, 5), new Point(0.5, 0), fontSize: 20),
                MakeRotationAnimation(new Point(140, 185), new[] {1, i}, 3.Seconds(), new Point(CenterX, 45)),
                MakeAxies(new Point(240, 295))
            };
        }
        public static Animation CreateEigenAnimation2() {
            var i = Complex.ImaginaryOne;
            return new Animation {
                new TextDesc("Rotating <1,-i>", new Point(CenterX, 5), new Point(0.5, 0), fontSize: 20),
                MakeRotationAnimation(new Point(140, 185), new[] {1, -i}, 3.Seconds(), new Point(CenterX, 45)),
                MakeAxies(new Point(240, 295))
            };
        }
        public static Animation CreateAnimation() {
            return CreateTelepathyAnimation();
        }
    }
}