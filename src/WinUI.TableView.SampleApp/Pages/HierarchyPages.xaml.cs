using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinUI.TableView; // for sorting event args and enums

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class HierarchyPage : Page
{
    public ObservableCollection<FlatNode> FlatItems { get; } = [];
    private ObservableCollection<Node> _roots = [];
    private DispatcherTimer? _timer;

    public HierarchyPage()
    {
        InitializeComponent();

        _roots = BuildMockTree();
        RebuildFlat(dir: null);
        HierarchyTable.ItemsSource = FlatItems;

        // start live updates
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, __) => UpdateMetrics();
        _timer.Start();
    }

    // Node presented to the TableView (flattened with depth and expansion state)
    public sealed class FlatNode : INotifyPropertyChanged
    {
        private bool _isExpanded;

        public FlatNode(Node node, int depth, FlatNode? parent)
        {
            Node = node;
            Depth = depth;
            Parent = parent;
        }

        public Node Node { get; }
        public int Depth { get; }
        public FlatNode? Parent { get; }

        public string Name => Node.Name;
        public ObservableCollection<Node> Children => Node.Children;
        public bool HasChildren => Children.Count > 0;

        // Icon per node (root apps unique, processes shared glyph)
        public string IconGlyph => Node.IconGlyph;

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        // for binding in XAML (indentation increased)
        public Thickness Indent => new(left: Depth * 24, top: 0, right: 0, bottom: 0);

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public sealed class Node : INotifyPropertyChanged
    {
        private double _cpu, _memoryMB, _diskMBps, _networkMBps, _gpu, _npu, _energyKW;

        public Node(string name, ObservableCollection<Node>? children = null)
        {
            Name = name;
            Children = children ?? new ObservableCollection<Node>();
            IconGlyph = ProcessGlyph; // default to process glyph, roots will be set explicitly
        }

        public string Name { get; set; }
        public ObservableCollection<Node> Children { get; set; }

        public string IconGlyph { get; set; }

        // Metrics per row with notifications
        public double Cpu { get => _cpu; set { if (_cpu != value) { _cpu = value; OnPropertyChanged(); } } }
        public double MemoryMB { get => _memoryMB; set { if (_memoryMB != value) { _memoryMB = value; OnPropertyChanged(); } } }
        public double DiskMBps { get => _diskMBps; set { if (_diskMBps != value) { _diskMBps = value; OnPropertyChanged(); } } }
        public double NetworkMBps { get => _networkMBps; set { if (_networkMBps != value) { _networkMBps = value; OnPropertyChanged(); } } }
        public double Gpu { get => _gpu; set { if (_gpu != value) { _gpu = value; OnPropertyChanged(); } } }
        public double Npu { get => _npu; set { if (_npu != value) { _npu = value; OnPropertyChanged(); } } }
        public double EnergyKW { get => _energyKW; set { if (_energyKW != value) { _energyKW = value; OnPropertyChanged(); } } }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private static readonly string ProcessGlyph = "\uE943"; // generic process icon
    private static readonly string[] AppGlyphs =
    {
        "\uECAD", // AppIconDefault
        "\uE7F8", // Game
        "\uE8B8", // Settings
        "\uE74C", // Mail
        "\uE721", // Camera
        "\uE13D", // Video
        "\uE11C", // MusicInfo
        "\uE8D4", // Shop
        "\uE8C8", // Contact
        "\uE7FC", // Calculator
        "\uE16E", // Photo
        "\uE7F2", // Clock
        "\uE8A5", // People
        "\uE8F1", // World
        "\uE75F", // Archive
    };

    private static ObservableCollection<Node> BuildMockTree()
    {
        // Generate a deeper and wider tree of applications and their processes
        var rnd = new Random(1);
        var roots = new ObservableCollection<Node>();

        for (int r = 1; r <= 25; r++)
        {
            var root = GenerateApp($"App {r}", depth: 0, maxDepth: 2, breadth: 6, rnd);
            root.IconGlyph = AppGlyphs[(r - 1) % AppGlyphs.Length];
            roots.Add(root);
        }

        return roots;

        static Node GenerateApp(string name, int depth, int maxDepth, int breadth, Random rnd)
        {
            var node = new Node(name);
            AssignMetrics(node, rnd, depth);

            if (depth < maxDepth)
            {
                var count = breadth - depth + rnd.Next(0, 3); // more children at higher levels
                node.Children = new ObservableCollection<Node>(
                    Enumerable.Range(1, count)
                              .Select(i =>
                              {
                                  var child = GenerateApp($"{name} - Proc {i}", depth + 1, maxDepth, breadth, rnd);
                                  child.IconGlyph = ProcessGlyph;
                                  return child;
                              })
                );
            }

            return node;
        }

        static void AssignMetrics(Node n, Random rnd, int depth)
        {
            // Simulate metrics; app totals vs processes have slightly different ranges
            var scale = depth == 0 ? 1.0 : 0.6;
            n.Cpu = Math.Round(rnd.NextDouble() * 90 * scale, 1);
            n.MemoryMB = Math.Round(100 + rnd.NextDouble() * 5000 * scale, 0);
            n.DiskMBps = Math.Round(rnd.NextDouble() * 200 * scale, 1);
            n.NetworkMBps = Math.Round(rnd.NextDouble() * 150 * scale, 1);
            n.Gpu = Math.Round(rnd.NextDouble() * 90 * scale, 1);
            n.Npu = Math.Round(rnd.NextDouble() * 90 * scale, 1);
            n.EnergyKW = Math.Round(rnd.NextDouble() * 0.5 * scale, 3);
        }
    }

    private void UpdateMetrics()
    {
        var rnd = new Random();
        foreach (var root in _roots)
        {
            Jitter(root, 0);
        }

        void Jitter(Node node, int depth)
        {
            double Clamp(double v, double min, double max) => Math.Max(min, Math.Min(max, v));
            double Scale() => depth == 0 ? 1.0 : 0.6;
            var s = Scale();

            node.Cpu = Clamp(node.Cpu + (rnd.NextDouble() - 0.5) * 10 * s, 0, 100);
            node.Gpu = Clamp(node.Gpu + (rnd.NextDouble() - 0.5) * 10 * s, 0, 100);
            node.Npu = Clamp(node.Npu + (rnd.NextDouble() - 0.5) * 10 * s, 0, 100);

            node.MemoryMB = Clamp(node.MemoryMB + (rnd.NextDouble() - 0.5) * 200 * s, 0, 8000);
            node.DiskMBps = Clamp(node.DiskMBps + (rnd.NextDouble() - 0.5) * 20 * s, 0, 400);
            node.NetworkMBps = Clamp(node.NetworkMBps + (rnd.NextDouble() - 0.5) * 15 * s, 0, 300);
            node.EnergyKW = Clamp(node.EnergyKW + (rnd.NextDouble() - 0.5) * 0.05 * s, 0, 1.0);

            foreach (var c in node.Children)
            {
                Jitter(c, depth + 1);
            }
        }
    }

    private void RebuildFlat(SortDirection? dir)
    {
        var expanded = FlatItems.Where(f => f.IsExpanded).Select(f => f.Node).ToHashSet();
        FlatItems.Clear();

        IEnumerable<Node> Order(IEnumerable<Node> nodes) => dir switch
        {
            SortDirection.Ascending => nodes.OrderBy(n => n.Name, StringComparer.OrdinalIgnoreCase),
            SortDirection.Descending => nodes.OrderByDescending(n => n.Name, StringComparer.OrdinalIgnoreCase),
            _ => nodes,
        };

        void AddNodeAndChildren(Node node, int depth, FlatNode? parent)
        {
            var fn = new FlatNode(node, depth, parent)
            {
                IsExpanded = expanded.Contains(node)
            };
            FlatItems.Add(fn);

            if (fn.IsExpanded)
            {
                foreach (var child in Order(node.Children))
                {
                    AddNodeAndChildren(child, depth + 1, fn);
                }
            }
        }

        foreach (var root in Order(_roots))
        {
            AddNodeAndChildren(root, 0, null);
        }
    }

    private void OnToggleNode(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not FlatNode flat) return;
        var index = FlatItems.IndexOf(flat);
        if (index < 0) return;

        if (flat.IsExpanded)
        {
            Collapse(flat, index);
        }
        else
        {
            Expand(flat, index);
        }
    }

    private void Expand(FlatNode flat, int atIndex)
    {
        flat.IsExpanded = true;
        var insertAt = atIndex + 1;
        foreach (var child in flat.Children.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
        {
            var fn = new FlatNode(child, flat.Depth + 1, flat);
            FlatItems.Insert(insertAt++, fn);
        }
    }

    private void Collapse(FlatNode flat, int atIndex)
    {
        flat.IsExpanded = false;
        // remove all descendants
        var removeFrom = atIndex + 1;
        while (removeFrom < FlatItems.Count && IsDescendantOf(FlatItems[removeFrom], flat))
        {
            FlatItems.RemoveAt(removeFrom);
        }
    }

    private static bool IsDescendantOf(FlatNode candidate, FlatNode ancestor)
    {
        var cur = candidate.Parent;
        while (cur is not null)
        {
            if (ReferenceEquals(cur, ancestor)) return true;
            cur = cur.Parent;
        }
        return false;
    }

    private void OnExpandAll(object sender, RoutedEventArgs e)
    {
        for (int i = 0; i < FlatItems.Count; i++)
        {
            var flat = FlatItems[i];
            if (flat.HasChildren && !flat.IsExpanded)
            {
                Expand(flat, i);
            }
        }
    }

    private void OnCollapseAll(object sender, RoutedEventArgs e)
    {
        for (int i = FlatItems.Count - 1; i >= 0; i--)
        {
            var flat = FlatItems[i];
            if (flat.IsExpanded)
            {
                Collapse(flat, i);
            }
        }
    }

    // Sorting handlers
    private void OnTableSorting(object sender, TableViewSortingEventArgs e)
    {
        if (e.Column?.Header?.ToString() != "Name") return;

        var next = e.Column.SortDirection switch
        {
            SortDirection.Ascending => SortDirection.Descending,
            SortDirection.Descending => (SortDirection?)null,
            _ => SortDirection.Ascending,
        };

        e.Column.SortDirection = next;
        RebuildFlat(next);
        e.Handled = true;
    }

    private void OnTableClearSorting(object sender, TableViewClearSortingEventArgs e)
    {
        if (e.Column is not null)
        {
            e.Column.SortDirection = null;
        }

        RebuildFlat(dir: null);
        e.Handled = true;
    }

    // XAML helpers for bindings
    public static string GlyphFor(bool isExpanded) => isExpanded ? "\uE70D" /*ChevronDown*/ : "\uE70E" /*ChevronRight*/;
    public static Visibility VisibilityForHasChildren(bool hasChildren) => hasChildren ? Visibility.Visible : Visibility.Collapsed;
}
