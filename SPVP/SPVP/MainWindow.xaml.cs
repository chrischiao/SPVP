using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;

namespace SPVP
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private const float OFFSET = 30000.0f; // 快进/后退30s 

        private bool _isPause = false;
        private bool _isPlaying = false;
        private System.Timers.Timer _progressTimer = new System.Timers.Timer();
        private System.Timers.Timer _volumnTimer = new System.Timers.Timer();

        public string VideoPath { get; set; }

        private Vlc.DotNet.Core.VlcMediaPlayer Player { get { return this.VlcControl.SourceProvider.MediaPlayer; } }

        public MainWindow()
        {
            InitializeComponent();

            var currentAssembly = Assembly.GetEntryAssembly();
            var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
            var libDirectory = new DirectoryInfo(System.IO.Path.Combine(currentDirectory, "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));
            this.VlcControl.SourceProvider.CreatePlayer(libDirectory);

            _progressTimer.Elapsed += ProgressTimer_Elapsed;
            _volumnTimer.Elapsed += VolumnTimer_Elapsed;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Player != null)
            {
                Player.Audio.Volume = 30;

                if (File.Exists(VideoPath))
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Play(VideoPath);
                    }));
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _volumnTimer.Close();

            if (_isPlaying)
            {
                Stop();
            }
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            string file = ((System.Array)e.Data.GetData(System.Windows.DataFormats.FileDrop)).GetValue(0).ToString();
            Play(file);
        }

        private void ProgressTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(new Action(() => { this.progress.Visibility = Visibility.Hidden; }));
            _progressTimer.Stop();
        }

        private void VolumnTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(new Action(() => { this.volumn.Visibility = Visibility.Hidden; }));
            _volumnTimer.Stop();
        }

        private void Play(string filePath)
        {
            try
            {
                Player.Play(new Uri(filePath));
                _isPlaying = true;
                this.title.Text = System.IO.Path.GetFileName(filePath);
            }
            catch
            { }
        }

        private void Stop()
        {
            if (_isPlaying)
            {
                new Task(() =>
                {
                    Player.Stop();//这里要开线程处理，不然会阻塞播放
                    _isPlaying = false;

                }).Start();
            }
        }

        private void Open(object sender, ExecutedRoutedEventArgs e)
        {
            if (Player != null)
            {
                var ofd = new Microsoft.Win32.OpenFileDialog();
                var result = ofd.ShowDialog();
                if (result.HasValue && result.Value)
                    Play(ofd.FileName);
            }
        }

        private void Stop(object sender, ExecutedRoutedEventArgs e)
        {
            Stop();
        }

        #region progress

        private void TogglePause(object sender, ExecutedRoutedEventArgs e)
        {
            if (_isPlaying)
            {
                if (_isPause)
                {
                    EndPause();
                }
                else
                {
                    Player.Pause();
                    _isPause = true;
                    this.title.Visibility = Visibility.Visible;
                    ShowProgress();
                }
            }
        }

        private void Forward(object sender, ExecutedRoutedEventArgs e)
        {
            if (_isPlaying)
            {
                if (_isPause)
                {
                    EndPause();
                    return;
                }

                float p = OFFSET / Player.Length;
                if(Player.Position +p < 0.99)
                    Player.Position += p; // Position为百分比，要小于1，等于1会停止

                ShowProgress(1500);
            }
        }

        private void Backward(object sender, ExecutedRoutedEventArgs e)
        {
            if (_isPlaying)
            {
                if (_isPause)
                {
                    EndPause();
                    return;
                }

                float p = OFFSET / Player.Length;
                if (Player.Position - p > 0.0)
                    Player.Position -= p;

                ShowProgress(1500);
            }
        }

        private void EndPause()
        {
            this.title.Visibility = Visibility.Hidden;
            this.progress.Visibility = Visibility.Hidden;
            Player.Play();
            _isPause = false;
        }

        private void ShowProgress(int duration = -1)
        {
            string total = TimeSpan.FromSeconds((int)(Player.Length / 1000)).ToString();
            string current = TimeSpan.FromSeconds((int)(Player.Time / 1000)).ToString();
            this.progress.Text = $"{current} / {total}";
            this.progress.Visibility = Visibility.Visible;

            if (duration > 100)
            {
                _progressTimer.Stop();
                _progressTimer.Interval = duration;
                _progressTimer.Start();
            }
        }

        #endregion

        #region Audio Volume

        //Audio.IsMute :静音和非静音
        //Audio.Volume：音量的百分比，值在0—200之间
        //Audio.Tracks：音轨信息，值在0 - 65535之间
        //Audio.Channel：值在1至5整数，指示的音频通道模式使用，值可以是：“1 = 立体声”，“2 = 反向立体声”，“3 = 左”，“4 = 右” “5 = 混音”。 
        //Audio.ToggleMute() : 方法，切换静音和非静音 

        private void VolUp(object sender, ExecutedRoutedEventArgs e)
        {
            if (_isPlaying)
            {
                if (Player.Audio.IsMute)
                    Player.Audio.ToggleMute();
                else if (Player.Audio.Volume < 180)
                    Player.Audio.Volume += 10;

                ShowVolumn();
            }
        }

        private void VolDown(object sender, ExecutedRoutedEventArgs e)
        {
            if (_isPlaying)
            {
                if (Player.Audio.IsMute)
                    Player.Audio.ToggleMute();
                else if (Player.Audio.Volume > 10)
                    Player.Audio.Volume -= 10;

                ShowVolumn();
            }
        }

        private void ToggleMute(object sender, ExecutedRoutedEventArgs e)
        {
            if (_isPlaying)
                Player.Audio.ToggleMute();
        }

        private void ShowVolumn(int duration = 3000)
        {
            if (duration > 100)
            {
                this.volumn.Text = Player.Audio.Volume.ToString();
                this.volumn.Visibility = Visibility.Visible;

                _volumnTimer.Stop();
                _volumnTimer.Interval = duration;
                _volumnTimer.Start();
            }
        }

        #endregion
    }
}
