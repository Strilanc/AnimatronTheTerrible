using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TwistedOak.Element.Env;
using TwistedOak.Util;
using System.Reactive.Linq;

namespace Animatron {
    public partial class MainWindow {
        public MainWindow() {
            var life = Lifetime.Immortal;
            var animation = Animations.NetworkSequenceDiagram.CreateWobblyThreePlayerNetworkAnimation(life);

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
                    txtPath.IsEnabled = false;
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
                    txtPath.IsEnabled = true;
                }
            };

            this.Loaded += (sender, arg) => {
                animation.LinkToCanvas(canvas, life);
                RunWithPotentialRecording(animation, isRecording);
                this.Width = Math.Max(50, Properties.Settings.Default.focusWidth);
                this.Height = Math.Max(50, Properties.Settings.Default.focusHeight);
            };
        }

        private async Task RunWithPotentialRecording(Animation animation, IObservableLatest<bool> recording, TimeSpan? frameTime = null) {
            var stepdt = frameTime ?? 50.Milliseconds();
            
            var encoder = new GifBitmapEncoder();
            recording.SkipWhile(e => !e).DistinctUntilChanged().Where(e => !e).Subscribe(e => {
                using (var f = new FileStream(Path.Combine(txtPath.Text, DateTime.Now.Ticks + ".gif"), FileMode.CreateNew)) {
                    encoder.Save(f);
                    encoder = new GifBitmapEncoder();
                }
            });

            for (var t = 0.Seconds(); t < 500.Seconds(); t += stepdt) {
                if (recording.Current) {
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

                await Task.Delay(stepdt);
            }
        }
    }
}
