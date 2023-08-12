using System;
using System.Threading;

namespace Chrysolite
{
    /// <summary>
    /// Provides static methods to manage an event-driven application's main thread, particularly when
    /// the application's flow relies on events emitted by <code>AppInterface</code> instance(s)
    /// </summary>
    public static class ObserverApplication
    {
        private static readonly ManualResetEvent _manualResetEvent = new ManualResetEvent(false);
        private static readonly ApplicationObserverState _state = new ApplicationObserverState();

        /// <summary>
        /// Hangs the main application thread while awaiting and handling events
        /// </summary>
        public static void Start()
        {
            lock (_state)
            {
                if (_state.CalledStart)
                {
                    throw new InvalidOperationException($"Method {nameof(Start)} may only be invoked once");
                }

                _state.CalledStart = true;
            }

            _manualResetEvent.WaitOne();
        }

        /// <summary>
        /// Releases the main application thread, allowing it to continue from 
        /// </summary>
        public static void Stop()
        {
            lock(_state)
            {
                if (_state.CalledStart)
                {
                    if (_state.CalledStop)
                    {
                        throw new InvalidOperationException($"Method {nameof(Stop)} may only be invoked once");
                    }

                    _manualResetEvent.Set();
                    _state.CalledStop = true;
                }
            }
        }
    }

    internal class ApplicationObserverState
    {
        public bool CalledStart { get; set; }
        public bool CalledStop { get; set; }
    }
}
