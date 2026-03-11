using Microsoft.Web.WebView2.Wpf;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Iec61850Sim.Desktop;

public partial class MainWindow : Window
{
    private Process? webProcess;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        StartWebHost();

        await Browser.EnsureCoreWebView2Async();

        Browser.Source = new Uri("http://localhost:8080");
    }

    private void StartWebHost()
    {
        var exePath = Path.Combine(
            AppContext.BaseDirectory,
            "Iec61850Sim.Web.exe");

        webProcess = Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = "--urls=http://localhost:8080",
            UseShellExecute = false
        });
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (webProcess != null && !webProcess.HasExited)
            webProcess.Kill();
    }
}