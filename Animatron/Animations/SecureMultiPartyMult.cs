using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Animatron;
using SnipSnap.Mathematics;
using Strilanc.LinqToCollections;
using TwistedOak.Element.Env;

namespace Animations {
    public static class SecureMultiPartyMult {
        struct Poly {
            public readonly uint Ax2;
            public readonly uint Bx;
            public readonly uint C;
            public Poly(uint ax2, uint bx, uint c) {
                Ax2 = ax2;
                Bx = bx;
                C = c;
            }
        }
        struct SplitShares {
            public readonly uint[] Shares;
            public SplitShares(IEnumerable<uint> shares) {
                Shares = shares.ToArray();
            }
        }
        private static SplitShares SplitSecret(uint value, uint rnd) {
            var v1 = value + rnd;
            var v2 = v1 + rnd;
            var v3 = v2 + rnd;
            return new SplitShares(new[] {
                v1 % 5,
                v2 % 5,
                v3 % 5
            });
        }
        private static Poly CombineSecret(SplitShares c) {
            return new Poly(c.Shares[0], c.Shares[1], c.Shares[2]);
        }

        private static uint[] Split(this Random rng, uint value) {
            var d = (uint)rng.Next(5);
            var v1 = value + d;
            var v2 = v1 + d;
            var v3 = v2 + d;
            return new[] {
                v1 % 5,
                v2 % 5,
                v3 % 5
            };
        }
        private static uint[] Combine(uint c1, uint c2, uint c3) {
            var a = 3*c1 + 4*c2 + 3*c3;
            var b = 0*c1 + 4*c2 + 1*c3;
            var c = 3*c1 + 2*c2 + 1*c3;
            return new[] {a%5, b%5, c%5};
        }

