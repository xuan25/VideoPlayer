using FFMSSharp;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace VideoPlayer
{
    /// <summary>
    /// Interaction logic for VideoViewer.xaml
    /// </summary>
    public partial class VideoViewer : UserControl
    {
        /// <summary>
        /// Number of Rendering threads
        /// </summary>
        public int RenderThreadNumber { get; set; }

        /// <summary>
        /// VideoSource
        /// </summary>
        public VideoSource Source { get; private set; }

        /// <summary>
        /// Playback timestamp (in milliseconds)
        /// </summary>
        public long TimestampMs { get; set; }


        private WriteableBitmap RenderingBitmap { get; set; }


        private readonly object timeLock = new object();
        private long timeBaseNum;
        private long timeBaseDen;
        private double lastEndTime;
        int renderedFrameNum = -1;

        long[] pts_index;       // TODO: Use a balanced tree instead of a list to improve overall performance (lan de xie le)


        public VideoViewer()
        {
            InitializeComponent();

            System.Threading.ThreadPool.GetMinThreads(out int workerThreads, out int completionPortThreads);
            RenderThreadNumber = (int)(workerThreads * 1.5);
        }

        public void Init(VideoSource source, EventHandler<double> progressUpdatedHandler)
        {
            Source = source;

            InitIndex(progressUpdatedHandler);

            InitWriteableBitmap();
        }

        public void SetTimeMs(long ms)
        {
            int currFrameNum = GetFrameNumFromTime(ms);
            if (currFrameNum == renderedFrameNum)
            {
                return;
            }
            if (currFrameNum < 0)
            {
                currFrameNum = 0;
            }

            DrawFrame(currFrameNum);

            renderedFrameNum = currFrameNum;
        }

        private int GetFrameNumFromTime(long ms)
        {
            if (ms > lastEndTime)
            {
                return -1;
            }
            long currentPts = ms * timeBaseDen / timeBaseNum;
            for (int i = 0; i < pts_index.Length; i++)
            {
                long framePts = pts_index[i];
                if (framePts > currentPts)
                {
                    return i - 1;
                }
            }
            return pts_index.Length - 1;
        }

        internal int GetFrameNum()
        {
            if (TimestampMs > lastEndTime)
            {
                return -1;
            }
            long currentPts = TimestampMs * timeBaseDen / timeBaseNum;
            for (int i = 0; i < pts_index.Length; i++)
            {
                long framePts = pts_index[i];
                if (framePts > currentPts)
                {
                    return i - 1;
                }
            }
            return pts_index.Length - 1;
        }

        internal void SetFrameNum(int frameNum)
        {
            lock (timeLock)
            {
                long setPts = pts_index[frameNum];
                TimestampMs = setPts * timeBaseNum / timeBaseDen;
            }
        }

        private void InitIndex(EventHandler<double> progressUpdatedHandler)
        {
            int progressUpdatedMod = 1024;

            int numberOfFrames = Source.NumberOfFrames;
            Track videosourceTrack = Source.Track;
            timeBaseNum = videosourceTrack.TimeBaseNumerator;
            timeBaseDen = videosourceTrack.TimeBaseDenominator;
            lastEndTime = Source.LastEndTime * 1000;

            pts_index = new long[numberOfFrames];

            for (int i = 0; i < numberOfFrames; i++)
            {
                FrameInfo frameInfo = videosourceTrack.GetFrameInfo(i);
                pts_index[i] = frameInfo.PTS;
                if (i % progressUpdatedMod == 0)
                {
                    progressUpdatedHandler?.Invoke(this, (double)i / numberOfFrames);
                }
            }
        }

        private void InitWriteableBitmap()
        {
            int framenumber = 0;
            FFMSSharp.Frame curframe = Source.GetFrame(framenumber); // Valid until next call to GetFrame on the same video object

            System.Drawing.Size resolution = curframe.Resolution;

            Dispatcher.Invoke(() =>
            {
                RenderingBitmap = new WriteableBitmap(resolution.Width, resolution.Height, 96, 96, PixelFormats.Bgra32, null);
                VideoImage.Source = RenderingBitmap;
            });
        }

        private void DrawFrame(int frameNum)
        {
            try
            {
                FFMSSharp.Frame curframe = Source.GetFrame(frameNum);  // Valid until next call to GetFrame on the same video object
                System.Drawing.Size resolution = curframe.Resolution;
                IntPtr sourceIntPtr = curframe.Data[0];

                IntPtr targetIntPtr = IntPtr.Zero;
                Dispatcher.Invoke(() =>
                {
                    RenderingBitmap.Lock();
                    targetIntPtr = RenderingBitmap.BackBuffer;
                }, System.Windows.Threading.DispatcherPriority.Render);

                int sourceWidth = resolution.Width;
                int sourceHeight = resolution.Height;
                int sourceStride = sourceWidth * 4;

                int targetWidth = sourceWidth;
                int targetHeight = sourceHeight;
                int targetStride = targetWidth * 4;

                int taskCount = RenderThreadNumber;
                int taskHeight = (int)Math.Ceiling((float)targetHeight / taskCount);
                Parallel.For(0, taskCount, (t) =>
                {
                    int startY = t * taskHeight;
                    int endY = (t + 1) * taskHeight;
                    if (endY > sourceHeight)
                    {
                        endY = sourceHeight;
                    }

                    for (int y = startY; y < endY; y++)
                    {
                        for (int x = 0; x < targetWidth; x++)
                        {
                            //GetPixel(sourceIntPtr, sourceStride, x, y, out byte a, out byte r, out byte g, out byte b);
                            //SetPixel(targetIntPtr, targetStride, x, y, a, r, g, b);

                            int color = GetPixel(sourceIntPtr, sourceStride, x, y);
                            SetPixel(targetIntPtr, targetStride, x, y, color);
                        }
                    }
                    
                });

                Dispatcher.Invoke(() =>
                {
                    RenderingBitmap.AddDirtyRect(new Int32Rect(0, 0, RenderingBitmap.PixelWidth, RenderingBitmap.PixelHeight));
                    RenderingBitmap.Unlock();
                }, System.Windows.Threading.DispatcherPriority.Render);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        internal static int GetPixel(IntPtr pBackBuffer, int stride, int x, int y)
        {
            unsafe
            {
                IntPtr pPixel = pBackBuffer + y * stride + x * 4;

                return *((int*)pPixel);
            }
        }

        internal static void GetPixel(IntPtr pBackBuffer, int stride, int x, int y, out byte a, out byte r, out byte g, out byte b)
        {
            unsafe
            {
                IntPtr pPixel = pBackBuffer + y * stride + x * 4;

                int color = *((int*)pPixel);

                a = (byte)((color >> 24) & 0xFF);
                r = (byte)((color >> 16) & 0xFF);
                g = (byte)((color >> 8) & 0xFF);
                b = (byte)(color & 0xFF);
            }
        }

        internal static void SetPixel(IntPtr pBackBuffer, int stride, int x, int y, byte a, byte r, byte g, byte b)
        {
            unsafe
            {
                IntPtr pPixel = pBackBuffer + y * stride + x * 4;

                // Compute the pixel's color.
                int color_data = a << 24; // A
                color_data |= r << 16; // R
                color_data |= g << 8;   // G
                color_data |= b << 0;   // B

                // Assign the color data to the pixel.
                *((int*)pPixel) = color_data;
            }
        }

        internal static void SetPixel(IntPtr pBackBuffer, int stride, int x, int y, int color)
        {
            unsafe
            {
                IntPtr pPixel = pBackBuffer + y * stride + x * 4;

                // Assign the color data to the pixel.
                *((int*)pPixel) = color;
            }
        }

    }
}
