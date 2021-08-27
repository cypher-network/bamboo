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
        private double _interval = Double.NaN;
        private bool _autoreset = false;
        private object _lock = new object();
        public event ElapsedEventHandler Elapsed;

        private void OnTimeout(object source, System.Timers.ElapsedEventArgs e)
        {
            lock (_lock)
            {
                Elapsed?.Invoke(source, e);
                if (_timer.AutoReset)
                {
                    Restart();
                }
            }
        }

        public PausableTimer(double interval, bool autoreset = false)
        {
            _stopWatch = new Stopwatch();

            _timer = new Timer(interval);
            _timer.AutoReset = autoreset;
            _timer.Elapsed += OnTimeout;
            _interval = interval;
            _autoreset = autoreset;
        }

        public void Start()
        {
            lock (_lock)
            {
                _timer.Start();
                _stopWatch.Restart();
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                _timer.Stop();
                _stopWatch.Stop();
            }
        }

        public void Pause()
        {
            lock (_lock)
            {
                if (!_paused && _timer.Enabled)
                {
                    _paused = true;
                    _stopWatch.Stop();
                    _timer.Stop();
                    _timer.Elapsed -= OnTimeout;
                    _remainingTimeBeforePause = Math.Max(0, _timer.Interval - _stopWatch.ElapsedMilliseconds);
                }
            }
        }

        public void Restart()
        {
            _stopWatch = new Stopwatch();
            _timer.Stop();
            _timer.Elapsed -= OnTimeout;
            _timer = new Timer(_interval);
            _timer.Elapsed += OnTimeout;
            _timer.AutoReset = _autoreset;
            _timer.Start();
            _stopWatch.Start();
        }

        public void Resume()
        {
            lock (_lock)
            {
                if (_paused)
                {
                    _paused = false;
                    if (_remainingTimeBeforePause > 0)
                    {
                        _timer = new Timer(_remainingTimeBeforePause);
                        _timer.AutoReset = _autoreset;
                        _timer.Elapsed += OnTimeout;
                        _timer.Start();
                        _stopWatch.Start();
                    }
                    else
                    {
                        if (_timer.AutoReset)
                        {
                            _timer = new Timer(_interval);
                            _timer.AutoReset = true;
                            _timer.Elapsed += OnTimeout;
                            _timer.Start();
                            _stopWatch.Restart();
                        }
                    }
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