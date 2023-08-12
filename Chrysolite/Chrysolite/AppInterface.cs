using Chrysolite.Events;
using System;

#nullable disable

namespace Chrysolite
{
    /// <summary>
    /// An API for interacting with a CLI application
    /// </summary>
    public class AppInterface
    {
        /// <summary>
        /// Session constructor
        /// </summary>
        /// <param name="applicationPath">Fully qualified application path (e.g. C:\my\app.exe)</param>
        /// <param name="description">A description of the application</param>
        /// <param name="stdoutLatency">The time period, in milliseconds, granted to the application to 
        /// send a subsequent character after it has sent a non-newline character before it is considered
        /// to have printed a full message and to be waiting for input</param>
        /// <param name="inactivityTimeout">The maximum time period, in milliseconds, that the application is allowed to remain dormant
        /// without receiving input or sending output, before it is terminated</param>
        public AppInterface(string applicationPath, string description,
            int stdoutLatency = Defaults.DEFAULT_MAX_OUTPUT_LATENCY_MS,
            int inactivityTimeout = Defaults.DEFAULT_CLIENT_TIMEOUT_MS)
        {
            appPath = applicationPath;
            Description = description;
            outputLatency = stdoutLatency;
            timeout = inactivityTimeout;
        }

        /// <summary>
        /// Executes the application associated with the instance
        /// </summary>
        /// <param name="args">Optional arguments to pass to the application</param>
        public void Execute(string args = null)
        {
            if (Running)
            {
                throw new InvalidOperationException("Application is already running");
            }

            worker = new CliAppProxy(appPath, outputLatency, timeout);
            worker.StandardMessageReceived += Worker_StandardMessageReceived;
            worker.ErrorMessageReceived += Worker_ErrorMessageReceived;
            worker.AppExited += Worker_AppExited;

            worker.Execute(args);
            Running = true;
        }

        /// <summary>
        /// Sends input to the application associated with this instance
        /// </summary>
        /// <param name="input">Input to send to the application</param>
        public void SendInput(string input)
        {
            if (!Running)
            {
                throw new InvalidOperationException("There is no running application to send input to");
            }

            worker.SendInput(input);
        }

        /// <summary>
        /// Immediately stops the running application
        /// </summary>
        public void Kill()
        {
            if (!Running) { return; }

            worker.Kill();
        }

        /// <summary>
        /// Gets a description of the application that this instance interfaces with
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets an indication of whether or not this instance is actively interfacing with its
        /// associated application
        /// </summary>
        public bool Running { get; private set; }

        /// <summary>
        /// Raised when a message is emitted by the managed application's standard output stream
        /// </summary>
        public event MessageReceivedEventHandler StandardMessageReceived;

        /// <summary>
        /// Raised when a message is emitted by the managed application's standard output stream
        /// </summary>
        public event MessageReceivedEventHandler ErrorMessageReceived;

        /// <summary>
        /// Raised the managed application's process is terminated
        /// </summary>
        public event AppExitedEventHandler AppExited;

        // bubble events up
        private void Worker_AppExited(object sender, AppExitedEventArgs e)
        {
            Running = false;
            worker = null;
            AppExited?.Invoke(this, e);
        }
        private void Worker_ErrorMessageReceived(object sender, MessageReceivedEventArgs e) => ErrorMessageReceived?.Invoke(this, e);
        private void Worker_StandardMessageReceived(object sender, MessageReceivedEventArgs e) => StandardMessageReceived?.Invoke(this, e);

        private string appPath;
        private int timeout;
        private int outputLatency;
        private CliAppProxy worker;
    }
}
