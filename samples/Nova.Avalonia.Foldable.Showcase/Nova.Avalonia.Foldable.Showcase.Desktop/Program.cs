using Avalonia;

namespace Nova.Avalonia.Foldable.Showcase.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.MainWindowFactory = view => new SimulatorWindow(view);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
