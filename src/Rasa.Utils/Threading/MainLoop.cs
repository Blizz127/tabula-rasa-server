using System;
using System.Diagnostics;
using System.Threading;

namespace Rasa.Threading
{
    public class MainLoop
    {
        public int LoopTime { get; }
        public bool Running { get; private set; }
        public ILoopable Object { get; }
        public Thread LoopThread { get; private set; }

        private static long CurrentMs()
        {
            return (DateTime.UtcNow - new DateTime(1970, 1, 1)).Ticks / TimeSpan.TicksPerMillisecond;
        }

        public MainLoop(ILoopable obj, int loopTime)
        {
            Object = obj;
            LoopTime = loopTime;
        }

        public void Start()
        {
            if (Running)
                throw new Exception("Unable to start a running MainLoop!");

            Running = true;

            LoopThread = new Thread(Loop)
            {
                Priority = ThreadPriority.Highest
            };
            LoopThread.Start();
        }

        public void Stop()
        {
            if (!Running)
                throw new Exception("Unable to stop a not running MainLoop!");

            // No need to join the thread, setting Running to false will eventually stop the thread
            Running = false;
        }

        /// <summary>
        /// How the loop has been doing since the last time anyone asked: how many times it ran,
        /// how long the slowest single pass took, and over how long a stretch.
        /// </summary>
        public struct LoopMetrics
        {
            public int Loops;
            public double PeakMs;
            public double WindowMs;

            public double LoopsPerSecond => WindowMs > 0.0 ? Loops * 1000.0 / WindowMs : 0.0;
        }

        /// <summary>
        /// Measured on the Stopwatch rather than the wall clock. DateTime.UtcNow moves in steps of
        /// about 15 ms on Windows, and a pass that does its work in 3 ms would read as 0 or 15.
        /// </summary>
        private static double StopwatchMs(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

        private readonly object _metricsLock = new object();
        private long _metricsFrom = Stopwatch.GetTimestamp();
        private int _metricsLoops;
        private long _metricsPeakTicks;

        /// <summary>Reads the window and starts a new one.</summary>
        public LoopMetrics TakeMetrics()
        {
            lock (_metricsLock)
            {
                var now = Stopwatch.GetTimestamp();
                var metrics = new LoopMetrics
                {
                    Loops = _metricsLoops,
                    PeakMs = StopwatchMs(_metricsPeakTicks),
                    WindowMs = StopwatchMs(now - _metricsFrom)
                };

                _metricsLoops = 0;
                _metricsPeakTicks = 0;
                _metricsFrom = now;

                return metrics;
            }
        }

        /// <summary>Reads the window without disturbing it, for anyone who just wants a look.</summary>
        public LoopMetrics PeekMetrics()
        {
            lock (_metricsLock)
                return new LoopMetrics
                {
                    Loops = _metricsLoops,
                    PeakMs = StopwatchMs(_metricsPeakTicks),
                    WindowMs = StopwatchMs(Stopwatch.GetTimestamp() - _metricsFrom)
                };
        }

        private void Loop()
        {
            var prevTime = CurrentMs();
            var prevSleepTime = 0;

            while (Running)
            {
                var realTime = CurrentMs();

                var delta = realTime - prevTime;

                var workFrom = Stopwatch.GetTimestamp();

                Object.MainLoop(delta);

                // The pass itself, not the pass plus the sleep that follows it: a loop that does
                // 10 ms of work and sleeps 90 is a healthy 100 ms iteration, and reporting 100
                // would hide the one that does 250 ms of work.
                var workTicks = Stopwatch.GetTimestamp() - workFrom;

                lock (_metricsLock)
                {
                    _metricsLoops++;

                    if (workTicks > _metricsPeakTicks)
                        _metricsPeakTicks = workTicks;
                }

                prevTime = realTime;

                if (delta <= LoopTime + prevSleepTime)
                {
                    prevSleepTime = LoopTime + prevSleepTime - (int)delta;
                    if (prevSleepTime < 10)
                        prevSleepTime = 10;
                }
                else
                    prevSleepTime = 10;

                Thread.Sleep(prevSleepTime);
            }
        }
    }
}
