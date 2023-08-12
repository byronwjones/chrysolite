# Chrysolite

An event-based API for automated control of command-line interface (CLI) applications, using the Observer pattern.

## Usage

```C#
string appPath = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe";

AppInterface myPsAppInterface = new AppInterface(appPath, "Powershell", inactivityTimeout: 300000);
int context = 0;

// handle output emitted by the CLI's standard output stream
myPsAppInterface.StandardMessageReceived += (sender, args) => {
    // echo the message emitted by PowerShell to stdout
    Console.WriteLine(args.Message);

    // PowerShell prompts for input follow the pattern:
    // PS C:\some\path> 
    if (Regex.IsMatch(args.Message, @"PS [^>]+>\s*$") == false) { return; }

    // the first time we see the input prompt, change directories
    if (context == 0)
    {
        context++;
        myPsAppInterface.SendInput("cd \"C:\\my directory\"");
        return;
    }

    // the second time we see the input prompt, run script
    if (context == 1)
    {
        context++;
        myPsAppInterface.SendInput(@".\run-some-script.ps1");
        return;
    }

    // third time we see the input prompt, shut down
    if (context == 2)
    {
        myPsAppInterface.Kill();
    }
};

// handle output emitted by the CLI's standard error stream
myPsAppInterface.ErrorMessageReceived += (sender, e) =>
{
    var ai = sender as AppInterface;
    var source = ai!.Description ?? string.Empty;
    Console.ForegroundColor = ConsoleColor.DarkRed;
    Console.WriteLine($"[{source}]: {e.Message}");
    Console.ResetColor();
};

// handle termination of the CLI instance's process
myPsAppInterface.AppExited += (sender, args) =>
{
    WriteSubtitle("We closed PowerShell - all done!");

    // Allow the main thread to continue running from where ObserverApplication.Start() was invoked.
    // In this example, this effectively terminates the application.
    ObserverApplication.Stop();
};

// Hang the application's main thread, allowing it to respond to events
ObserverApplication.Start();
```
