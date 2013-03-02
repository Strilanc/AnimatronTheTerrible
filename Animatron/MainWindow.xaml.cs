using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TwistedOak.Element.Env;
using TwistedOak.Util;
using System.Reactive.Linq;
using Strilanc.LinqToCollections;
using System.Linq;

namespace Animatron {
    public partial class MainWindow {
        public MainWindow() {

            //using (var f = File.Open("C:\\Users\\Craig\\Documents\\GifRecordings\\634965148361216632.gif", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)) {
            //    using (var f2 = File.Open("C:\\Users\\Craig\\Documents\\GifRecordings\\LOOP634965148361216632.gif",
            //                           FileMode.OpenOrCreate,
            //                           FileAccess.ReadWrite,
            //                           FileShare.ReadWrite)) {
            //        f.LoopGif(f2);
            //        return;
            //        f.AdjustEncodedGif(50.Milliseconds());
            //    }
            //}
            //return;

            var life = Lifetime.Immortal;
            var animation = Animations.Unitaryness.CreateFullGroverAnimation();

            InitializeComponent();
            var isRecording = new ObservableValue<bool>();
            
            txtPath.Text = string.IsNullOrWhiteSpace(Properties.Settings.Default.path) ? 
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GifRecordings") :
                Properties.Settings.Default.path;
            txtPath.TextChanged += (sender, arg) => {
                Properties.Settings.Default.path = txtPath.Text;
                Properties.Settings.Default.Save();
            };
            btnRecord.Click += (sender, arg) => {
                if (!isRecording.Current) {
                    if (!Directory.Exists(txtPath.Text)) {
                        try {
                            Directory.CreateDirectory(txtPath.Text);
                        } catch {
                            MessageBox.Show("Directory doesn't exist and couldn't be created!");
                            return;
                        }
                    }
                    this.ResizeMode = ResizeMode.NoResize;
                    btnRecord.Content = "Stop Recording";
                    Properties.Settings.Default.focusHeight = Math.Max(50, this.ActualHeight);
                    Properties.Settings.Default.focusWidth = Math.Max(50, this.ActualWidth);
                    Properties.Settings.Default.Save();
                    isRecording.Update(true);
                } else {
                    isRecording.Update(false);
                    this.ResizeMode = ResizeMode.CanResize;
                    btnRecord.Content = "Start Recording";
                }
            };

            this.Loaded += (sender, arg) => {
                animation.LinkToCanvas(canvas, life);
                RunWithPotentialRecording(animation, isRecording, frameTime: 50.Milliseconds(), startTime: 100.Milliseconds(), stopTime: 100.Milliseconds() + 5.Seconds().DividedBy(3));
                this.Width = Math.Max(50, Properties.Settings.Default.focusWidth);
                this.Height = Math.Max(50, Properties.Settings.Default.focusHeight);
            };
        }

