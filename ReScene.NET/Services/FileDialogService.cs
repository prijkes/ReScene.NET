using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace ReScene.NET.Services;

public class FileDialogService : IFileDialogService
{
    private static Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }

    public async Task<string?> OpenFileAsync(string title, IReadOnlyList<string> filters)
    {
        var window = GetMainWindow();
        if (window == null) return null;

        var storageProvider = window.StorageProvider;
        var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = BuildFileTypes(filters)
        });

        return result.Count > 0 ? result[0].Path.LocalPath : null;
    }

    public async Task<IReadOnlyList<string>> OpenFilesAsync(string title, IReadOnlyList<string> filters)
    {
        var window = GetMainWindow();
        if (window == null) return [];

        var storageProvider = window.StorageProvider;
        var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = true,
            FileTypeFilter = BuildFileTypes(filters)
        });

        return result.Select(f => f.Path.LocalPath).ToList();
    }

    public async Task<string?> SaveFileAsync(string title, string defaultExtension, IReadOnlyList<string> filters, string? defaultFileName = null)
    {
        var window = GetMainWindow();
        if (window == null) return null;

        var storageProvider = window.StorageProvider;
        var result = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            DefaultExtension = defaultExtension,
            SuggestedFileName = defaultFileName,
            FileTypeChoices = BuildFileTypes(filters)
        });

        return result?.Path.LocalPath;
    }

    private static List<FilePickerFileType> BuildFileTypes(IReadOnlyList<string> filters)
    {
        var types = new List<FilePickerFileType>();
        foreach (string filter in filters)
        {
            // Filter format: "Description|*.ext1;*.ext2"
            var parts = filter.Split('|');
            if (parts.Length == 2)
            {
                var patterns = parts[1].Split(';').ToList();
                types.Add(new FilePickerFileType(parts[0]) { Patterns = patterns });
            }
        }
        return types;
    }
}