        private static Animation MakePolyAnimation(Ani<Point> center, Ani<uint[]> value) {
            return new Animation {
                new TextDesc(value.Select(e => string.Format("{0}x²+{1}x+{2}",e[0],e[1],e[2])), center, new Point(0.5, 0.5))
            };
        }
        private static Animation MakeCircularValueAnimation(Ani<Point> center, Ani<uint> value) {
            var w = 10;
            return new Animation {
                5.Range().Select(i => new PointDesc(
                    center.Select(e => e + new Vector(Math.Sin(i*3.14159*2.0/5), -Math.Cos(i*3.14159*2.0/5))*w),
                    radius: w/2,
                    fill: value.Select(v => i < v ? (Brush)Brushes.LightGray : Brushes.Transparent),
                    stroke: Brushes.Gray, 
                    strokeThickness: 0.5)),
                5.Range().Select(i => new PointDesc(
                    center.Select(e => e + new Vector(Math.Sin(i*3.14159*2.0/5), -Math.Cos(i*3.14159*2.0/5))*w),
                    radius: 2,
                    fill: value.Select(v => i == v ? (Brush)Brushes.LightGray : Brushes.Transparent))),
                new TextDesc(value.Select(e => ""+e), center, new Point(0.5, 0.5), fontSize: 24, fontStyle: FontStyles.Normal, fontWeight: FontWeights.Bold, foreground: Brushes.White),
                new TextDesc(value.Select(e => ""+e), center, new Point(0.5, 0.5), fontSize: 20, fontStyle: FontStyles.Normal, fontWeight: FontWeights.Bold)
            };
        }
        private static Animation MakeValueAnimation(Ani<Point> center, Ani<uint> value) {
            var w = 10;
            var r = center.Select(e => new Rect(e - new Vector(w, w), new Size(w*2, w*2)));
            return new Animation {
                5.Range().Select(i => new RectDesc(
                    center.Select(e => new Rect(e + new Vector(0, w)*i, new Size(w, w))),
                    fill: value.Select(v => i == v ? (Brush)Brushes.LightGray : Brushes.Transparent),
                    stroke: Brushes.Black, strokeThickness: 1))
            };
        }
        public static Animation CreateStepByStepAnimation(Ani<uint[]> aniInputs, Ani<uint[]> aniDifs, Ani<Point> zero) {
            var b = new Point(25, 25);
            var rng = new Random(40601);

            var progress = from inputs in aniInputs
                           from difs in aniDifs
                           let shares = from input in inputs
                                        select SplitSecret(input, difs[0])
                           let polys = from s in shares
                                       select CombineSecret(s)
                           let distributedShares = from i in 3.Range()
                                                   select shares.Select(s => s.Shares[i])
                           let soloMultipliedShares = from d in distributedShares
                                                      select d.Aggregate((e1, e2) => (e1*e2)%5)
                           let splitShares2 = from i in soloMultipliedShares.Count.Range()
                                              select SplitSecret(soloMultipliedShares[i], difs[i + 1])
                           let redistributedShares = from i in 3.Range()
                                                     select new SplitShares(splitShares2.Select(s => s.Shares[i]))
                           let recombinedShares = from s in redistributedShares
                                                  select CombineSecret(s)
                           let finalShares = new SplitShares(from e in recombinedShares
                                                             select e.C)
                           let result = CombineSecret(finalShares)
                           select new {
                               inputs,
                               polys,
                               shares,
                               distributedShares,
                               soloMultipliedShares,
                               splitShares2,
                               redistributedShares,
                               recombinedShares,
                               finalShares,
                               result
                           };

            var animation = new Animation();
            var per = animation.Periodic(10.Seconds());

            var step1Split = per.LimitedSameTime(0.Seconds(), 2.Seconds());
            per.Add(from i in 3.Range()
                           select new RectDesc(pos: from z in zero 
                                                    select new Rect(z + new Vector(0,i*50), new Size(200, 40)),
                                               stroke: Brushes.Black,
                                               strokeThickness: 1,
                                               fill: Brushes.GreenYellow.LerpToTransparent(0.5)));
            per.Add(from i in 3.Range()
                           select new TextDesc(pos: from z in zero
                                                    select z + new Vector(0+5, i * 50+5),
                                               text: "Player " + (i+1),
                                               reference: new Point(0, 0)));
            per.Add(from i in 2.Range()
                           select new TextDesc(pos: from z in zero
                                                    select z + new Vector(0 + 15, i * 50 + 25),
                                               text: from p in progress select "Input = " + (p.inputs[i] + 1),
                                               reference: new Point(0, 0)));

            var step2Split = per.LimitedSameTime(2.Seconds(), 4.Seconds());
            step2Split.Add(from i in 2.Range()
                           select new TextDesc(pos: from z in zero
                                                    select z + new Vector(0 + 70, i * 50 + 5),
                                               text: from p in progress select string.Format("Poly: {0}x + {1}", p.polys[i].Bx, p.polys[i].C),
                                               reference: new Point(0, 0)));
            step2Split.Add(from i in 2.Range()
                           select new TextDesc(pos: from z in zero
                                                    select z + new Vector(0 + 70, i * 50 + 25),
                                               text: from p in progress select string.Format("Shares: {0}", string.Join(", ", p.shares[i].Shares)),
                                               reference: new Point(0, 0)));


            step1Split.Add(MakeValueAnimation(zero.Select(e => e + new Vector(10, 10)), progress.Select(e => e.inputs[0])));
            step1Split.Add(MakeValueAnimation(zero.Select(e => e + new Vector(10, 20)), progress.Select(e => e.inputs[1])));
            step1Split.Add();

            return animation;
        }
        public static Animation CreateAnimation() {
            var rng = new Random(40601);
            return CreateStepByStepAnimation(new[] {
                (uint)rng.Next(5),
                (uint)rng.Next(5)
            }, new[] {
                (uint)rng.Next(5),
                (uint)rng.Next(5),
                (uint)rng.Next(5),
                (uint)rng.Next(5)
            }, new Point(10, 10));
            //var b = new Point(25, 25);

            //var v1 = (uint)rng.Next(5);
            //var v2 = (uint)rng.Next(5);
            //var s1s = 10.Range().Select(e => rng.Split(v1)).ToArray();
            //var s2s = 10.Range().Select(e => rng.Split(v2)).ToArray();
            //var s1a = Ani.Anon(t => s1s[(int)Math.Floor(t.TotalSeconds)]);
            //var s2a = Ani.Anon(t => s2s[(int)Math.Floor(t.TotalSeconds)]);

            //var c3a =  s1a.Combine(s2a, (s1,s2) =>  s1.Zip(s2, (e1, e2) => (e1 * e2) % 5).ToArray());
            //var cc3a = c3a.Select(c3 => c3.Select(rng.Split).ToArray());
            //var ss3a = cc3a.Select(cc3 => 3.Range().Select(i => Combine(cc3[0][i], cc3[1][i], cc3[2][i])).ToArray());
            //var s3a = ss3a.Select(ss3 => Combine(ss3[0][2], ss3[1][2], ss3[2][2]));

            //var dx = new Vector(20, 0);
            //var dx2 = new Vector(40, 0);
            //var dy = new Vector(0, 30)*3;
            //var dy2 = new Vector(0, 20)*3;
            //var a = new Animation {
            //    MakeValueAnimation(b, v1),
            //    MakeValueAnimation(b + dy, v2),
            //    3.Range().Select(i => new Animation {
            //        MakeValueAnimation(b + dx2+dx*i, s1a.Select(s1 => s1[i])),                    
            //        MakeValueAnimation(b + dx2+dx*i + dy, s2a.Select(s2 => s2[i])),
            //        MakeValueAnimation(b + dx2+dx*i + dy*2, c3a.Select(c3 => c3[i])),
            //        3.Range().Select(j => new Animation {
            //            MakeValueAnimation(b + dx2+dx*i + dy*3 + dy2*j, cc3a.Select(cc3 => cc3[i][j]))
            //        }),
            //        MakeValueAnimation(b + dx2*5+dx*2 + dy*3 + dy2*i, ss3a.Select(ss3 => ss3[i][2])),
            //        MakePolyAnimation(b + dx2*3+dx*2 + dy*3 + dy2*i, ss3a.Select(ss3 => ss3[i])),
            //        MakeValueAnimation(b + dx2*5+dx*2 + dy*4 + dy2*3, s3a.Select(s3 => s3[2]))
            //    })
            //};

            //var r = new Animation();
            //r.Periodic(10.Seconds()).Add(a);
            //return r;
        }
    }
}