        private async Task RunWithPotentialRecording(Animation animation, IObservableLatest<bool> recording, TimeSpan? frameTime = null, TimeSpan? startTime = null, TimeSpan? stopTime = null) {
            var forceRecording = new ObservableValue<bool?>();
            var stepdt = frameTime ?? 50.Milliseconds();

            var encoder = new GifBitmapEncoder();
            recording.CombineLatest(forceRecording, (v1, f) => f ?? v1).SkipWhile(e => !e).DistinctUntilChanged().Subscribe(e => {
                if (!e) {
                    using (var f1 = new MemoryStream()) {
                        using (var f2 = new FileStream(Path.Combine(txtPath.Text, DateTime.Now.Ticks + ".gif"), FileMode.CreateNew)) {
                            encoder.Save(f1);
                            encoder = new GifBitmapEncoder();
                            f1.Flush();
                            f1.Position = 0;
                            f1.LoopGif(f2);
                        }
                    }
                }
                txtPath.IsEnabled = !e;
            });

            for (var t = 0.Seconds(); t < 500.Seconds(); t += stepdt) {
                if (startTime.HasValue && t - stepdt < startTime.Value && t >= startTime.Value) {
                    forceRecording.Update(true);
                }
                if (stopTime.HasValue && t - stepdt < stopTime.Value && t >= stopTime.Value) {
                    forceRecording.Update(null);
                }
                if (forceRecording.Current ?? recording.Current) {
                    var rtb = new RenderTargetBitmap(
                        (int)Math.Floor(canvas.ActualWidth),
                        (int)Math.Floor(canvas.ActualHeight),
                        96,
                        96,
                        PixelFormats.Pbgra32);
                    rtb.Render(canvas);
                    encoder.Frames.Add(BitmapFrame.Create(rtb));
                }

                var step = new Step(
                    previousTotalElapsedTime: t,
                    timeStep: stepdt);

                foreach (var e in animation.StepActions.CurrentItems())
                    e.Value.Invoke(step);

                await Task.Delay(stepdt.Times(10));
            }
        }
    }
    internal static class GifUtil {
        private static void Expect<T>(this T actual, T expected) {
            if (!Equals(actual, expected)) throw new IOException(string.Format("Expected {0}, not {1}.", expected, actual));
        }
        private static int ReadLittleEndianShort(this FileStream f) {
            return f.ReadByte() + (f.ReadByte() << 8);
        }
        private static void WriteLittleEndianShort(this FileStream f, ushort value) {
            f.WriteByte((byte)(value & 0xFF));
            f.WriteByte((byte)((value>>8) & 0xFF));
        }
        public static void LoopGif(this Stream input, FileStream output) {
            // copy header over
            for (var i = 0; i < 13; i++) {
                var b = (byte)input.ReadByte();
                output.WriteByte(b);
            }

            // insert new thing
            var looper = new byte[] {
                0x21, //extension block
                0xFF, //application extension label
                0x0B, //length
                0x4E, 0x45, 0x54, 0x53, 0x43, 0x41, 0x50, 0x45, 0x32, 0x2E, 0x30, //"NETSCAPE2.0"
                // data block
                0x03, //length of data
                0x01, //??
                0x00, 0x00, // repeat count (0=forever)
                // terminator block
                0x00
            };
            output.Write(looper, 0, looper.Length);
            var buffer = new byte[1024];
            while (true) {
                var n = input.Read(buffer, 0, buffer.Length);
                if (n == 0) break;
                output.Write(buffer, 0, n);
            }
        }
        public static void AdjustEncodedGif(this FileStream f, TimeSpan frameDelay) {
            ushort frameDelayValue;
            checked {
                frameDelayValue = (ushort)Math.Round(frameDelay.TotalMilliseconds / 10);
            }
            // header
            f.ReadByte().Expect('G');
            f.ReadByte().Expect('I');
            f.ReadByte().Expect('F');
            f.ReadByte().Expect('8');
            f.ReadByte().Expect('9');
            f.ReadByte().Expect('a');
            var logicalWidth = f.ReadLittleEndianShort();
            var logicalHeight = f.ReadLittleEndianShort();
            var flags0 = f.ReadByte();
            var backgroundColorId = f.ReadByte();
            var pixelAspectRatio = f.ReadByte();

            // graphic control extension
            f.ReadByte().Expect(0x21); // is an extension block
            f.ReadByte().Expect(0xF9); // is graphic control extension
            f.ReadByte().Expect(4); // block size
            var flags = f.ReadByte();
            var frameDelay1 = f.ReadLittleEndianShort();
            var transparentColorId = f.ReadByte();
            var blockTerminator = f.ReadByte();
            //f.WriteLittleEndianShort(frameDelayValue);

            // image descriptor
            f.ReadByte().Expect(0x2C); // is an image descriptor
            var left = f.ReadLittleEndianShort();
            var top = f.ReadLittleEndianShort();
            var width = f.ReadLittleEndianShort();
            var height = f.ReadLittleEndianShort();
            var flags2 = f.ReadByte();
            var hasLocalColorTable = (flags2 & 0x80) != 0;
            var sizeOfLocalColorTable = hasLocalColorTable ? (1 << ((flags2 & 0x07) + 1)) : 0;
            var colorTable = 
                sizeOfLocalColorTable.Range()
                .Select(i => new {r = f.ReadByte(), g = f.ReadByte(), b = f.ReadByte()})
                .ToArray();

            // table based image data
            var lzwMinCodeSize = f.ReadByte();
            var blocks =
                1000.Range()
                    .Select(b => f.ReadByte())
                    .TakeWhile(e => e > 0)
                    .Select(n =>
                            (n - 1).Range()
                                   .Select(i => f.ReadByte())
                                   .ToArray())
                    .ToArray();

        }
    }
}
