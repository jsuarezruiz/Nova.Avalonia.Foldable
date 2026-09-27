using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace Nova.Avalonia.Foldable.Showcase.Android;

// ScreenLayout and SmallestScreenSize matter on foldables: folding and unfolding changes the screen,
// and handling it here keeps the activity (and the app's state) alive across the change.
[Activity(
    Label = "Guide To the Stars",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ResizeableActivity = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
public class MainActivity : AvaloniaMainActivity
{
}
