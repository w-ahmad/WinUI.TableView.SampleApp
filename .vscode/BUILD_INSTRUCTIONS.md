# Building WinUI.TableView.SampleApp in VS Code

## Prerequisites
- Visual Studio 2022 (Enterprise/Professional/Community) with Windows App SDK workload installed
- .NET 9.0 SDK
- Windows 10/11 SDK

## Build Configuration

This project uses **MSBuild** (not `dotnet build`) because WinUI 3 apps require MSIX packaging tools that are only available in Visual Studio's MSBuild.

### VS Code Tasks

The following keyboard shortcuts work in VS Code:

- **Ctrl+Shift+B**: Build the project (default build task)
- **F5**: Build and launch the app with debugger attached

### Manual Build Commands

From PowerShell in the workspace root:

```powershell
# Build
& "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\msbuild.exe" `
  src/WinUI.TableView.SampleApp/WinUI.TableView.SampleApp.csproj `
  /t:Build /p:Configuration=Debug /p:Platform=x64

# Clean
& "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\msbuild.exe" `
  src/WinUI.TableView.SampleApp/WinUI.TableView.SampleApp.csproj `
  /t:Clean /p:Configuration=Debug /p:Platform=x64

# Restore NuGet packages
dotnet restore src/WinUI.TableView.SampleApp/WinUI.TableView.SampleApp.csproj
```

### Output Location

Built binaries are located at:
```
src/WinUI.TableView.SampleApp/bin/x64/Debug/net9.0-windows10.0.22621.0/win-x64/WinUI.TableView.SampleApp.exe
```

## Troubleshooting

### Build Error: MSB4062 - Microsoft.Build.Packaging.Pri.Tasks not found

**Problem**: This error occurs when trying to use `dotnet build` instead of MSBuild.

**Solution**: WinUI 3 apps must be built with Visual Studio's MSBuild. The tasks.json configuration already uses the correct MSBuild path.

### MSBuild not found

If you see "The term 'msbuild' is not recognized":
1. MSBuild is not in your PATH (expected)
2. The tasks.json uses the full path: `C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\msbuild.exe`
3. Adjust the path if you have a different Visual Studio edition (Community/Professional)

## New Grouping Feature

The build includes the newly added grouping functionality:
- **File**: `WinUI.TableView/src/TableView.Grouping.cs`
- **Example Page**: `src/WinUI.TableView.SampleApp/Pages/GroupingExamplePage.xaml`
- **Documentation**: `WinUI.TableView/docs/GROUPING.md`

To test the grouping feature:
1. Press F5 to launch the app
2. Navigate to the "Grouping Example" page
3. Select a property to group by (Department, Status, Priority)
4. Test expand/collapse functionality
