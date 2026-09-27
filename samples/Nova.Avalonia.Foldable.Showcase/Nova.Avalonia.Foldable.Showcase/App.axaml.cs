using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Nova.Avalonia.Foldable.Showcase.Views;

namespace Nova.Avalonia.Foldable.Showcase;

public partial class App : Application
{
    /// <summary>
    /// Lets the desktop head host the view next to the fold simulator.
    /// </summary>
    public static Func<MainView, Window>? MainWindowFactory { get; set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var view = new MainView();
            desktop.MainWindow = MainWindowFactory?.Invoke(view)
                ?? new Window { Title = "Guide To the Stars", Width = 375, Height = 812, Content = view };
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime activity)
        {
            activity.MainViewFactory = () => new MainView();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new MainView();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
