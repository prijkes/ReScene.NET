using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ReScene.NET.Services;
using ReScene.NET.ViewModels;
using ReScene.NET.Views;

namespace ReScene.NET;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(
                    new SrrCreationService(), new SrsCreationService(), new FileDialogService(), new RecentFilesService())
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
