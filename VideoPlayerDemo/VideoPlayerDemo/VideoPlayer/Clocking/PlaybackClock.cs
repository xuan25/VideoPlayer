using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VideoPlayer.Clocking
{
    class PlaybackClock
    {
        private readonly MmTimer Timer;
        public double Ticks { get; set; }

        public double SpeedRatio { get; set; }

        public PlaybackClock()
        {
            SpeedRatio = 1;

            Timer = new MmTimer();
            Timer.Interval = 1;
            Timer.Mode = MmTimer.MmTimerMode.Periodic;
            Timer.Tick += Timer_Tick;
            Ticks = 0;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Ticks += SpeedRatio;
        }

        public void Play()
        {
            Timer.Start();
        }

        public void Pause()
        {
            Timer.Stop();
        }
    }
}
