using System;

namespace Chrysolite.Events
{
    public class MessageReceivedEventArgs : EventArgs
    {
        internal MessageReceivedEventArgs(string message)
        {
            Message = message;
            IsGuaranteedComplete = message.Contains('\n');
        }

        /// <summary>
        /// Message contents received
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets a flag which is true when the message received is for certainty a complete one (i.e. is terminated by a newline character).
        /// </summary>
        /// <remarks>Some applications await user input without emitting a newline character, such that
        /// the user input appears on the same line in the CLI as the app output.  We can guess that this
        /// is the case by postulating that, if there is a lull where no new characters
        /// are emitted after a certain delay, it is because the app is waiting for input.  We
        /// could of course be wrong (i.e. we didn't wait long enough), which is why the completeness of messages received
        /// under such circumstances is not guaranteed</remarks>
        public bool IsGuaranteedComplete { get; }
    }
}
