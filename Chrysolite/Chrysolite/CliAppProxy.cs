using Chrysolite.Events;
using System.ComponentModel;
using System.Diagnostics;
using System.Timers;
using System;

#nullable disable

namespace Chrysolite
{
    /// <summary>
    /// A disposable internal interface for the underlying Process.
    /// This allows the underlying Process to be disposed of after every execution,
    /// while keeping information about the target application and its settings intact.
    /// </summary>
    public class CliAppProxy
    {
        public CliAppProxy(string applicationPath, int stdoutLatency, int timeout)
        {
            maxOutputLatency = stdoutLatency;
            applicationTimeout = timeout;

            app = new Process();
            app.StartInfo.UseShellExecute = false;
            app.StartInfo.FileName = applicationPath;
            app.StartInfo.RedirectStandardError = true;
            app.StartInfo.RedirectStandardOutput = true;
            app.StartInfo.RedirectStandardInput = true;
            app.StartInfo.CreateNoWindow = true;
            app.EnableRaisingEvents = true;
            app.Exited += App_Exited;

            stdoutChannel = new BackgroundWorker();
            stdoutChannel.WorkerReportsProgress = true;
            stdoutChannel.DoWork += DoWorkOnOutputThread;
            stdoutChannel.ProgressChanged += StdoutChannel_ProgressChanged;

            stderrChannel = new BackgroundWorker();
            stderrChannel.WorkerReportsProgress = true;
            stderrChannel.DoWork += DoWorkOnOutputThread;
            stderrChannel.ProgressChanged += StderrChannel_ProgressChanged;

        }

        /// <summary>
        /// Executes the target application with the given arguments
        /// </summary>
        /// <param name="args">Optional command line arguments</param>
        public void Execute(string args)
        {
            app.StartInfo.Arguments = args;
            if (app.Start())
            {
                // start up the output threads
                stderrChannel.RunWorkerAsync(new OutputThreadContext(app.StandardError, maxOutputLatency));
                stdoutChannel.RunWorkerAsync(new OutputThreadContext(app.StandardOutput, maxOutputLatency));

                // start the inactivity timer
                inactivityTimer = new Timer(applicationTimeout);
                inactivityTimer.AutoReset = false;
                inactivityTimer.Elapsed += InactivityTimer_Elapsed;
                inactivityTimer.Start();
            }
        }

        /// <summary>
        /// Sends input to the target application
        /// </summary>
        /// <param name="input">Input to send</param>
        public void SendInput(string input)
        {
            // if we get input while we are closing, we'll just ignore it
            if (inactivityTimeoutOccurred) { return; }

            app.StandardInput.WriteLine(input);
            ResetInactivityTimer();
        }

        /// <summary>
        /// Immediately stops application execution
        /// </summary>
        public void Kill()
        {
            if (appExited) { return; }

            app.Kill();
            App_Exited(this, new EventArgs());
        }

        private void ResetInactivityTimer()
        {
            if (inactivityTimer == null) { return; }
            try
            {
                inactivityTimer.Stop();
                inactivityTimer.Start();
            }
            catch
            {
                // we can get a NullReferenceException here. If we do, behave as if the timer went off
                InactivityTimer_Elapsed(this, null);
            }
        }

        private void InactivityTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // we assume the app is hung up if this timer goes off, so we close it
            inactivityTimeoutOccurred = true;
            app.Kill();
            App_Exited(this, new EventArgs());
        }

        /// <summary>
        /// The procedure executed separately on both the standard output and standard error thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoWorkOnOutputThread(object sender, DoWorkEventArgs e)
        {
            var context = e.Argument as OutputThreadContext;
            var worker = sender as BackgroundWorker;

            var charBuffer = new char[1];
            var messageBuffer = string.Empty;

            var timer = new Timer(context!.MaxOutputLatency);
            timer.AutoReset = false;
            timer.Elapsed += (source, evt) => {
                if (!string.IsNullOrWhiteSpace(messageBuffer))
                {
                    worker!.ReportProgress(0, messageBuffer);
                }
                messageBuffer = string.Empty;
            };

            // the thread will hang here until a character becomes available to read off of the stream, or the stream is closed
            while (context.Stream.Read(charBuffer, 0, 1) > 0)
            {
                var c = charBuffer[0];
                messageBuffer += c;
                if (timer.Enabled) { timer.Stop(); }

                // reception of a newline means transmission of a new message
                if (c == '\n')
                {
                    if (!string.IsNullOrWhiteSpace(messageBuffer))
                    {
                        worker!.ReportProgress(0, messageBuffer);
                    }
                    messageBuffer = string.Empty;
                }
                else
                {
                    // Start the timer constraining how much time can go by before another character for the current message is received.
                    // If this timer goes off, we'll consider whatever is in the message
                    // buffer to be the full message
                    timer.Start();
                }
            }

            // we're done -- no need for the timer
            if (timer.Enabled) { timer.Stop(); }
            // send the last message
            if (!string.IsNullOrWhiteSpace(messageBuffer))
            {
                worker!.ReportProgress(0, messageBuffer);
            }

            timer.Dispose();
        }
        // invoked when standard out receives a new message from the target app
        private void StdoutChannel_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            StandardMessageReceived?.Invoke(this, new MessageReceivedEventArgs(e.UserState.ToString()));
            ResetInactivityTimer();
        }

        /// <summary>
        /// Handler invoked when standard error stream receives a new message from the target app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StderrChannel_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ErrorMessageReceived?.Invoke(this, new MessageReceivedEventArgs(e.UserState.ToString()));
            ResetInactivityTimer();
        }

        public event MessageReceivedEventHandler StandardMessageReceived;
        public event MessageReceivedEventHandler ErrorMessageReceived;
        public event AppExitedEventHandler AppExited;

        private void App_Exited(object sender, EventArgs e)
        {
            if (appExited) { return; }
            appExited = true;

            // we're not going to raise our app exited event yet
            // we'll delay our event to prevent a race condition where sometimes a message received
            // event happens after the exit event
            exitTimer = new Timer(100);
            exitTimer.AutoReset = false;
            exitTimer.Elapsed += ExitTimer_Elapsed;
            exitTimer.Start();

            if (inactivityTimer != null)
            {
                inactivityTimer.Stop();
                inactivityTimer.Dispose();
                inactivityTimer = null;
            }
        }

        private void ExitTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // raise our event
            AppExited?.Invoke(this, new AppExitedEventArgs(app.ExitCode, inactivityTimeoutOccurred));

            exitTimer.Dispose();

            if (app != null)
            {
                app.Dispose();
                app = null;
            }
        }

        private readonly int applicationTimeout;
        private readonly int maxOutputLatency;
        private Process app;
        private readonly BackgroundWorker stdoutChannel;
        private readonly BackgroundWorker stderrChannel;
        private Timer inactivityTimer;
        private Timer exitTimer;
        private bool inactivityTimeoutOccurred = false;
        private bool appExited = false;
    }
}