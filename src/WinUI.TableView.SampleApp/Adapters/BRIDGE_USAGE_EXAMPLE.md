# CollectionViewSource Bridge Usage Example

## Basic Setup

```csharp
using WinUI.TableView.SampleApp.Adapters;
using Microsoft.UI.Xaml.Data;
using System.Collections.ObjectModel;

public sealed partial class MyPage : Page
{
    private CollectionViewSourceBridge _bridge;
    private ObservableCollection<ExampleModel> _data;
    private CollectionViewSource _collectionViewSource;

    public MyPage()
    {
        this.InitializeComponent();
        
        // 1. Create your data collection
        _data = new ObservableCollection<ExampleModel>();
        
        // 2. Create CollectionViewSource
        _collectionViewSource = new CollectionViewSource
        {
            Source = _data,
            IsSourceGrouped = false
        };
        
        // 3. Create the bridge
        _bridge = CollectionViewSourceBridgeFactory.CreateForExampleModels(_collectionViewSource);
        
        // 4. Bind TableView to adapted items
        MyTableView.ItemsSource = _bridge.AdaptedItems;
        
        LoadSampleData();
    }
    
    private void LoadSampleData()
    {
        // Add sample data
        _data.Add(new ExampleModel { FirstName = "John", LastName = "Doe", Department = "Engineering" });
        _data.Add(new ExampleModel { FirstName = "Jane", LastName = "Smith", Department = "Marketing" });
        _data.Add(new ExampleModel { FirstName = "Bob", LastName = "Johnson", Department = "Engineering" });
        // Bridge automatically syncs changes to TableView!
    }
    
    private void GroupByDepartment_Click(object sender, RoutedEventArgs e)
    {
        // ListView-style grouping!
        _bridge.SetGrouping("Department");
    }
    
    private void ClearGrouping_Click(object sender, RoutedEventArgs e)
    {
        _bridge.ClearGrouping();
    }
    
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        _bridge?.Dispose(); // Clean up resources
        base.OnNavigatedFrom(e);
    }
}
```

## XAML Setup

```xml
<controls:TableView x:Name="MyTableView" SelectionMode="Single">
    <controls:TableView.Columns>
        <controls:TableViewTextColumn Header="First Name" 
                                      Binding="{Binding UnderlyingItem.FirstName}"
                                      Width="120"/>
        <controls:TableViewTextColumn Header="Last Name" 
                                      Binding="{Binding UnderlyingItem.LastName}"
                                      Width="120"/>
        <controls:TableViewTextColumn Header="Department" 
                                      Binding="{Binding UnderlyingItem.Department}"
                                      Width="120"/>
    </controls:TableView.Columns>
</controls:TableView>
```

## Key Points

1. **Automatic Synchronization**: Changes to `_data` automatically appear in the TableView
2. **ListView-style Grouping**: Use `SetGrouping("PropertyName")` like PropertyGroupDescription
3. **Access Original Data**: Use `UnderlyingItem` in bindings to access the original object
4. **Type Safety**: Factory methods provide compile-time checking
5. **Memory Management**: Always dispose the bridge when done

## Supported Data Types

- `ExampleModel` - via `CreateForExampleModels()`
- `IStorageItem` - via `CreateForStorageItems()`  
- Any type - via `CreateAutoDetect()` or `CreateCustom<T>()`

The bridge successfully enables ListView-style CollectionViewSource integration with TableView while preserving all of TableView's advanced features like column sorting, virtualization, and custom cell types.