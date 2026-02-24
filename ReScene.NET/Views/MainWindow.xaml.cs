using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using ReScene.NET.ViewModels;

namespace ReScene.NET.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Drop += OnDrop;
        DragOver += OnDragOver;
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

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

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.None;

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null)
            {
                foreach (var file in files)
                {
                    if (IsSceneFile(file))
                    {
                        e.Effects = DragDropEffects.Copy;
                        break;
                    }
                }
            }
        }

        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

        var files = e.Data.GetData(DataFormats.FileDrop) as string[];
        if (files == null) return;

        foreach (var file in files)
        {
            if (IsSceneFile(file))
            {
                if (DataContext is MainWindowViewModel vm)
                    vm.OpenSceneFile(file);
                break;
            }
        }
    }

    private void OnHyperlinkRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnAboutClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = "About ReScene.NET",
            Width = 350,
            Height = 160,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Children =
                {
                    new TextBlock { Text = "ReScene.NET", FontSize = 18, FontWeight = FontWeights.Bold },
                    new TextBlock { Text = "SRR/SRS File Inspector and Creator", Margin = new Thickness(0, 8, 0, 0) },
                    new TextBlock { Text = "Built with WPF", Foreground = System.Windows.Media.Brushes.Gray, Margin = new Thickness(0, 8, 0, 0) }
                }
            }
        };

        dialog.ShowDialog();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Ctrl+1 through Ctrl+6 switch tabs
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key >= Key.D1 && e.Key <= Key.D6)
        {
            if (DataContext is MainWindowViewModel vm)
                vm.SelectedTabIndex = e.Key - Key.D1;
            e.Handled = true;
        }
    }
}
