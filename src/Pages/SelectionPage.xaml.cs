using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class SelectionPage : Page
{
    public SelectionPage()
    {
        InitializeComponent();

        Loaded += SelectionPage_Loaded;
    }

    private async void SelectionPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await Task.Delay(5000);

        tableView.Columns =
        [
            new TableViewTextColumn {Header  = "Id", Binding = new Binding{Path = new PropertyPath("Id")}}
        ];
    }
}
