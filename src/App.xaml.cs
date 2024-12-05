using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using WinUI.TableView.SampleApp.Helpers;

namespace WinUI.TableView.SampleApp;

public partial class App : Application
{
    private readonly Lazy<MainWindow> _mainWindow;
    private readonly Lazy<AppWindow> _appWindow;

    public App()
    {
        InitializeComponent();

        _mainWindow = new(() => new MainWindow());
        _appWindow = new(() =>
        {
            var hWnd = WindowNative.GetWindowHandle(Window);
            var wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        });
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Window.Content = new NavigationPage();
        ThemeHelper.Initialize();

        Window.Activate();
    }

    public Window Window => _mainWindow.Value;
    public AppWindow AppWindow => _appWindow.Value;
    public static new App Current => (App)Application.Current;
}
