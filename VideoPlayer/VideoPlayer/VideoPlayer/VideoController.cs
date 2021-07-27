using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VideoPlayer.Clocking;

namespace VideoPlayer
{
    public class VideoController
    {
        

        /// <summary>
        /// Indicates whether the initialization is complete 
        /// </summary>
        public bool Inited { get; set; }

        /// <summary>
        /// Video source
        /// </summary>
        public FFMSSharp.VideoSource VideoSource { get; private set; }

        /// <summary>
        /// Indicates whether it is playing 
        /// </summary>
        public bool IsPlaying { get; set; }

        /// <summary>
        /// Playback timestamp (in seconds)
        /// </summary>
        public double PlaybackTime
        {
            get
            {
                return (double)Clock.Ticks / 1000;
            }
            set
            {
                Clock.Ticks = value * 1000;
            }
        }

        /// <summary>
        /// Playback speed ratio (default 0)
        /// </summary>
        public double PlaybackSpeed
        {
            get
            {
                return Clock.SpeedRatio;
            }
            set
            {
                Clock.SpeedRatio = value;
            }
        }

        /// <summary>
        /// Hander for timestamp updated
        /// </summary>
        /// <param name="time">Current pkayback time (in seconds)</param>
        public delegate void PlaybackTimeUpdatedHander(double time);

        /// <summary>
        /// Handler for timestamp updated
        /// </summary>
        public PlaybackTimeUpdatedHander TimeUpdatedHander { get; set; }

        private readonly VideoViewer Viewer;
        private readonly PlaybackClock Clock;
        private Thread RenderThread;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="videoViewer">VideoViewer to show the video</param>
        public VideoController(VideoViewer videoViewer)
        {
            IsPlaying = false;
            Inited = false;

            Viewer = videoViewer;

            Clock = new PlaybackClock();
        }

        public enum LoadingState
        {
            /// <summary>
            /// Building frame index
            /// </summary>
            IndexingFrames,
        }

        /// <summary>
        /// Initialize the video player with a file
        /// </summary>
        /// <param name="filename">The path of the video file to be played </param>
        /// <param name="progressUpdatedHandler">Handler for loading progress update</param>
        /// <param name="statusUpdatedHandler">Handler for loading state update</param>
        public void Init(string filename, EventHandler<double> progressUpdatedHandler, EventHandler<LoadingState> statusUpdatedHandler)
        {
            lock (this)
            {
                progressUpdatedHandler?.Invoke(this, 0);
                statusUpdatedHandler?.Invoke(this, LoadingState.IndexingFrames);

                // Video
                FFMSSharp.FFMS2.Initialize();

                FFMSSharp.Indexer indexer = new FFMSSharp.Indexer(filename);
                FFMSSharp.Index index = indexer.Index();
                int firstVideoTrackNo = index.GetFirstTrackOfType(FFMSSharp.TrackType.Video);
                FFMSSharp.VideoSource videosource = index.VideoSource(filename, firstVideoTrackNo, threads: 0, seekMode: FFMSSharp.SeekMode.Aggressive);
                VideoSource = videosource;

                FFMSSharp.Frame propframe = videosource.GetFrame(0);
                List<int> pixfmts = new List<int>();
                pixfmts.Add(FFMSSharp.FFMS2.GetPixelFormat("bgra"));
                videosource.SetOutputFormat(pixfmts, propframe.EncodedResolution.Width, propframe.EncodedResolution.Height, FFMSSharp.Resizer.Bicubic);

                Viewer.Init(videosource, (object sender1, double e1) =>
                {
                    progressUpdatedHandler?.Invoke(this, e1);
                });
                progressUpdatedHandler?.Invoke(this, 1);

                // Playback
                if (RenderThread != null)
                {
                    RenderThread.Abort();
                }
                Thread thread = new Thread(RenderLoop)
                {
                    IsBackground = true,
                    Name = "RenderThread"
                };
                RenderThread = thread;
                thread.Start();

                Inited = true;
            }
        }

        /// <summary>
        /// Play
        /// </summary>
        public void Play()
        {
            lock (this)
            {
                if (!Inited)
                {
                    return;
                }
                if (!IsPlaying)
                {
                    IsPlaying = true;
                    Clock.Play();
                }
            }
        }

        /// <summary>
        /// Pause
        /// </summary>
        public void Pause()
        {
            lock (this)
            {
                if (!Inited)
                {
                    return;
                }
                if (IsPlaying)
                {
                    IsPlaying = false;
                    Clock.Pause();
                }
            }
        }

        /// <summary>
        /// Close the video
        /// </summary>
        public void Close()
        {
            lock (this)
            {
                Pause();
                if (RenderThread != null)
                {
                    RenderThread.Abort();
                    RenderThread.Join();
                }
                Viewer.Close();
            }
        }

        private void RenderLoop()
        {
            long lastTick = -1;
            while (true)
            {
                long tick = (long)Clock.Ticks;

                // Render
                if (tick == lastTick)
                {
                    Thread.Sleep(0);
                }
                else
                {
                    TimeUpdatedHander?.Invoke((double)tick / 1000);

                    Viewer.SetTimeMs(tick);
                    lastTick = tick;

                    Thread.Sleep(0);
                }
            }
        }

    }
}
