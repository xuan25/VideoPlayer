using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace VideoPlayer.Clocking
{
    public sealed class MmTimer : IComponent
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct MmTimerCaps
        {
            public int periodMin;
            public int periodMax;
        }

        public enum MmTimerMode
        {
            OneShot,
            Periodic
        }

        #region Fields
        //
        private static MmTimerCaps caps;
        //
        private int timerID;
        private bool isRunning;
        private int interval;
        private int resolution;
        private MmTimerMode mode;
        //
        private TimeProc timeProcOneShot;
        private TimeProc timeProcPeriodic;
        /// <summary>
        /// Occurs when the timer interval has elapsed.
        /// </summary>
        public event EventHandler Tick;
        /// <summary>
        /// Occurs when the timer is disposed.
        /// </summary>
        public event EventHandler Disposed;
        //
        #endregion

        #region Properties
        //
        public ISite Site { get; set; }
        /// <summary>
        /// Gets whether the timer is running.
        /// </summary>
        public bool IsRunning 
        {
            get
            {
                return isRunning;
            }
        }
        /// <summary>
        /// Gets or Sets the timer mode.
        /// </summary>
        public MmTimerMode Mode
        {
            get
            {
                return mode;
            }
            set
            {
                this.mode = value;
            }
        }
        /// <summary>
        /// Gets or Sets the timer interval.
        /// <para>Exceptions:</para>
        /// <para>Exception("invalid period")</para>
        /// </summary>
        public int Interval
        {
            get
            {
                return this.interval;
            }
            set
            {
                if (value < caps.periodMin || value > caps.periodMax)
                {
                    throw new Exception("invalid period");
                }
                this.interval = value;
            }
        }
        //
        #endregion

        #region Methods
        //
        [DllImport("winmm.dll")]
        private static extern int timeGetDevCaps(ref MmTimerCaps caps, int sizeOfTimerCaps);
        [DllImport("winmm.dll")]
        private static extern int timeKillEvent(int id);
        [DllImport("winmm.dll")]
        private static extern int timeSetEvent(int delay, int resolution, TimeProc proc, int user, int mode);
        //
        static MmTimer()
        {
            timeGetDevCaps(ref caps, Marshal.SizeOf(caps));
        }
        //
        public MmTimer()
        {
            this.interval = caps.periodMin;
            this.resolution = caps.periodMin;
            this.mode = MmTimerMode.Periodic;
            this.isRunning = false;
            this.timeProcPeriodic = new TimeProc(this.TimerPeriodicEventCallback);
            this.timeProcOneShot = new TimeProc(this.TimerOneShotEventCallback);
        }
        //
        public MmTimer(IContainer container):this()
        {
            container.Add(this);
        }
        //
        ~MmTimer()
        {
            timeKillEvent(this.timerID);
        }
        /// <summary>
        /// Releases all resoures by the timer.
        /// </summary>
        public void Dispose()
        {
            timeKillEvent(this.timerID);
            GC.SuppressFinalize(this); // 通知析构函数不执行
            EventHandler disposed = this.Disposed;
            if (disposed != null)
            {
                disposed(this, EventArgs.Empty);
            }
        }
        //
        private void TimerPeriodicEventCallback(int id, int msg, int user, int param1, int param2)
        {
            this.OnTick(EventArgs.Empty);
        }
        //
        private void TimerOneShotEventCallback(int id, int msg, int user, int param1, int param2)
        {
             this.OnTick(EventArgs.Empty);
             this.Stop();
        }
        /// <summary>
        /// Starts the timer.
        /// <para>Exceptions:</para>
        /// <para>Exception("Unable to start MmTimer")</para>
        /// </summary>
        public void Start()
        {
            if (this.isRunning == false)
            {
                if (this.Mode == MmTimerMode.Periodic)
                {
                    this.timerID = timeSetEvent(this.interval, this.resolution, this.timeProcPeriodic, 0, (int)this.Mode);
                }
                else
                {
                    this.timerID = timeSetEvent(this.interval, this.resolution, this.timeProcOneShot, 0, (int)this.Mode);
                }
                if (this.timerID == 0)
                {
                    throw new Exception("Unable to start MmTimer");
                }
                this.isRunning = true;
            }
        }
        /// <summary>
        /// Stops the timer.
        /// </summary>
        public void Stop()
        {
            if (this.isRunning)
            {
                timeKillEvent(this.timerID);
                this.isRunning = false;
            }
        }
        //
        private void OnTick(EventArgs e)
        {
            EventHandler tick = this.Tick;
            if (tick != null)
            {
                tick(this, e);
            }
        }
        //
        #endregion

        #region Delegate
        //
        private delegate void TimeProc(int id, int msg, int user, int param1, int param2);
        //
        #endregion
    }
}
