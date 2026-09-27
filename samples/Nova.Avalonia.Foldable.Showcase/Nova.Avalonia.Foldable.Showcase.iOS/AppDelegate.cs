using Avalonia;
using Avalonia.iOS;
using Foundation;

namespace Nova.Avalonia.Foldable.Showcase.iOS;

[Register("AppDelegate")]
public sealed class AppDelegate : AvaloniaAppDelegate<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) =>
        base.CustomizeAppBuilder(builder).WithInterFont();
}
