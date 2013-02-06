using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TwistedOak.Element.Env;
using TwistedOak.Util;

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
                    this.ResizeMode = ResizeMode.NoResize;
                    btnRecord.Content = "Stop Recording";
                    Properties.Settings.Default.focusHeight = Math.Max(50, this.ActualWidth);
                    Properties.Settings.Default.focusWidth = Math.Max(50, this.ActualHeight);
                    Properties.Settings.Default.Save();
                    Recording = true;
                } else {
                    Recording = false;
                    this.ResizeMode = ResizeMode.CanResize;
                    btnRecord.Content = "Start Recording";
                    txtPath.IsEnabled = true;
                }
            };
            this.Loaded += (sender, arg) => {
                var life = Lifetime.Immortal;
                var animation = Animations.MovingCircleIntersectsLine.Animate(life);
                animation.LinkToCanvas(canvas, life);
                RunWithPotentialRecording(animation);
                this.Width = Math.Max(50, Properties.Settings.Default.focusWidth);
                this.Height = Math.Max(50, Properties.Settings.Default.focusHeight);
            };
        }

        private async Task RunWithPotentialRecording(Animation animation, TimeSpan? frameTime= null) {
            var wasRecording = false;
            var encoder = new GifBitmapEncoder();
            var stepdt = frameTime ?? 50.Milliseconds();
            for (var t = 0.Seconds(); t < 500.Seconds(); t += stepdt) {
                if (Recording) {
                    var rtb = new RenderTargetBitmap(
                        (int)Math.Floor(canvas.ActualWidth),
                        (int)Math.Floor(canvas.ActualHeight),
                        96,
                        96,
                        PixelFormats.Pbgra32);
                    rtb.Render(canvas);
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
        }
    }
}
