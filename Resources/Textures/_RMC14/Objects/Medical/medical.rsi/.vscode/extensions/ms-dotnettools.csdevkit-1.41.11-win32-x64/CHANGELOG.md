# Change Log

## 1.41.* - Release
### Added
- [Retire "Add .NET Aspire Orchestration" right-click and command palette item](https://github.com/microsoft/vscode-dotnettools/issues/2104)
- [Razor editor issue working on pages in Aspire dashboard](https://github.com/microsoft/vscode-dotnettools/issues/2151)
### Fixed
- [IsTestProject state not resolved correctly for tests](https://github.com/microsoft/vscode-dotnettools/issues/1843)
- [C# Dev Kit fails to discover tests on built DLLs](https://github.com/microsoft/vscode-dotnettools/issues/2130)
- [Using statements are inserted at incorrect positions when accepting auto-import suggestions in Blazor](https://github.com/microsoft/vscode-dotnettools/issues/2065)

## 1.40.23 - Prerelease
### Fixed
- Additional telemetry and bug fixes

## 1.30.44 - Release
### Fixed
- [Error in Test Explorer project system observer](https://github.com/microsoft/vscode-dotnettools/issues/2116)
- [C# DevKit calls entitlement API 4 time instead of 1 to get a license information on startup](https://github.com/microsoft/vscode-dotnettools/issues/2134)

## 1.30.32 - Release
### Added
- [Show quick-pick templates during server initialization](https://github.com/microsoft/vscode-dotnettools/issues/2125)
### Fixed
- [Option "Open Solution" should appear for .slnx files](https://github.com/microsoft/vscode-dotnettools/issues/1905)
- [Refresh Tests doesn't respect open solution filter](https://github.com/microsoft/vscode-dotnettools/issues/2045)
- [Solution build doesn't target solution filters correctly](https://github.com/microsoft/vscode-dotnettools/issues/1347)
- [When selecting a .slnf for a folder, the build task builds the base .sln file](https://github.com/microsoft/vscode-dotnettools/issues/1701)
- [Unable to watch for changes in a large workspace folder](https://github.com/microsoft/vscode-dotnettools/issues/360)
 
## 1.20.35 - Release
### Added
- [Ability to rejoin a running apphost session after a project is rebuilt](https://github.com/microsoft/vscode-dotnettools/issues/2123)
  
### Fixed
- [Add Project Reference does not filter project list to exclude already referenced projects](https://github.com/microsoft/vscode-dotnettools/issues/1831)
- [Cannot add project reference via commands](https://github.com/microsoft/vscode-dotnettools/issues/1833)
- [Cannot use secret management by right clicking on .csproj under the solution explorer](https://github.com/microsoft/vscode-dotnettools/issues/509)
- [Build command should default to the current project as selection](https://github.com/microsoft/vscode-dotnettools/issues/2041)
- [Blank page when a WebAssembly project with Aspire is enabled and the application is restarted via the dashboard](https://github.com/microsoft/vscode-dotnettools/issues/2124)

## v1.19.63 - Release
### Added
- [Enable Test Platform log level](https://github.com/microsoft/vscode-dotnettools/issues/1979)
- SDK Path Consolidation 

### Fixed
- [DevKit Test Explorer sometimes shows tests and sometimes doesn't](https://github.com/microsoft/vscode-dotnettools/issues/1966)
- [Debugging test from test explorer fails](https://github.com/microsoft/vscode-dotnettools/issues/1922)
- [Error: Stream terminated before required bytes were read.](https://github.com/microsoft/vscode-dotnettools/issues/1910)
- [Dynamic Debug Configurations no longer picks up launchSettings.json](https://github.com/microsoft/vscode-dotnettools/issues/1899)
- [Testing with "Testing Platform Protocol" option hangs when debugging](https://github.com/microsoft/vscode-dotnettools/issues/1616)
- [Runsettings path is not read from .code-workspace settings.](https://github.com/microsoft/vscode-dotnettools/issues/1805)
- [C# Dev Kit is not showing compile errors-stale sln file](https://github.com/microsoft/vscode-dotnettools/issues/1083)
- [Test window update causes VSCode window to freeze for ~1 minute](https://github.com/microsoft/vscode-dotnettools/issues/1787)
- [Test Explorer doesn't work with unit test dll alone](https://github.com/microsoft/vscode-dotnettools/issues/1848)
- [Projects infinitely loading - couldn't find a debug adapter](https://github.com/microsoft/vscode-dotnettools/issues/1811)
- [Manage User Secrets opening in wrong folder on macOS](https://github.com/microsoft/vscode-dotnettools/issues/1880)
- [Cannot correctly locate dotnet (Linux)](https://github.com/microsoft/vscode-dotnettools/issues/637)
- [Solution explorer not showing](https://github.com/microsoft/vscode-dotnettools/issues/808)
- [Template list not rendering](https://github.com/microsoft/vscode-dotnettools/issues/1841)
- [Version greater than 1.16.6 does no longer open solutions files](https://github.com/microsoft/vscode-dotnettools/issues/1984)
- [Set Startup Project on latest code-insiders doesn't seem to stick](https://github.com/microsoft/vscode-dotnettools/issues/1986)
- [Blazorwasm debugging does not start browser with VS Code v1.100.0](https://github.com/microsoft/vscode-dotnettools/issues/2004)

## v1.19.60 - Prerelease
### Fixed
- [Version greater than 1.16.6 does no longer open solutions files](https://github.com/microsoft/vscode-dotnettools/issues/1984)
- [Set Startup Project on latest code-insiders doesn't seem to stick](https://github.com/microsoft/vscode-dotnettools/issues/1986)
- [Blazorwasm debugging does not start browser with VS Code v1.100.0](https://github.com/microsoft/vscode-dotnettools/issues/2004)

## v1.19.45 - Prerelease
### Added
- [Enable Test Platform log level](https://github.com/microsoft/vscode-dotnettools/issues/1979)
- SDK Path Consolidation 

### Fixed
- [DevKit Test Explorer sometimes shows tests and sometimes doesn't](https://github.com/microsoft/vscode-dotnettools/issues/1966)
- [Cannot correctly locate dotnet (Linux)](https://github.com/microsoft/vscode-dotnettools/issues/637)
- [Solution explorer not showing](https://github.com/microsoft/vscode-dotnettools/issues/808)
- [Template list not rendering](https://github.com/microsoft/vscode-dotnettools/issues/1841)

## v1.19.35 - Prerelease
### Fixed
- [Debugging test from test explorer fails](https://github.com/microsoft/vscode-dotnettools/issues/1922)
- [Error: Stream terminated before required bytes were read.](https://github.com/microsoft/vscode-dotnettools/issues/1910)
- [Dynamic Debug Configurations no longer picks up launchSettings.json](https://github.com/microsoft/vscode-dotnettools/issues/1899)
- [Testing with "Testing Platform Protocol" option hangs when debugging](https://github.com/microsoft/vscode-dotnettools/issues/1616)
- [Runsettings path is not read from .code-workspace settings.](https://github.com/microsoft/vscode-dotnettools/issues/1805)
- [C# Dev Kit is not showing compile errors-stale sln file](https://github.com/microsoft/vscode-dotnettools/issues/1083)

## v1.19.20 - Prerelease
### Fixed
- [Test window update causes VSCode window to freeze for ~1 minute](https://github.com/microsoft/vscode-dotnettools/issues/1787)

## v1.19.4 - Prerelease
### Fixed
- [Test Explorer doesn't work with unit test dll alone](https://github.com/microsoft/vscode-dotnettools/issues/1848)
- [Projects infinitely loading - couldn't find a debug adapter](https://github.com/microsoft/vscode-dotnettools/issues/1811)
- [Manage User Secrets opening in wrong folder on macOS](https://github.com/microsoft/vscode-dotnettools/issues/1880)

## v1.18.25 - Release
### Fixed
- [Solution Explorer doesn't show any projects](https://github.com/microsoft/vscode-dotnettools/issues/1919) 

## v1.18.23 - Release
### Added
- [Add Project Reference does not filter project list to exclude already referenced](https://github.com/microsoft/vscode-dotnettools/issues/1831)
- [Cannot add project reference via commands](https://github.com/microsoft/vscode-dotnettools/issues/1833)
- [Cannot remove project references in SE](https://github.com/microsoft/vscode-dotnettools/issues/462)
- [Cannot use secret manament by right clicking on .csproj under the solution explorer](https://github.com/microsoft/vscode-dotnettools/issues/509)
- Provide added context for GitHub Copilot completions integration

## v1.18.16 - Prerelease
### Fixed
- Fixes for 9.0.4 SDK

## v1.18.14 - Prerelease
### Added
- [Add Project Reference does not filter project list to exclude already referenced](https://github.com/microsoft/vscode-dotnettools/issues/1831)
- [Cannot add project reference via commands](https://github.com/microsoft/vscode-dotnettools/issues/1833)
- [Cannot remove project references in SE](https://github.com/microsoft/vscode-dotnettools/issues/462)
- [Cannot use secret manament by right clicking on .csproj under the solution explorer](https://github.com/microsoft/vscode-dotnettools/issues/509)
- Provide added context for GitHub Copilot completions integration

### Fixed
- [Microsoft.CodeAnalysis.LanguageServer client: couldn't create connection to server](https://github.com/dotnet/vscode-csharp/issues/8034)

## v1.17.64 - Release
### Added
- CSS Hot Reload for MAUI Blazor Hybrid is now in preview. To enable it, turn on the "[Experimental] Enables C# Hot Reload while debugging" setting.
- Move to .NET9

### Fixed
- Enhancements to the Solution-less workspace experience
- [Unit Tests are not discovered in Test Explorer by C# Dev Kit in version 1.10.16 to 1.13.9 inclusive](https://github.com/microsoft/vscode-dotnettools/issues/1644)
- [Testing: MSTest: .NET8 Tests projects that compiles fine are neither visible in Test Explorer, nor runnable](https://github.com/microsoft/vscode-dotnettools/issues/996)
- [Test diffing is broken](https://github.com/microsoft/vscode-dotnettools/issues/1604)
- [Testing with "Testing Platform Protocol" option hangs when debugging ](https://github.com/microsoft/vscode-dotnettools/issues/1616)
- [Tests not discovered](https://github.com/microsoft/vscode-dotnettools/issues/1751)
- ["Cancel Test Run" should stop running tests, even if being debugged](https://github.com/microsoft/vscode-dotnettools/issues/1786)
- [Test Explorer Does Not Refresh Tests](https://github.com/microsoft/vscode-dotnettools/issues/252)
- [go to references on method failed with System.InvalidOperationException: TypedConstant is an array. Use Values property](https://github.com/microsoft/vscode-dotnettools/issues/1731)

## v1.17.62 - Prerelease
### Fixed
- [go to references on method failed with System.InvalidOperationException: TypedConstant is an array. Use Values property](https://github.com/microsoft/vscode-dotnettools/issues/1731)

## v1.17.48 - Prerelease
### Fixed
- [Test diffing is broken](https://github.com/microsoft/vscode-dotnettools/issues/1604)
- [Testing with "Testing Platform Protocol" option hangs when debugging ](https://github.com/microsoft/vscode-dotnettools/issues/1616)
- [Tests not discovered](https://github.com/microsoft/vscode-dotnettools/issues/1751)
- ["Cancel Test Run" should stop running tests, even if being debugged](https://github.com/microsoft/vscode-dotnettools/issues/1786)
- [Test Explorer Does Not Refresh Tests](https://github.com/microsoft/vscode-dotnettools/issues/252)

## v1.17.27 - Prerelease
### Fixed
- bug fixes

## v1.17.12 - Prerelease
### Fixed
- Enhancements to the Solution-less workspace experience
- [Unit Tests are not discovered in Test Explorer by C# Dev Kit in version 1.10.16 to 1.13.9 inclusive](https://github.com/microsoft/vscode-dotnettools/issues/1644)
- [Testing: MSTest: .NET8 Tests projects that compiles fine are neither visible in Test Explorer, nor runnable](https://github.com/microsoft/vscode-dotnettools/issues/996)

### Added
- Move to .NET9

## v1.17.4 - Prerelease

### Fixed
- Bug fixes

### Added
- CSS Hot Reload for MAUI Blazor Hybrid is now in preview. To enable it, turn on the "[Experimental] Enables C# Hot Reload while debugging" setting.

## v1.16.6 - Release
### Fixed
- [Failed to find dotnet from path with "which dotnet"](https://github.com/microsoft/vscode-dotnettools/issues/1565)
- [.NET not found in PATH, but exists in my computer even I install the .NET SDK and set the PATH variable in VSCode](https://github.com/microsoft/vscode-dotnettools/issues/1621)

## v1.16.4 - Prerelease
### Fixed
- [Failed to find dotnet from path with "which dotnet"](https://github.com/microsoft/vscode-dotnettools/issues/1565)
- [.NET not found in PATH, but exists in my computer even I install the .NET SDK and set the PATH variable in VSCode](https://github.com/microsoft/vscode-dotnettools/issues/1621)

## v1.15.34 - Release

### Added
- Preview of dotnet.previewSolution-freeWorkspaceMode (Prevent automatic creation of solution files)

### Fixed
- [Cannot debug blazor web app](https://github.com/microsoft/vscode-dotnettools/issues/1692)
- [Default C# targets don't execute in /bin/Debug/net8.0/](https://github.com/microsoft/vscode-dotnettools/issues/1575)
- [Two users SSH remove Ubuntu, one will fail](https://github.com/microsoft/vscode-dotnettools/issues/1555)

## v1.15.32 - Prerelease
- Bug Fixes

### Added
- Preview of dotnet.previewSolution-freeWorkspaceMode (Prevent automatic creation of solution files)

## v1.15.13 - Prerelease

### Fixed
- [Cannot debug blazor web app](https://github.com/microsoft/vscode-dotnettools/issues/1692)
- [Two users SSH remove Ubuntu, one will fail](https://github.com/microsoft/vscode-dotnettools/issues/1555)

## v1.15.2 and v1.14.18 - Prerelease
This is the same release but missed the version bump so rereleased with the correct version number

### Fixed
- [Default C# targets don't execute in /bin/Debug/net8.0/](https://github.com/microsoft/vscode-dotnettools/issues/1575)

## v1.14.14 - Release
### Fixed
- [Test diffing is broken](https://github.com/microsoft/vscode-dotnettools/issues/1604)
- [C# Dev kit - File Permanently Deleted When Moved to Same Directory](https://github.com/microsoft/vscode-dotnettools/issues/1572)
- [Select Target Framework label is wrong](https://github.com/microsoft/vscode-dotnettools/issues/1540)
- [Unity messages classified as unused code](https://github.com/microsoft/vscode-dotnettools/issues/1587)
- [New Aspire Debug Session is stopped on an internal break from previous session](https://github.com/microsoft/vscode-dotnettools/issues/1652)

### Added
- [Implement Diagnostic log levels in Test Explorer](https://github.com/microsoft/vscode-dotnettools/issues/1580)

## v1.14.12 - Prerelease
### Fixed
- [Select Target Framework label is wrong](https://github.com/microsoft/vscode-dotnettools/issues/1540)
- [Unity messages classified as unused code](https://github.com/microsoft/vscode-dotnettools/issues/1587)

## v1.14.8 - Prerelease
### Fixed
- [Test diffing is broken](https://github.com/microsoft/vscode-dotnettools/issues/1604)
- [C# Dev kit - File Permanently Deleted When Moved to Same Directory](https://github.com/microsoft/vscode-dotnettools/issues/1572)

### Added
- [Implement Diagnostic log levels in Test Explorer](https://github.com/microsoft/vscode-dotnettools/issues/1580)

## v1.14.2 - Prerelease
- bug fixes

## v1.13.x - Release
### Added
- Run/Debug Azure Functions applications locally
- Preview Add Aspire Orchestration command

### Fixed
- [Select Target Framework label is wrong](https://github.com/microsoft/vscode-dotnettools/issues/1540)
- Telemetry fixes
- Bug fixes

## v1.13.6 - Prerelease (note: No v1.12.x release, skipped to v.1.13.x)
### Added
- Preview Add Aspire Orchestration command

### Fixed
- Run/Debug Azure Functions applications locally
- Telemetry fixes

## v1.12.37 - Prerelease
### Added
- Run/Debug Azure Functions applications locally

### Fixed
- Bug fixes

## v1.12.27 - Prerelease
### Fixed
-[Select Target Framework label is wrong](https://github.com/microsoft/vscode-dotnettools/issues/1540)

## v1.12.16 - Prerelease
- Bug fixes
- Telemetry fixes

## v1.12.2 - Prerelease
- Bug fixes

## v1.11.14 - Release
### Added
- [Use diff test explorer rendering on assert failures](https://github.com/microsoft/vscode-dotnettools/issues/1330)
- [Add support for call stacks in test failures](https://github.com/microsoft/vscode-dotnettools/issues/1294)

### Fixed
- [Request textDocument/hover failed.](https://github.com/microsoft/vscode-dotnettools/issues/1413)
- Bug fixes

## v1.11.11 - Prerelease
- Bug fixes

## v1.11.9 - Prerelease
### Added
- [Use diff test explorer rendering on assert failures](https://github.com/microsoft/vscode-dotnettools/issues/1330)
- [Add support for call stacks in test failures](https://github.com/microsoft/vscode-dotnettools/issues/1294)

## v1.11.6 - Prerelease
### Fixed
- [Request textDocument/hover failed.](https://github.com/microsoft/vscode-dotnettools/issues/1413)

## v1.11.4 - Prerelease
- Bug fixes

## v1.10.18 - Release
### Fixed
- [New v1.10.16 release stop finding my tests on test explorer](https://github.com/microsoft/vscode-dotnettools/issues/1436)

## v1.11.4 - Prerelease
- Bug fixes

## 1.10.16 - Release
### Added
- [Support new VS Code code coverage adapter/UI](https://github.com/microsoft/vscode-dotnettools/issues/1035)
- [Can't get code coverage when running tests via Test UI](https://github.com/microsoft/vscode-dotnettools/issues/554)

### Fixed
- [Devkit server fails to start with dotnet installed via snap](https://github.com/microsoft/vscode-dotnettools/issues/605)
- [Error when trying to add a new C# file with a hotkey](https://github.com/microsoft/vscode-dotnettools/issues/1334)
- [Go to Definition with SourceLink erroring with "Could not find a part of the path '/home/UserName/Documents/.dotnet/symbolcache/<FILENAME>"](https://github.com/microsoft/vscode-dotnettools/issues/1380)

## v1.10.12 - Prerelease
### Added
- [Support new VS Code code coverage adapter/UI](https://github.com/microsoft/vscode-dotnettools/issues/1035)

### Fixed
- [Error when trying to add a new C# file with a hotkey](https://github.com/microsoft/vscode-dotnettools/issues/1334)
- [Go to Definition with SourceLink erroring with "Could not find a part of the path '/home/UserName/Documents/.dotnet/symbolcache/<FILENAME>"](https://github.com/microsoft/vscode-dotnettools/issues/1380)

## v1.10.4 - Prerelease
- Bug fixes
- [Devkit server fails to start with dotnet installed via snap](https://github.com/microsoft/vscode-dotnettools/issues/605)

## v1.9.55 - Release
### Added
- Drag and drop files within Solution Explorer

### Fixed
- Improved Razor editing experience
- Fixed add new project scenario with workspace which removed all folders
- Enhanced project status information for clearer understanding of projects loaded
- The .NET Hot Reload output pane now uses "standard" VS Code log formatting, getting colorization and improved date/tag formatting
- [Track Active Item feature should be disabled when not in Explorer Panel](https://github.com/microsoft/vscode-dotnettools/issues/1117#issuecomment-2269994906)
- [Wrong project template](https://github.com/microsoft/vscode-dotnettools/issues/857)
- [Request textDocument/semanticTokens/range failed in .razor file in Blazor Web App](https://github.com/microsoft/vscode-dotnettools/issues/854)
- [Unable to start extension](https://github.com/microsoft/vscode-dotnettools/issues/1256)

## v1.9.53 - Prelease
### Added
- Drag and drop files within Solution Explorer

### Fixed
- Improved Razor editing experience
- Fixed add new project scenario with workspace which removed all folders
- Bug Fixes
- [Track Active Item feature should be disabled when not in Explorer Panel](https://github.com/microsoft/vscode-dotnettools/issues/1117#issuecomment-2269994906)
- [Blazor Components are seen a unknown](https://github.com/microsoft/vscode-dotnettools/issues/49)

## v1.9.8 - Prerelease
### Fixed
- Enhanced project status information for clearer understanding of projects loaded
- The .NET Hot Reload output pane now uses "standard" VS Code log formatting, getting colorization and improved date/tag formatting

## v1.9.2 - Prerelease
- Bug Fixes

## v1.8.14 - Release
### Added
- [Support new VS Code code coverage adapter/UI](https://github.com/microsoft/vscode-dotnettools/issues/1035)
- [Blazor Web App New Project - Missing Auto render mode project ](https://github.com/microsoft/vscode-dotnettools/issues/836)
- [C# Dev Kit for VS Code missing controller based API project template](https://github.com/microsoft/vscode-dotnettools/issues/404)
- New Command Palette command to set Build configuration ".NET: Select a Configuration"
- [Allow integration of 3rd party CLI Project Templates inside VS Code Create .NET Project option](https://github.com/microsoft/vscode-dotnettools/issues/715)
- [Create .Net project from 3rd party published .Net project templates](https://github.com/microsoft/vscode-dotnettools/issues/673)
- [Expose more templates and custom templates for project](https://github.com/microsoft/vscode-dotnettools/issues/115)
- [Include custom dotnet new templates in the Solution Explorer](https://github.com/microsoft/vscode-dotnettools/issues/48)

### Fixed
- [In version 1.7.25, the "Debug > Start new instance" command is broken](https://github.com/microsoft/vscode-dotnettools/issues/1195)
- [Activating the "Microsoft.VisualStudio.ProjectSystem.Query.Remoting.QueryExecutionService (0.2)" service failed.](https://github.com/microsoft/vscode-dotnettools/issues/840)
- [Ambiguous reference between ‘Plugin.BLE.Abstractions.Trace’ and ‘System.Diagnostics.Trace’](https://github.com/microsoft/vscode-dotnettools/issues/1204)
- [Problem with .NET: New Project](https://github.com/microsoft/vscode-dotnettools/issues/1153)
- [Wrong project template](https://github.com/microsoft/vscode-dotnettools/issues/857)
- [Creating new project does not do the same as dotnet new](https://github.com/microsoft/vscode-dotnettools/issues/684)

## v1.8.8 - Prerelease
### Fixed
- [In version 1.7.25, the "Debug > Start new instance" command is broken](https://github.com/microsoft/vscode-dotnettools/issues/1195)

## v1.7.27 - Release
### Fixed
- [In version 1.7.25, the "Debug > Start new instance" command is broken](https://github.com/microsoft/vscode-dotnettools/issues/1195)

## v1.8.2 - Prerelease
### Added
- New Command Palette command to set Build configuration ".NET: Select a Configuration"

### Fixed
- [Display test output for successful tests](https://github.com/microsoft/vscode-dotnettools/issues/989)

## v1.7.25 - Release

### Added
- (Preview) Improved Launch/Debug Experience
- (Experimental) Improved .NET New Project Experience

### Fixed
- [testhost is locking files during build](https://github.com/microsoft/vscode-dotnettools/issues/1089)

## v1.7.22 - Prerelease

### Fixed
- [testhost is locking files during build](https://github.com/microsoft/vscode-dotnettools/issues/1089)

## v1.7.4 - Prerelease
### Added
- (Experimental) Improved .NET New Project Experience

### Fixed
- Bug Fixes

## v1.7.2 - Prerelease
### Added
- (Preview) Improved Launch/Debug Experience

### Fixed
- Bug Fixes

## v1.6.8 - Release
### Added
- XAML IntelliSense for .NET MAUI
- Option to include prerelease packages in NuGet package operations

### Fixed
- Strengthening the .NET Aspire Run/Deb Experience
- [CSDevKit creating entries in my git folder](https://github.com/microsoft/vscode-dotnettools/issues/1008)
- [Disable Telemetry](https://github.com/microsoft/vscode-dotnettools/issues/985)
- [First 'Run without Debugging' starts a debug session.](https://github.com/microsoft/vscode-dotnettools/issues/1050)
- [CSDevKit creating entries in my git folder](https://github.com/microsoft/vscode-dotnettools/issues/1008)

## v1.6.2 - Prerelease
### Fixed
- Bug Fixes for .NET Aspire Run/Debug Experience

## v1.5.20 - Release
### Added
- [New SDK Acquisition Experience in Walkthrough](https://github.com/microsoft/vscode-dotnettools/issues/978)
- [Add, Update, and Remove NuGet Packages commands](https://github.com/microsoft/vscode-dotnettools/issues/62)
- Run/Debug .NET Aspire projects
- [Track active File in Solution Explorer command](https://github.com/microsoft/vscode-dotnettools/issues/51)

## v1.5.16 - Prerelease
### Added
- Enable New SDK Acquisition experience by default 

### Fixed
- .NET Aspire Run/Debug support for Mac/Linux

## v1.5.14 - Prerelease
### Fixed
- Bug fixes

## v1.5.12 - Prerelease
### Fixed
- Bug fixes

## v1.5.10 - Prerelease
### Added
- [New SDK Acquisition Experience in Walkthrough](https://github.com/microsoft/vscode-dotnettools/issues/978)

## v1.5.8 - Prerelease
### Added
- [Add, Update, and Remove NuGet Packages commands](https://github.com/microsoft/vscode-dotnettools/issues/62)
- Run/Debug .NET Aspire projects
- [Track active File in Solution Explorer command](https://github.com/microsoft/vscode-dotnettools/issues/51)

### Fixed
- Bug fixes

## v1.5.6 - Prerelease

### Fixed
- Bug fixes

## v1.5.4 - Prerelease

### Fixed
- Bug fixes

## v1.4.29 - Release

### Fixed
- Bug fixes

### Removed
- IntellICode extension from auto Downloading

## v1.4.28 - Prerelease

### Fixed
- Bug fixes

## v1.4.2 - Prerelease
### Fixed
- [Improve readme content for readability](https://github.com/microsoft/vscode-dotnettools/issues/761)

## v1.3.8 - Release
### Fixed
- Restarting debug session for Blazor WebAssembly projects
- [Hot Reload not working .NET 8 on WSL](https://github.com/microsoft/vscode-dotnettools/issues/794)
- [Deleting a TestMethod throws and error until Text Explorer refreshes tests ](https://github.com/microsoft/vscode-dotnettools/issues/661)
- [Add possibility to provide settings to define .runsettings or arguments for unit test execution with C# Dev Kit](https://github.com/microsoft/vscode-dotnettools/issues/156)
- [Add FAQ link to notification of project type support](https://github.com/microsoft/vscode-dotnettools/issues/710)

### Added
- Auto open Solution Explorer to increase discoverability

## v1.3.8 - Prerelease
- Bug fixes

## v1.3.6 - Prerelease
### Fixed
- Restarting debug session for Blazor WebAssembly projects

### Added
- Auto open Solution Explorer to increase discoverability


## v1.3.2 - Prerelease
### Fixed
- [Hot Reload not working .NET 8 on WSL](https://github.com/microsoft/vscode-dotnettools/issues/794)
- [Deleting a TestMethod throws and error until Text Explorer refreshes tests ](https://github.com/microsoft/vscode-dotnettools/issues/661)
- [Add possibility to provide settings to define .runsettings or arguments for unit test execution with C# Dev Kit](https://github.com/microsoft/vscode-dotnettools/issues/156)
- [Add FAQ link to notification of project type support](https://github.com/microsoft/vscode-dotnettools/issues/710)

## v1.2.7 - Release
### Fixed
- Enable open solution on explorer for easier discovery
- [Run and Debug ignores environmentVariables in launchSettings.json](https://github.com/microsoft/vscode-dotnettools/issues/608)
- [C# Web API cannot connect to the server](https://github.com/microsoft/vscode-dotnettools/issues/718)
- [Latest Pre-Release cannot debug Blazor Web App](https://github.com/microsoft/vscode-dotnettools/issues/696)
- [Debugger should default to integratedTerminal when launching a console app without debugging](https://github.com/microsoft/vscode-dotnettools/issues/606)
- [Restart debugging doesn't rebuild the project](https://github.com/microsoft/vscode-dotnettools/issues/496)

## v1.2.5 - Prerelease
### Fixed
- Enable open solution on explorer for easier discovery

## v1.2.2 - Prerelease
###Fixed
- [Run and Debug ignores environmentVariables in launchSettings.json](https://github.com/microsoft/vscode-dotnettools/issues/608)
- [C# Web API cannot connect to the server](https://github.com/microsoft/vscode-dotnettools/issues/718)
- [Latest Pre-Release cannot debug Blazor Web App](https://github.com/microsoft/vscode-dotnettools/issues/696)
- [Debugger should default to integratedTerminal when launching a console app without debugging](https://github.com/microsoft/vscode-dotnettools/issues/606)
- [Restart debugging doesn't rebuild the project](https://github.com/microsoft/vscode-dotnettools/issues/496)

## v1.1.16 - Release

### Fixed
- [Run and Debug ignores environmentVariables in launchSettings.json](https://github.com/microsoft/vscode-dotnettools/issues/608)
- Updated Readme to change the name of .NET Install Tool
- Added New Project template types to Create .NET Project list
- [Old tests shown after renaming test folder/project](https://github.com/microsoft/vscode-dotnettools/issues/645)
- [Configuring a logpoint including variables after a breakpoint breaks the debugging session](https://github.com/microsoft/vscode-dotnettools/issues/583)


## v1.1.5 - Prerelease

## Fixed
- Added New Project template types to Create .NET Project list

## v1.0.14 - Release

## Fixed
- [Debugger should better handle Console.Read APIs with internalConsole](https://github.com/dotnet/vscode-csharp/issues/5704)
- [Terminal should always appear on top of the debug console when using integrated terminal](https://github.com/dotnet/vscode-csharp/issues/6516)
- [Debugger should default to integratedTerminal when launching a console app without debugging](https://github.com/microsoft/vscode-dotnettools/issues/606)
- [A valid browser not found when debugging from Codespaces](https://github.com/microsoft/vscode-dotnettools/issues/560)
- [Getting started instructions should include restart device](https://github.com/microsoft/vscode-dotnettools/issues/268)


## v1.0.12 - Prerelease

## Fixed
- [Debugger should better handle Console.Read APIs with internalConsole](https://github.com/dotnet/vscode-csharp/issues/5704)
- [Terminal should always appear on top of the debug console when using integrated terminal](https://github.com/dotnet/vscode-csharp/issues/6516)
- [Debugger should default to integratedTerminal when launching a console app without debugging](https://github.com/microsoft/vscode-dotnettools/issues/606)
- [A valid browser not found when debugging from Codespaces](https://github.com/microsoft/vscode-dotnettools/issues/560)
- [Getting started instructions should include restart device](https://github.com/microsoft/vscode-dotnettools/issues/268)

## v1.0.4

 - Bug fixes and version number increase

## v0.6.3 - Prerelease

### Fixed
- Debugger improvements

## v0.5.150 - Release

### Added
- **Edit and Continue/Hot Reload Support** (Added support for Edit and Continue (EnC) and Hot Reload when debugging managed applications with the C# dev kit. Please note that EnC/Hot Reload debugging on Mac and Linux requires .NET 8 RC1 and above. In order to start using this feature, enable the csharp.experimental.debug.hotReload setting.)
- Localized Add Project tags
- Add support for listing projects in relative paths


### Fixed
- [Intellisense does not apply to new file until I restart VS Code](https://github.com/microsoft/vscode-dotnettools/issues/525)
- [Run button does not work](https://github.com/microsoft/vscode-dotnettools/issues/505)
- [Unable to watch for changes in this large workspace folder ](https://github.com/microsoft/vscode-dotnettools/issues/360)
- [.NET: Clean with terminal profile that already contains -Command parameter.](https://github.com/microsoft/vscode-dotnettools/issues/117)
- [Unable to apply environment variable settings from launchSettings.json](https://github.com/microsoft/vscode-dotnettools/issues/241)
- [Run without debugging does not launches browser for BlazorWasmApp](https://github.com/microsoft/vscode-dotnettools/issues/259)
- [.ts files do not appear in solution explorer with Microsoft.TypeScript.MSBuild](https://github.com/microsoft/vscode-dotnettools/issues/412)
- [Add new file failed from Solution Explorer](https://github.com/microsoft/vscode-dotnettools/issues/500)
- [Variable $(SolutionDir) is *Undefined* during build events](https://github.com/microsoft/vscode-dotnettools/issues/114)
- [Adding Razor page doesn't use correct default namespace or ask me for name of page](https://github.com/microsoft/vscode-dotnettools/issues/95)
- [Tests are removed from test explorer after changes to test files](https://github.com/microsoft/vscode-dotnettools/issues/460)
- [Add new file failed from Solution Explorer.](https://github.com/microsoft/vscode-dotnettools/issues/500)
- [Problem if Solution is not in same directory as main csproj](https://github.com/microsoft/vscode-dotnettools/issues/537)

## v0.5.127 - Prerelease

### Fixed
- [Tests are removed from test explorer after changes to test files](https://github.com/microsoft/vscode-dotnettools/issues/460)
- [Add new file failed from Solution Explorer.](https://github.com/microsoft/vscode-dotnettools/issues/500)
- [Problem if Solution is not in same directory as main csproj](https://github.com/microsoft/vscode-dotnettools/issues/537)

## v0.5.98 - Prerelease

### Fixed
- [Intellisense does not apply to new file until I restart VS Code](https://github.com/microsoft/vscode-dotnettools/issues/525)
- [Run button does not work](https://github.com/microsoft/vscode-dotnettools/issues/505)
- [Unable to watch for changes in this large workspace folder ](https://github.com/microsoft/vscode-dotnettools/issues/360)
- [.NET: Clean with terminal profile that already contains -Command parameter.](https://github.com/microsoft/vscode-dotnettools/issues/117)

## v0.5.47 - Prerelease

### Added
- Localized Add Project tags

### Fixed
- [Unable to apply environment variable settings from launchSettings.json](https://github.com/microsoft/vscode-dotnettools/issues/241)
- [Run without debugging does not launches browser for BlazorWasmApp](https://github.com/microsoft/vscode-dotnettools/issues/259)
- [.ts files do not appear in solution explorer with Microsoft.TypeScript.MSBuild](https://github.com/microsoft/vscode-dotnettools/issues/412)
- [Add new file failed from Solution Explorer](https://github.com/microsoft/vscode-dotnettools/issues/500)

## v0.5.24 - Prerelease

### Fixed
- Mac specific project build/load issues

## v0.5.2 - Prerelease

### Added
- Add support for listing projects in relative paths
- Hot Reload support

### Fixed
- [Variable $(SolutionDir) is *Undefined* during build events](https://github.com/microsoft/vscode-dotnettools/issues/114)
- [Adding Razor page doesn't use correct default namespace or ask me for name of page](https://github.com/microsoft/vscode-dotnettools/issues/95)

## v0.4.10 - Release

### Added
- [Add Project Reference in Solution Explorer](https://github.com/microsoft/vscode-dotnettools/issues/47)

### Fixed
- [Output window is opened automatically every time VS Code starts](https://github.com/microsoft/vscode-dotnettools/issues/192)
- [Walkthrough fails to launch on mac](https://github.com/microsoft/vscode-dotnettools/issues/155)
- [C# Dev Kit still Downloading the .NET Runtime when i Using configured](https://github.com/microsoft/vscode-dotnettools/issues/396)
- [Run all tests in file in multi-targeted project builds entire solution](https://github.com/microsoft/vscode-dotnettools/issues/265)
- [Test Explorer Not Working for projects with alternate outputs specified](https://github.com/microsoft/vscode-dotnettools/issues/28)
- [Test Explorer displaying duplicate tests](https://github.com/microsoft/vscode-dotnettools/issues/217)
- [No C# project is currently loaded when .NET 8 Preview 7 is present](https://github.com/microsoft/vscode-dotnettools/issues/318)
- [.NET Server crashing with CultureNotFoundException](https://github.com/microsoft/vscode-dotnettools/issues/386)
- [Improved reliability of .NET runtime discovery on startup](https://github.com/microsoft/vscode-dotnettools/issues/332)
- Ensure empty folders stay in project after last file is deleted
- [Throw error message for invalid characters in file name](https://github.com/microsoft/vscode-dotnettools/issues/280)

## v0.4.8 - Prerelease

### Fixed
- Ensure empty folders stay in project after last file is deleted
- [Throw error message for invalid characters in file name](https://github.com/microsoft/vscode-dotnettools/issues/280)

## v0.4.6 - Prerelease

### Fixed
- [.NET Server crashing with CultureNotFoundException](https://github.com/microsoft/vscode-dotnettools/issues/386)
- [Improved reliability of .NET runtime discovery on startup](https://github.com/microsoft/vscode-dotnettools/issues/332)

## v0.4.5 - Prerelease

### Fixed
- [Walkthrough fails to launch on mac](https://github.com/microsoft/vscode-dotnettools/issues/155)
- [C# Dev Kit still Downloading the .NET Runtime when i Using configured](https://github.com/microsoft/vscode-dotnettools/issues/396)
- [Run all tests in file in multi-targeted project builds entire solution](https://github.com/microsoft/vscode-dotnettools/issues/265)
- [Test Explorer Not Working for projects with alternate outputs specified](https://github.com/microsoft/vscode-dotnettools/issues/28)
- [Test Explorer displaying duplicate tests](https://github.com/microsoft/vscode-dotnettools/issues/217)
- [No C# project is currently loaded when .NET 8 Preview 7 is present](https://github.com/microsoft/vscode-dotnettools/issues/318)

## v0.4.2 - Prerelease

### Added
- [Add Project Reference in Solution Explorer](https://github.com/microsoft/vscode-dotnettools/issues/47)

### Fixed
- [Output window is opened automatically every time VS Code starts](https://github.com/microsoft/vscode-dotnettools/issues/192)

## v0.3.21 - Release

### Fixed
- Improvements in Test Discovery
- [Don't know how to launch profile 'IIS Express'; the commandName 'IISExpress' is not supported. #90](https://github.com/microsoft/vscode-dotnettools/issues/90)
- [Improve license details (subscription) in README #134](https://github.com/microsoft/vscode-dotnettools/issues/134)
- [Enabling solution wide analysis attempts to compute diagnostics for non-source files #209](https://github.com/microsoft/vscode-dotnettools/issues/209)
- [Intellisense and syntrax highlighting intermittent when C# Dev Kit is installed #210](https://github.com/microsoft/vscode-dotnettools/issues/210)

## v0.3.18 - Prerelease

- Bug fixes

## v0.3.5 - Prerelease

### Fixed

- Removing system assemblies from some of the project system server package
- Improvements in Test Discovery
- Improvements in telemetry

## v0.3.2 - Prerelease

### Fixed
- [Don't know how to launch profile 'IIS Express'; the commandName 'IISExpress' is not supported. #90](https://github.com/microsoft/vscode-dotnettools/issues/90)
- [Improve license details (subscription) in README #134](https://github.com/microsoft/vscode-dotnettools/issues/134)
- [Enabling solution wide analysis attempts to compute diagnostics for non-source files #209](https://github.com/microsoft/vscode-dotnettools/issues/209)
- [Intellisense and syntrax highlighting intermittent when C# Dev Kit is installed #210](https://github.com/microsoft/vscode-dotnettools/issues/210)

## v0.2.100 - Release

### Fixed
- Improved Readme text
- Reduce Logging
- Support spacing in paths

## v0.2.99 - Prerelease

- Bug fixes

## v0.2.70 - Prerelease

- Bug fixes

## v0.1.103 - Release

- Bug fixes

## v0.1.83 - Release

- Initial Release