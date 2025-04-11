using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace WinUI.TableView.SampleApp.Helpers;

internal class TitleBarHelper
{
    public static Color ApplySystemThemeToCaptionButtons(Window window)
    {
        var element = App.Current.MainWindow.Content as FrameworkElement;
        var color = element.ActualTheme == ElementTheme.Dark ? Colors.White : Colors.Black;
        SetCaptionButtonColors(window, color);
        return color;
    }

    public static void SetCaptionButtonColors(Window window, Color color)
    {
        var res = Application.Current.Resources;
        res["WindowCaptionForeground"] = color;
        window.AppWindow.TitleBar.ButtonForegroundColor = color;
    }
}