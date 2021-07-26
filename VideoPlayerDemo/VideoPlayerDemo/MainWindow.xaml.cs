using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using VideoPlayer;

namespace VideoPlayerDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            InitEnv();

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        /// <summary>
        /// 初始化环境
        /// </summary>
        private void InitEnv()
        {
            // 判断指令集
            string instructionSet;
            switch (IntPtr.Size)
            {
                case 4:
                    instructionSet = "x86";
                    break;
                case 8:
                    instructionSet = "x64";
                    break;
                default:
                    throw new Exception("Unknow CPU instruction set");
            }

            // 配置ffms环境变量
            var ffms2Dir = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "ffms2");
            Application.Current.Resources["ffms2Dir"] = ffms2Dir;
            Environment.SetEnvironmentVariable("PATH", Path.Combine(ffms2Dir, instructionSet) + ";" + Environment.GetEnvironmentVariable("PATH"));
        }

        VideoController videoController;

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 创建视频控制器
            videoController = new VideoController(this.Viewer);

            // 回放时间更新处理函数
            videoController.TimeUpdatedHander = (double time) =>
            {
                // 若播放到结尾，则将回放时间设置为 0
                if (time >= videoController.VideoSource.LastTime)
                {
                    videoController.PlaybackTime = 0;
                }
            };

            // 加载视频
            Task.Factory.StartNew(() =>
            {
                videoController.Init("test.mp4",
                (object sender1, double e1) =>
                {
                    // 加载进度更新处理函数
                    Console.WriteLine($"载入中: {e1 * 100:0.00} %");
                },
                (object sender1, VideoController.LoadingState e1) =>
                {
                    // 加载状态更新处理函数
                    switch (e1)
                    {
                        case VideoController.LoadingState.IndexingFrames:
                            Console.WriteLine("正在建立视频帧索引...");
                            break;
                    }

                });

                // 加载完成后，播放视频
                videoController.Play();
            });
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(videoController != null)
            {
                videoController.Close();
            }
        }
    }
}
