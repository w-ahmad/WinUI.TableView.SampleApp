using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace WinUI.TableView.SampleApp.Converters;

public sealed class HeatmapBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is null)
        {
            return new SolidColorBrush(Colors.Transparent);
        }

        double v;
        try
        {
            v = System.Convert.ToDouble(value);
        }
        catch
        {
            return new SolidColorBrush(Colors.Transparent);
        }

        // Parse parameter: "percent" or "max:<number>"
        double max = 100.0;
        if (parameter is string p && !string.IsNullOrWhiteSpace(p))
        {
            p = p.Trim().ToLowerInvariant();
            if (p.StartsWith("max:"))
            {
                if (double.TryParse(p.AsSpan(4), out var m) && m > 0)
                {
                    max = m;
                }
            }
        }

        var ratio = max <= 0 ? 0 : Math.Clamp(v / max, 0.0, 1.0);

        // 5 strata: 0-0.2, 0.2-0.4, 0.4-0.6, 0.6-0.8, 0.8-1.0
        byte alpha = ratio switch
        {
            < 0.2 => (byte)0x10,
            < 0.4 => (byte)0x28,
            < 0.6 => (byte)0x40,
            < 0.8 => (byte)0x6A,
            _      => (byte)0x90,
        };

        // Use a warm color (OrangeRed) with varying alpha
        var baseColor = Colors.OrangeRed;
        var color = Windows.UI.Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);
        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
