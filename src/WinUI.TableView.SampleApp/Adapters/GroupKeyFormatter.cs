using System;

namespace WinUI.TableView.SampleApp.Adapters;

/// <summary>
/// Helper class for formatting group keys based on property values.
/// Provides consistent grouping logic for common scenarios like dates, sizes, and file types.
/// </summary>
public static class GroupKeyFormatter
{
    /// <summary>
    /// Formats a property value into a group key string.
    /// </summary>
    /// <param name="propertyName">The property name being grouped by.</param>
    /// <param name="value">The property value.</param>
    /// <returns>A formatted group key string.</returns>
    public static string FormatGroupKey(string propertyName, object? value)
    {
        if (value is null)
        {
            return "(Unknown)";
        }

        return propertyName switch
        {
            "DateModified" when value is DateTimeOffset dto => FormatDateGroup(dto),
            "DateModified" when value is DateTime dt => FormatDateGroup(new DateTimeOffset(dt)),
            "Dob" when value is DateOnly dateOnly => FormatDateGroup(new DateTimeOffset(dateOnly.ToDateTime(TimeOnly.MinValue))),
            "Size" when value is ulong size => FormatSizeGroup(size),
            "Size" when value is long size => FormatSizeGroup((ulong)size),
            "FileType" when value is string ext => FormatFileTypeGroup(ext),
            "ItemType" => value.ToString() ?? "(Unknown)",
            "Name" => FormatAlphabeticalGroup(value.ToString() ?? ""),
            _ => value.ToString() ?? "(Unknown)"
        };
    }

    /// <summary>
    /// Formats a date into a relative time group (Today, Yesterday, This Week, etc.).
    /// </summary>
    public static string FormatDateGroup(DateTimeOffset date)
    {
        var now = DateTimeOffset.Now;
        var daysDiff = (now.Date - date.Date).Days;

        return daysDiff switch
        {
            0 => "Today",
            1 => "Yesterday",
            < 7 => "This Week",
            < 14 => "Last Week",
            < 30 => "This Month",
            < 60 => "Last Month",
            < 365 => date.ToString("MMMM yyyy"),
            _ => date.ToString("yyyy")
        };
    }

    /// <summary>
    /// Formats a file size into a size category group.
    /// </summary>
    public static string FormatSizeGroup(ulong size)
    {
        const ulong KB = 1024;
        const ulong MB = KB * 1024;
        const ulong GB = MB * 1024;

        return size switch
        {
            0 => "Empty",
            < KB => "Tiny (< 1 KB)",
            < KB * 100 => "Small (< 100 KB)",
            < MB => "Medium (< 1 MB)",
            < MB * 10 => "Large (< 10 MB)",
            < MB * 100 => "Very Large (< 100 MB)",
            < GB => "Huge (< 1 GB)",
            _ => "Gigantic (> 1 GB)"
        };
    }

    /// <summary>
    /// Formats a file extension into a file type category.
    /// </summary>
    public static string FormatFileTypeGroup(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return "No Extension";
        }

        var ext = extension.TrimStart('.').ToUpperInvariant();

        // Group common file types into categories
        return ext switch
        {
            // Documents
            "DOC" or "DOCX" or "PDF" or "TXT" or "RTF" or "ODT" => "Documents",
            
            // Spreadsheets
            "XLS" or "XLSX" or "CSV" or "ODS" => "Spreadsheets",
            
            // Presentations
            "PPT" or "PPTX" or "ODP" => "Presentations",
            
            // Images
            "JPG" or "JPEG" or "PNG" or "GIF" or "BMP" or "SVG" or "WEBP" or "ICO" => "Images",
            
            // Videos
            "MP4" or "AVI" or "MKV" or "MOV" or "WMV" or "FLV" or "WEBM" => "Videos",
            
            // Audio
            "MP3" or "WAV" or "FLAC" or "AAC" or "OGG" or "WMA" or "M4A" => "Audio",
            
            // Archives
            "ZIP" or "RAR" or "7Z" or "TAR" or "GZ" or "BZ2" => "Archives",
            
            // Code
            "CS" or "XAML" or "XML" or "JSON" or "JS" or "TS" or "PY" or "JAVA" or "CPP" or "H" => "Code Files",
            
            // Executables
            "EXE" or "DLL" or "MSI" or "APP" => "Applications",
            
            _ => $".{ext} Files"
        };
    }

    /// <summary>
    /// Formats a name into alphabetical groups (A-C, D-F, etc.).
    /// </summary>
    public static string FormatAlphabeticalGroup(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "# (Other)";
        }

        var firstChar = char.ToUpperInvariant(name[0]);

        if (!char.IsLetter(firstChar))
        {
            return "# (Other)";
        }

        // Group in ranges of 3 letters
        return firstChar switch
        {
            >= 'A' and <= 'C' => "A - C",
            >= 'D' and <= 'F' => "D - F",
            >= 'G' and <= 'I' => "G - I",
            >= 'J' and <= 'L' => "J - L",
            >= 'M' and <= 'O' => "M - O",
            >= 'P' and <= 'R' => "P - R",
            >= 'S' and <= 'U' => "S - U",
            >= 'V' and <= 'X' => "V - X",
            >= 'Y' and <= 'Z' => "Y - Z",
            _ => "# (Other)"
        };
    }

    /// <summary>
    /// Gets a sort key for a formatted group key to ensure proper ordering.
    /// </summary>
    public static string GetSortKey(string propertyName, string groupKey)
    {
        // For date groups, use a numeric prefix to ensure chronological order
        if (propertyName == "DateModified" || propertyName == "Dob")
        {
            return groupKey switch
            {
                "Today" => "1_Today",
                "Yesterday" => "2_Yesterday",
                "This Week" => "3_This Week",
                "Last Week" => "4_Last Week",
                "This Month" => "5_This Month",
                "Last Month" => "6_Last Month",
                _ when groupKey.Contains("2") => $"7_{groupKey}", // Years starting with 2 (2000+)
                _ => $"8_{groupKey}"
            };
        }

        // For size groups, use a numeric prefix
        if (propertyName == "Size")
        {
            return groupKey switch
            {
                "Empty" => "1_Empty",
                var s when s.StartsWith("Tiny") => "2_Tiny",
                var s when s.StartsWith("Small") => "3_Small",
                var s when s.StartsWith("Medium") => "4_Medium",
                var s when s.StartsWith("Large") => "5_Large",
                var s when s.StartsWith("Very Large") => "6_Very Large",
                var s when s.StartsWith("Huge") => "7_Huge",
                var s when s.StartsWith("Gigantic") => "8_Gigantic",
                _ => groupKey
            };
        }

        return groupKey;
    }
}
