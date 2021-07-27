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
        /// Initialising the environment
        /// </summary>
        private void InitEnv()
        {
            // Determining the instruction set
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

            // Configuring ffms environment variables
            var ffms2Dir = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "ffms2");
            Environment.SetEnvironmentVariable("PATH", Path.Combine(ffms2Dir, instructionSet) + ";" + Environment.GetEnvironmentVariable("PATH"));
        }

        VideoController videoController;

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Creating a video controller
            videoController = new VideoController(this.Viewer);

            // Playback time update processing method
            videoController.TimeUpdatedHander = (double time) =>
            {
                // If playback reaches the end, set the playback time to 0
                if (time >= videoController.VideoSource.LastTime)
                {
                    videoController.PlaybackTime = 0;
                }
            };

            // Load video
            string videoPath = "test.mp4";

            Task.Factory.StartNew(() =>
            {
                videoController.Init(videoPath,
                   (object sender1, double e1) =>
                   {
                       // Load progress updated handling functions
                       Console.WriteLine($"Loading: {e1 * 100:0.00} %");
                   },
                   (object sender1, VideoController.LoadingState e1) =>
                   {
                       // Loading status updated handling method
                       switch (e1)
                       {
                           case VideoController.LoadingState.IndexingFrames:
                               Console.WriteLine("Building index of video frames...");
                               break;
                       }
                   });

                // Play the video after loading
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
