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
//using System.Windows.Threading;
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

        public string VideoPath { get; set; }

        private Vlc.DotNet.Core.VlcMediaPlayer Player { get { return this.VlcControl.SourceProvider.MediaPlayer; } }

        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var currentAssembly = Assembly.GetEntryAssembly();
            var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
            var libDirectory = new DirectoryInfo(System.IO.Path.Combine(currentDirectory, "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));
            this.VlcControl.SourceProvider.CreatePlayer(libDirectory);

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

        private void Open(object sender, ExecutedRoutedEventArgs e)
        {
            if (Player != null)
            {
                this.tbPath.Visibility = Visibility.Visible;
                this.tbPath.Focus();
            }
        }

        private void TbPath_KeyDown(object sender, KeyEventArgs e)
        {
            if (Player != null && e.Key == Key.Enter)
            {
                string filePath = this.tbPath.Text.Trim('"');
                if (File.Exists(filePath))
                {
                    this.tbPath.Text = "";
                    this.tbPath.Visibility = Visibility.Hidden;
                    Play(filePath);
                }
            }
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            string file = ((System.Array)e.Data.GetData(System.Windows.DataFormats.FileDrop)).GetValue(0).ToString();
            Play(file);
        }

        private void Play(string filePath)
        {
            try
            {
                Player.Play(new Uri(filePath));
                _isPlaying = true;
            }
            catch
            { }
        }

        private void Close(object sender, ExecutedRoutedEventArgs e)
        {
            if (_isPlaying)
            {
                new Task(() =>
                {
                    this.VlcControl.SourceProvider.MediaPlayer.Stop();//这里要开线程处理，不然会阻塞播放
                    _isPlaying = false;

                }).Start();
            }
        }

        #region progress

        private void TogglePause(object sender, ExecutedRoutedEventArgs e)
        {
            if (_isPlaying)
            {
                if (_isPause)
                {
                    Player.Play();
                    HideProgress();
                }
                else
                {
                    Player.Pause();
                    ShowProgress();
                }

                _isPause = !_isPause;
            }
        }

        private void Forward(object sender, ExecutedRoutedEventArgs e)
        {
            if (_isPlaying)
            {
                if (_isPause)
                {
                    Player.Play();
                    _isPause = false;
                    return;
                }

                float p = OFFSET / Player.Length;
                if(Player.Position +p < 0.99)
                    Player.Position += p; // Position为百分比，要小于1，等于1会停止

                ShowProgress();
                HideProgress(2000);
            }
        }

        private void Backward(object sender, ExecutedRoutedEventArgs e)
        {
            if (_isPlaying)
            {
                if (_isPause)
                {
                    Player.Play();
                    _isPause = false;
                    return;
                }

                float p = OFFSET / Player.Length;
                if (Player.Position - p > 0.0)
                    Player.Position -= p;

                ShowProgress();
                HideProgress(2000);
            }
        }

        private void ShowProgress()
        {
            int totalMins = (int)(Player.Length / 1000 / 60);
            int current = (int)(totalMins * Player.Position);
            this.progress.Text = $"{current} / {totalMins}";
            this.progress.Visibility = Visibility.Visible;
        }

        private void HideProgress(int delay = 0)
        {
            if (delay > 100)
            {
                new Thread(() =>
                {
                    Thread.Sleep(delay);
                    this.Dispatcher.BeginInvoke(new Action(() => { this.progress.Visibility = Visibility.Hidden; }));
                }).Start();
            }
            else
                this.progress.Visibility = Visibility.Hidden;
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

        private void ShowVolumn(int duration = 2000)
        {
            this.volumn.Text = Player.Audio.Volume.ToString();
            this.volumn.Visibility = Visibility.Visible;
            new Thread(() =>
            {
                Thread.Sleep(2000);
                this.Dispatcher.BeginInvoke(new Action(() => { this.volumn.Visibility = Visibility.Hidden; }));
            }).Start();
        }

        #endregion
    }
}
