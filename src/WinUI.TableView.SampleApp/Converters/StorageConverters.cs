using Microsoft.UI.Xaml.Data;
using System;

namespace WinUI.TableView.SampleApp.Converters;

/// <summary>
/// Converts a boolean IsExpanded value to a chevron glyph.
/// </summary>
public class BoolToChevronConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isExpanded)
        {
            return isExpanded ? "\uE70E" : "\uE76C"; // Down chevron : Right chevron
        }
        return "\uE76C"; // Default to collapsed (right chevron)
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts file size in bytes to human-readable format.
/// </summary>
public class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not ulong bytes)
        {
            return "0 bytes";
        }

        if (bytes == 0) return "0 bytes";
        
        string[] sizes = { "bytes", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        
        return $"{size:0.##} {sizes[order]}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts file type to appropriate Segoe MDL2 icon glyph.
/// </summary>
public class FileTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var fileType = value?.ToString()?.ToLowerInvariant() ?? string.Empty;

        return fileType switch
        {
            // Images
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".svg" or ".ico" => "\uEB9F",
            
            // Videos
            ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" or ".flv" or ".webm" => "\uE8B2",
            
            // Music
            ".mp3" or ".wav" or ".flac" or ".m4a" or ".wma" or ".aac" or ".ogg" => "\uE8D6",
            
            // Archives
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" or ".bz2" => "\uE8B5",
            
            // Executables
            ".exe" or ".msi" or ".bat" or ".cmd" or ".ps1" => "\uE756",
            
            // Documents
            ".pdf" => "\uE8A5",
            ".doc" or ".docx" => "\uE8A5",
            ".xls" or ".xlsx" => "\uE8A5",
            ".ppt" or ".pptx" => "\uE8A5",
            ".txt" or ".log" => "\uE8A5",
            
            // Code files
            ".cs" or ".cpp" or ".c" or ".h" or ".hpp" => "\uE943",
            ".js" or ".ts" or ".jsx" or ".tsx" => "\uE943",
            ".py" or ".rb" or ".java" or ".go" => "\uE943",
            ".html" or ".css" or ".scss" or ".xml" or ".json" => "\uE943",
            
            // Folders
            "folder" => "\uE8B7",
            
            // Default
            _ => "\uE8A5" // Generic document icon
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
