using System;

namespace Chrysolite.Events
{
    public class AppExitedEventArgs : EventArgs
    {
        internal AppExitedEventArgs(int exitCode, bool appTimedOut)
        {
            ExitCode = exitCode;
            AppTimedOut = appTimedOut;
        }

        /// <summary>
        /// Gets the exit code returned when the process terminated
        /// </summary>
        public int ExitCode { get; }


        /// <summary>
        /// Gets a flag which is true when the application was forced to exit due to inactivity for a period of time longer
        /// than the configured inactivity period
        /// </summary>
        public bool AppTimedOut { get; }
    }
}
