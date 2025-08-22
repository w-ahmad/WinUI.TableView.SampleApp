using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class HierarchyPage : Page
{
    public HierarchyPage()
    {
        InitializeComponent();

        var data = new ObservableCollection<Node>
        {
            new Node("Root A", new ObservableCollection<Node>
            {
                new Node("A.1", new ObservableCollection<Node> { new Node("A.1.a"), new Node("A.1.b") }),
                new Node("A.2"),
            }),
            new Node("Root B", new ObservableCollection<Node>
            {
                new Node("B.1"),
                new Node("B.2", new ObservableCollection<Node> { new Node("B.2.a"), new Node("B.2.b"), new Node("B.2.c") }),
            }),
        };

        HierarchyTable.ItemsSource = data;
    }

    private void OnExpandAll(object sender, RoutedEventArgs e)
    {
        // Hook up once expansion state is tracked (placeholder).
    }

    private void OnCollapseAll(object sender, RoutedEventArgs e)
    {
        // Hook up once expansion state is tracked (placeholder).
    }

    public sealed class Node
    {
        public Node(string name, ObservableCollection<Node>? children = null)
        {
            Name = name;
            Children = children ?? new ObservableCollection<Node>();
        }

        public string Name { get; set; }
        public ObservableCollection<Node> Children { get; set; }
    }
}
