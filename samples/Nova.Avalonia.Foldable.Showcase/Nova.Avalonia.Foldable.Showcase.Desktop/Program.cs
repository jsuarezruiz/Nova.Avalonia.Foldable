using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Nova.Avalonia.Foldable.Simulator;

namespace Nova.Avalonia.Foldable.Showcase.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.MainWindowFactory = view => new FoldableSimulatorWindow(view, "Guide To the Stars")
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://Nova.Avalonia.Foldable.Showcase.Desktop/AppIcon.png"))),
        };
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
