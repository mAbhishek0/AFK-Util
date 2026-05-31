using System;
using System.IO;
using Microsoft.UI.Xaml;

namespace AfkBot;

public partial class App : Application
{
    public App()
    {
        this.UnhandledException += App_UnhandledException;
        this.InitializeComponent();
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        var logPath = Path.Combine(AppContext.BaseDirectory, "crash.log");
        File.WriteAllText(logPath, $"UnhandledException: {e.Exception}\n{e.Exception.StackTrace}");
        e.Handled = true;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            m_window = new MainWindow();
            m_window.Activate();
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "crash.log");
            File.WriteAllText(logPath, $"OnLaunched Exception: {ex}");
        }
    }

    private Window? m_window;
}
