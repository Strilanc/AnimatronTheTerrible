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
                var life = Lifetime.Immortal;
                var animation = Animations.MovingCircleIntersectsLine.Animate(life);
                animation.LinkToCanvas(canvas, life);
                RunWithPotentialRecording(animation);
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

        private async Task RunWithPotentialRecording(Animation animation, TimeSpan? frameTime= null) {
            var wasRecording = false;
            var encoder = new GifBitmapEncoder();
            var stepdt = frameTime ?? 50.Milliseconds();
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
        }
    }
}
