# WinUI.TableView.SampleApp Development Instructions

**Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.**

## About This Repository

The WinUI.TableView.SampleApp is a cross-platform sample application showcasing the [WinUI.TableView](https://github.com/w-ahmad/WinUI.TableView) control. It demonstrates table functionality including selection, filtering, sorting, editing, and data export across Windows (WinUI), WebAssembly, Android, and iOS platforms using the Uno Platform framework.

## Working Effectively

### Prerequisites and Setup
- Install .NET 9.0 SDK: `curl -L https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 9.0 --install-dir ~/.dotnet`
- Add to PATH: `export PATH="$HOME/.dotnet:$PATH"`
- Install required workloads: `dotnet workload install wasm-tools android`
- **CRITICAL**: Always use the `--recurse-submodules` flag when cloning: `git clone --recurse-submodules https://github.com/w-ahmad/WinUI.TableView.SampleApp.git`
- If already cloned, initialize submodules: `git submodule update --init --recursive`

### Building the Application

Navigate to the project directory: `cd src/WinUI.TableView.SampleApp.Uno`

#### Build Commands and Timing Expectations
**NEVER CANCEL builds or long-running commands. Set timeouts appropriately:**

- **Package restore**: `dotnet restore -p:EnableWindowsTargeting=true` -- takes 45-50 seconds. NEVER CANCEL. Set timeout to 90+ seconds.
- **WebAssembly Debug build**: `dotnet build -f net9.0-browserwasm -p:EnableWindowsTargeting=true` -- takes 55-60 seconds. NEVER CANCEL. Set timeout to 120+ seconds.
- **WebAssembly Release build**: `dotnet publish -f net9.0-browserwasm -p:EnableWindowsTargeting=true --configuration Release` -- takes 5-6 minutes. NEVER CANCEL. Set timeout to 10+ minutes.
- **Android build**: `dotnet build -f net9.0-android -p:EnableWindowsTargeting=true` -- takes 70-75 seconds. NEVER CANCEL. Set timeout to 150+ seconds.
- **Windows build**: Only possible on Windows machines with WinUI SDK. On Linux, use EnableWindowsTargeting=true flag.

#### Platform-Specific Notes
- **Linux/macOS**: Must use `-p:EnableWindowsTargeting=true` for all builds due to Windows target framework
- **WebAssembly**: Requires wasm-tools workload, builds to `bin/Release/net9.0-browserwasm/publish/wwwroot`
- **Android**: Requires android workload, produces APK files
- **iOS**: Requires macOS with Xcode (not testable on Linux)

### Running the Application

#### WebAssembly (Browser)
```bash
cd src/WinUI.TableView.SampleApp.Uno
dotnet publish -f net9.0-browserwasm -p:EnableWindowsTargeting=true --configuration Release
cd bin/Release/net9.0-browserwasm/publish/wwwroot
python3 -m http.server 8080
# Navigate to http://localhost:8080
```

#### Android
Requires Android device or emulator connected. Use `dotnet run -f net9.0-android` (not tested on Linux environment).

### Testing and Validation

#### Manual Testing Scenarios
**ALWAYS run through these scenarios after making changes:**

1. **Application Launch**: Verify the app loads without errors and displays the main table view
2. **Table Functionality**: Navigate through different demo sections (Overview, Grid Lines, Selection, etc.)
3. **Data Display**: Confirm sample data displays correctly with proper formatting
4. **Navigation**: Test left sidebar navigation between different table features
5. **Responsive Design**: Verify layout adapts to different screen sizes

#### Automated Testing
- Unit tests are available in the WinUI.TableView submodule: `WinUI.TableView/tests/`
- Tests require Windows environment to run (WinUI-specific)
- Command: `dotnet test -p:EnableWindowsTargeting=true` (builds but doesn't execute on Linux)

### Code Quality

#### Formatting and Linting
Navigate to `src/` directory for all formatting commands.

- **Format code**: `dotnet format WinUI.TableView.SampleApp.Uno/WinUI.TableView.SampleApp.Uno.csproj --no-restore whitespace` -- fixes whitespace formatting
- **Check formatting**: `dotnet format WinUI.TableView.SampleApp.Uno/WinUI.TableView.SampleApp.Uno.csproj --verify-no-changes --no-restore` -- validates formatting without making changes
- **Style analysis**: `dotnet format WinUI.TableView.SampleApp.Uno/WinUI.TableView.SampleApp.Uno.csproj --no-restore style` -- applies code style fixes

**Note**: Formatting the entire solution includes the submodule which may have its own formatting conventions. Focus on the sample app project specifically.

#### Build Validation Steps
Always run these commands before committing changes:
1. `cd src/WinUI.TableView.SampleApp.Uno && dotnet restore -p:EnableWindowsTargeting=true`
2. `dotnet build -f net9.0-browserwasm -p:EnableWindowsTargeting=true`
3. `cd ../.. && cd src && dotnet format WinUI.TableView.SampleApp.Uno/WinUI.TableView.SampleApp.Uno.csproj --verify-no-changes --no-restore`
4. Manual testing of WebAssembly build

### Common Issues and Solutions

#### Known Build Issues
- **Error CS0120 in App.xaml.cs**: If you see "An object reference is required for the non-static field DebugSettings", this is a known issue with the WASM target. The fix is already applied.
- **EnableWindowsTargeting**: Required on non-Windows platforms due to Windows-specific target frameworks
- **Submodule not found**: Run `git submodule update --init --recursive`

#### Performance Considerations
- Debug builds are fast (~1 minute) but Release builds with AOT compilation take 5+ minutes
- WebAssembly builds include Emscripten compilation which is CPU-intensive
- Android builds include resource processing and APK generation

### Repository Structure

```
/
├── .github/                    # GitHub workflows and configuration
│   ├── workflows/
│   │   ├── ci-build.yml       # Windows CI build pipeline
│   │   └── azure-static-web-app.yml  # WebAssembly deployment
├── src/
│   ├── WinUI.TableView.SampleApp/     # Windows WinUI project
│   ├── WinUI.TableView.SampleApp.Uno/ # Cross-platform Uno project
│   ├── Directory.Build.props   # Global build properties
│   ├── Directory.Packages.props # Package version management
│   └── global.json            # .NET SDK version pinning
├── WinUI.TableView/           # Git submodule with the control library
└── README.md
```

### Key Files and Their Purpose
- `src/WinUI.TableView.SampleApp.Uno/WinUI.TableView.SampleApp.Uno.csproj`: Main cross-platform project
- `src/global.json`: Specifies Uno.Sdk version (currently 6.2.0-dev.26)
- `src/Directory.Packages.props`: Centralized package version management
- `.github/workflows/ci-build.yml`: Windows-only CI build (x86, x64, ARM64)
- `.github/workflows/azure-static-web-app.yml`: WebAssembly deployment pipeline

### Development Workflow
1. Always work in the Uno project (`src/WinUI.TableView.SampleApp.Uno/`)
2. Shared code goes in the parent directory (`src/WinUI.TableView.SampleApp/`)
3. Platform-specific code goes in `Platforms/` subdirectories
4. Test WebAssembly builds early and frequently
5. Use Debug configuration for development, Release for final validation

### CI/CD Integration
The repository includes GitHub Actions for:
- **CI builds**: Windows builds for x86, x64, ARM64 (`ci-build.yml`)
- **WebAssembly deployment**: Automated builds and deployment to Azure Static Web Apps (`azure-static-web-app.yml`)

Both pipelines require submodule initialization and use MSBuild for Windows builds and dotnet CLI for WebAssembly builds.