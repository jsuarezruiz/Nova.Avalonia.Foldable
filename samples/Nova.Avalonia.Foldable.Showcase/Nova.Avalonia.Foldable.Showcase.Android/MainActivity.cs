using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace Nova.Avalonia.Foldable.Showcase.Android;

// ScreenLayout, SmallestScreenSize and Density matter on foldables: folding and unfolding changes the
// screen, and on some devices its density. Handling the change here keeps the activity, and the app's
// state, alive across it.
[Activity(
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ResizeableActivity = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : AvaloniaMainActivity
{
}
