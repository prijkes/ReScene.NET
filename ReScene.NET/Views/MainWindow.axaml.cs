using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ReScene.NET.ViewModels;

namespace ReScene.NET.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // Handle command-line arguments
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1 && IsSceneFile(args[1]) && File.Exists(args[1]))
        {
            if (DataContext is MainWindowViewModel vm)
                vm.OpenSceneFile(args[1]);
        }
    }

    private static bool IsSceneFile(string path)
    {
        return path.EndsWith(".srr", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".srs", StringComparison.OrdinalIgnoreCase);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.None;

        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles();
            if (files != null)
            {
                foreach (var file in files)
                {
                    if (IsSceneFile(file.Path.LocalPath))
                    {
                        e.DragEffects = DragDropEffects.Copy;
                        break;
                    }
                }
            }
        }
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files)) return;

        var files = e.Data.GetFiles();
        if (files == null) return;

        foreach (var file in files)
        {
            string path = file.Path.LocalPath;
            if (IsSceneFile(path))
            {
                if (DataContext is MainWindowViewModel vm)
                    vm.OpenSceneFile(path);
                break;
            }
        }
    }

    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnToggleHexView(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.Inspector.ShowHexView = !vm.Inspector.ShowHexView;
    }

    private async void OnAboutClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = "About ReScene.NET",
            Width = 350,
            Height = 160,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(20),
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "ReScene.NET", FontSize = 18, FontWeight = Avalonia.Media.FontWeight.Bold },
                    new TextBlock { Text = "SRR/SRS File Inspector and Creator" },
                    new TextBlock { Text = "Built with Avalonia UI", Foreground = Avalonia.Media.Brushes.Gray }
                }
            }
        };

        await dialog.ShowDialog(this);
    }
}
