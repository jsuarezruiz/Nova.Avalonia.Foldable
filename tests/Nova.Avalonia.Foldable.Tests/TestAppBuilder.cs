using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(Nova.Avalonia.Foldable.Tests.TestAppBuilder))]

namespace Nova.Avalonia.Foldable.Tests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<Application>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
