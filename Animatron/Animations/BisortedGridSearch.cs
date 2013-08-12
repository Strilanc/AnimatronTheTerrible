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
    public static class BisortedGridSearch {
        private static IReadOnlyList<IReadOnlyList<T>> Transpose<T>(this IReadOnlyList<IReadOnlyList<T>> rectangle) {
            if (rectangle.Count == 0) return rectangle;
            return new AnonymousReadOnlyList<IReadOnlyList<T>>(
                () => rectangle[0].Count,
                i => new AnonymousReadOnlyList<T>(
                    () => rectangle.Count,
                    j => rectangle[j][i]));
        } 
        private static IEnumerable<Tuple<int, int, int>> TryFindIndexOfItemInBisortedGrid<T>(this IReadOnlyList<IReadOnlyList<T>> matrix, T item, IComparer<T> comparer = null) {
            if (matrix == null) throw new ArgumentNullException("matrix");
            comparer = comparer ?? Comparer<T>.Default;

            // check size
            var width = matrix.Count;
            if (width == 0) yield break;
            var height = matrix[0].Count;
            if (height < width) {
                foreach (var e in matrix.Transpose().TryFindIndexOfItemInBisortedGrid(item, comparer)) {
                    yield return Tuple.Create(e.Item2, e.Item1, e.Item3);
                }
                yield break;
            }

            // search
            var minCol = 0;
            var maxRow = height - 1;
            var t = height / width;
            while (minCol < width && maxRow >= 0) {
                // query the item in the minimum column, t above the maximum row
                var luckyRow = Math.Max(maxRow - t, 0);
                var cmpItemVsLucky = comparer.Compare(item, matrix[minCol][luckyRow]);
                yield return Tuple.Create(minCol, luckyRow, cmpItemVsLucky);
                if (cmpItemVsLucky == 0) yield break;

                // did we eliminate t rows from the bottom?
                if (cmpItemVsLucky < 0) {
                    maxRow = luckyRow - 1;
                    continue;
                }

                // we eliminated most of the current minimum column
                // spend lg(t) time eliminating rest of column
                var minRowInCol = luckyRow + 1;
                var maxRowInCol = maxRow;
                while (minRowInCol <= maxRowInCol) {
                    var mid = minRowInCol + (maxRowInCol - minRowInCol + 1) / 2;
                    var cmpItemVsMid = comparer.Compare(item, matrix[minCol][mid]);
                    yield return Tuple.Create(minCol, mid, cmpItemVsMid);
                    if (cmpItemVsMid == 0) yield break;
                    if (cmpItemVsMid > 0) {
                        minRowInCol = mid + 1;
                    }
                    else {
                        maxRowInCol = mid - 1;
                        maxRow = mid - 1;
                    }
                }

                minCol += 1;
            }
        }

        private static T[] Rotate<T>(this IEnumerable<T> items, int n) {
            return items.Skip(n).Concat(items.Take(n)).ToArray();
        }
        public static Animation CreateAdversaryStrategyAnimation() {
            var ani = new Animation();

            var rows = 6;
            var dt = 50.Milliseconds();
            var colsPerRowPower = 3;
            var colsPerRow = ((1 << colsPerRowPower) - 1);
            var cols = rows * colsPerRow;
            var b = new Point(5, 5);
            var w = 15;
            var dx = new Vector(w, 0);
            var dy = new Vector(0, w);

            var rng = new Random(235716);
            var d = new Dictionary<int, int>();
            d[-2] = 0;
            d[-1] = 0;
            d[0] = 5;
            d[1] = 1;
            d[2] = 2;
            d[3] = 3;
            d[4] = 4;

            for (var i = 0; i < rows; i++) {
                if (!d.ContainsKey(i)) d[i] = rng.Next(colsPerRow);
                for (var j = 0; j < colsPerRow; j++) {
                    ani.Add(new RectDesc(new Rect(b + dy * (rows-i-1) + dx * (j+i*colsPerRow), new Size(w, w)), fill: Brushes.Yellow.LerpTo(Brushes.Orange, j < d[i] ? 0 : 0.5)));
                }
                for (var j = i * colsPerRow + colsPerRow; j < cols; j++) {
                    ani.Add(new RectDesc(new Rect(b + dy * (rows - i - 1) + dx * j, new Size(w, w)), fill: Brushes.LightGray));
                }
            }
            for (var i = 0; i <= rows; i++) {
                ani.Add(new LineSegmentDesc((b + dy * i).Sweep(dx * cols), thickness: 0.1));
            }
            for (var i = 0; i <= cols; i++) {
                ani.Add(new LineSegmentDesc((b + dx * i).Sweep(dy * rows), thickness: 0.1));
            }

            var order = Enumerable.Range(0, rows).Rotate(rows/2);
            var period = dt.Times((colsPerRowPower*rows+2)*3) + 2.Seconds() + 2.Seconds();
            var pani = ani.Periodic(period);
            var tf = period;

            var t = 0.Seconds();
            t += 2.Seconds();
            for (var i = -2; i < rows; i++) {
                var min = 0;
                var max = colsPerRow - 1;
                for (var j = 0; j < (i < 0 ? 1 : colsPerRowPower); j++) {
                    var mid = (min + max) / 2;
                    var dir = i >= 0 && mid >= d[order[i]];
                    int x;
                    int y;
                    if (i == -2) {
                        x = 3;
                        y = 1;
                        dir = false;
                    } else if (i == -1) {
                        x = 30;
                        y = 4;
                        dir = true;
                    }
                    else {
                        x = mid + order[i] * colsPerRow;
                        y = rows - order[i] - 1;
                    }
                    pani.LimitedSameTime(t, t + dt + dt).Add(new RectDesc(
                        new Rect(b + x * dx + y * dy, new Size(w, w)),
                        stroke: Brushes.Blue,
                        strokeThickness:3));
                    t += dt;
                    if (i == rows - 1 && j == colsPerRowPower - 1) {
                        pani.LimitedSameTime(t, tf).Add(new RectDesc(
                            new Rect(b + x * dx + y * dy-dx*0.5-dy*0.5, new Size(w*2, w*2)),
                            fill: Brushes.Green,
                            stroke: Brushes.Blue,
                            strokeThickness: 3));
                    }
                    else {
                        Rect r;
                        if (dir) {
                            max = mid - 1;
                            r = new Rect(b + dy*y + dx*x, new Size((cols - x)*w, (rows - y)*w));
                        } else {
                            min = mid + 1;
                            r = new Rect(b, new Size(x*w + w, y*w + w));
                        }
                        pani.LimitedSameTime(t, t+dt).Add(new RectDesc(
                                     r,
                                     fill: Brushes.Gray.LerpToTransparent(0.5)));
                        t += dt;
                        pani.LimitedSameTime(t, tf).Add(new RectDesc(
                                     r,
                                     fill: Brushes.Black));
                    }
                    t += dt;

                    if (i < 0) break;
                }

            }

            return ani;
        }
        private static Animation CreateSearchStrategyAnimation(IReadOnlyList<IReadOnlyList<int>> matrix, int item, int w) {
            var ani = new Animation();

            var rows = matrix[0].Count;
            var cols = matrix.Count;
            var dt = 50.Milliseconds();
            var b = new Point(5, 5);
            var dx = new Vector(w, 0);
            var dy = new Vector(0, w);

            for (var r = 0; r < rows; r++) {
                for (var c = 0; c < cols; c++) {
                    if (matrix[c][r] > item) {
                        ani.Add(new RectDesc(new Rect(b + dy * r + dx * c, new Size(w, w)), fill: Brushes.LightGray));
                    } else if (matrix[c][r] == item) {
                        ani.Add(new RectDesc(new Rect(b + dy * r + dx * c, new Size(w, w)), fill: Brushes.Green));
                    }
                }
            }
            for (var i = 0; i <= rows; i++) {
                ani.Add(new LineSegmentDesc((b + dy * i).Sweep(dx * cols), thickness: 0.1));
            }
            for (var i = 0; i <= cols; i++) {
                ani.Add(new LineSegmentDesc((b + dx * i).Sweep(dy * rows), thickness: 0.1));
            }

            var moves = matrix.TryFindIndexOfItemInBisortedGrid(item).ToArray();
            var period = dt.Times(moves.Length * 3) + 2.Seconds() + 2.Seconds();
            var pani = ani.Periodic(period);
            var tf = period;

            var t = 0.Seconds();
            t += 2.Seconds();


            foreach (var move in moves) {
                var x = move.Item1;
                var y = move.Item2;
                pani.LimitedSameTime(t, t + dt + dt).Add(new RectDesc(
                                                             new Rect(b + x * dx + y * dy, new Size(w, w)),
                                                             stroke: Brushes.Blue,
                                                             strokeThickness: 3));
                t += dt;
                if (move.Item3 == 0) {
                    pani.LimitedSameTime(t, tf).Add(new RectDesc(
                                                        new Rect(b + x * dx + y * dy - dx * 0.5 - dy * 0.5, new Size(w * 2, w * 2)),
                                                        fill: Brushes.Green,
                                                        stroke: Brushes.Blue,
                                                        strokeThickness: 3));
                }
                else {
                    var r = move.Item3 < 0
                                ? new Rect(b + dy * y + dx * x, new Size((cols - x) * w, (rows - y) * w))
                                : new Rect(b, new Size(x * w + w, y * w + w));
                    pani.LimitedSameTime(t, t + dt).Add(new RectDesc(
                                                            r,
                                                            fill: Brushes.Gray.LerpToTransparent(0.5)));
                    t += dt;
                    pani.LimitedSameTime(t, tf).Add(new RectDesc(
                                                        r,
                                                        fill: Brushes.Black));
                }
                t += dt;
            }

            return ani;
        }
        public static Animation CreateSearchStrategyAnimation() {
            var height = 20;
            var width = 100;
            var rng = new Random(500);
            var matrix = width.Range().Select(e => new int[height]).ToArray();
            matrix[0][0] = rng.Next(5);
            foreach (var r in height.Range().Skip(1)) {
                matrix[0][r] = matrix[0][r - 1] + rng.Next(15);
            }
            foreach (var c in width.Range().Skip(1)) {
                matrix[c][0] = matrix[c - 1][0] + rng.Next(15);
            }
            foreach (var r in height.Range().Skip(1)) {
                foreach (var c in width.Range().Skip(1)) {
                    matrix[c][r] = Math.Max(matrix[c - 1][r] + rng.Next(25), matrix[c][r - 1] + rng.Next(10));
                }
            }

            var item = 900;
            return CreateSearchStrategyAnimation(matrix, item, 5);
        }
        public static Animation CreateSearchStrategyAnimation2() {
            var height = 40;
            var width = 8;
            var rng = new Random(4060);
            var matrix = width.Range().Select(e => new int[height]).ToArray();
            matrix[0][0] = rng.Next(5);
            foreach (var r in height.Range().Skip(1)) {
                matrix[0][r] = matrix[0][r - 1] + rng.Next(8);
            }
            foreach (var c in width.Range().Skip(1)) {
                matrix[c][0] = matrix[c - 1][0] + rng.Next(8);
            }
            foreach (var r in height.Range().Skip(1)) {
                foreach (var c in width.Range().Skip(1)) {
                    matrix[c][r] = Math.Max(matrix[c - 1][r] + rng.Next(80), matrix[c][r - 1] + rng.Next(8));
                }
            }

            var item = 242;
            return CreateSearchStrategyAnimation(matrix.Transpose(), item, 15);
        }
    }
}