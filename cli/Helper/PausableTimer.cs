using System;
using System.Timers;
using System.Diagnostics;

namespace Cli.Helper
{
    public class PausableTimer : IDisposable
    {
        private Timer _timer;
        private Stopwatch _stopWatch;
        private bool _paused;
        private double _remainingTimeBeforePause;
        public event ElapsedEventHandler Elapsed;

        public PausableTimer()
        {
            _stopWatch = new Stopwatch();

            _timer = new Timer();
            _timer.AutoReset = false;
            _timer.Elapsed += (sender, arguments) =>
            {
                Elapsed?.Invoke(sender, arguments);

                if (_timer != null && _timer.AutoReset)
                {
                    _stopWatch.Restart();
                }
            };

        }

        public PausableTimer(double interval) : this()
        {
            Interval = interval;
        }

        public bool AutoReset
        {
            get
            {
                return _timer.AutoReset;
            }
            set
            {
                _timer.AutoReset = value;
            }
        }

        public bool Enabled
        {
            get
            {
                return _timer.Enabled;
            }
            set
            {
                _timer.Enabled = value;
            }
        }

        public double Interval
        {
            get
            {
                return _timer.Interval;
            }
            set
            {
                _timer.Interval = value;
            }
        }

        public bool Paused
        {
            get
            {
                return _paused;
            }
        }

        public void Start()
        {
            _timer.Start();
            _stopWatch.Restart();
        }

        public void Stop()
        {
            _timer.Stop();
            _stopWatch.Stop();
        }

        public void Pause()
        {
            if (!_paused && _timer.Enabled)
            {
                _paused = true;
                _stopWatch.Stop();
                _timer.Stop();
                _remainingTimeBeforePause = Math.Max(0, Interval - _stopWatch.ElapsedMilliseconds);
            }
        }

        public void Resume()
        {
            if (_paused)
            {
                _paused = false;
                if (_remainingTimeBeforePause > 0)
                {
                    _timer.Interval = _remainingTimeBeforePause;
                    _timer.Start();
                    _stopWatch.Start();
                }
            }
        }

        bool _disposed = false;

        public void Dispose()
        {
            if (_timer != null && !_disposed)
            {
                // Not thread safe...
                _disposed = true;
                _timer.Dispose();
                _timer = null;
            }
        }

        ~PausableTimer()
        {
            Dispose();
        }
    }
}