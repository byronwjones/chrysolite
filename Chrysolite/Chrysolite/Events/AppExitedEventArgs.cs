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

        public int ExitCode { get; }
        public bool AppTimedOut { get; }
    }
}
