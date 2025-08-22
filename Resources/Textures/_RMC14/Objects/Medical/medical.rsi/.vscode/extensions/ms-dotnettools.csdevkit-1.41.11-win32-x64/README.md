# C# Dev Kit for Visual Studio Code
C# Dev Kit helps you manage your code with a solution explorer and test your code with integrated unit test discovery and execution, elevating your C# development experience wherever you like to develop (Windows, macOS, Linux, and even in a Codespace).

This extension builds on top of the great C# language capabilities provided by the [C# extension][CSharpExtension] and enhances your C# environment by adding a set of powerful tools and utilities that integrate natively with VS Code to help C# developers write, debug, and maintain their code faster and with fewer errors. Some of this new tooling includes but is not limited to:
* C# project and solution management via an integrated solution explorer
* Native testing environment to run and debug tests using the Test Explorer
* Roslyn-powered language service for best in-class C# language features such as code navigation, refactoring, semantic awareness, and more

## Quick Start
1. Install C# Dev Kit (The [C# extension][CSharpExtension] and the [.NET Install Tool](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.vscode-dotnet-runtime) will automatically be installed).
2. Open a folder/workspace that contains a C# project (.csproj) and the extension will activate, or use the ".NET: New Project..." command to create a new C# project.
3. Check out the [C# Getting Started](https://code.visualstudio.com/docs/csharp/get-started) documentation to learn more!

## Feature List & Walkthrough
* [Project System / Solution Explorer](https://code.visualstudio.com/docs/csharp/project-management)
  * Solution Node Actions
  * Add Project
  * [Build/Run Project](https://code.visualstudio.com/docs/csharp/build-tools)
* Code Editing (Uses the [C# extension][CSharpExtension])
  * [Refactoring](https://code.visualstudio.com/docs/csharp/refactoring)
  * [Code Navigation (Go To Definition/References)](https://code.visualstudio.com/docs/csharp/navigate-edit)
  * [Code Completions](https://code.visualstudio.com/docs/csharp/intellicode)
  * Roslyn-powered semantic awareness
* [Package Management](https://code.visualstudio.com/docs/csharp/package-management)
  * Automatic NuGet Restore
* [Debugging](https://code.visualstudio.com/docs/csharp/debugging)
* [Testing](https://code.visualstudio.com/docs/csharp/testing)
  * Discover, Run, and Debug Tests

## Requirements
* [.NET SDK](https://dotnet.microsoft.com/download)

## Features
### Manage your projects with a new solution view
C# Dev Kit extension enhances VS Code's existing Workspaces with a new Solution Explorer view, providing a curated and structured view of your application for effortless, central project management.  This lets you quickly add new projects or files to your solutions and easily build all or part of your solution.

![Animation showing C# Dev Kit's add existing project feature](https://github.com/microsoft/vscode-dotnettools/blob/main/docs/media/07-add.existing.project.gif?raw=true)

### Test your projects with expanded Test Explorer capabilities
With C# Dev Kit, your tests in XUnit, NUnit, MSTest and bUnit will be discovered and organized for you more easily for fast execution and results navigation. The extension also makes VS Code's Command Palette testing commands easily available for debugging and running your tests.

 ![Animation showing C# Dev Kit's Test Explorer integration](https://github.com/microsoft/vscode-dotnettools/blob/main/docs/media/TestRunning.gif?raw=true)

## Installed Extensions
C# Dev Kit will automatically install the [C# extension][CSharpExtension] and [.NET Install Tool](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.vscode-dotnet-runtime) to give you the best experience when working with C#. 

## Write your project faster with AI-powered C# development
The [IntelliCode for C# Dev Kit][vscodeintellicode-csharpExtension] extension is part of C# Dev Kit family of extensions and enhances the AI-assisted support beyond the basic [IntelliSense](https://code.visualstudio.com/docs/editor/intellisense) code-completion found in the existing C# extension.  It includes powerful IntelliCode features such as whole-line completions and starred suggestions based on your personal codebase. To take advantage of this functionality, you will need to install [IntelliCode for C# Dev Kit][vscodeintellicode-csharpExtension].

## Working with MAUI or Unity
C# Dev Kit family of extensions is great for all cloud native development. Working with mobile (MAUI) or Unity does require the addition of other extensions that provide functionality specific to their unique components.

For MAUI development, install the [.NET MAUI Extension][MAUIExtension].
For Unity development, install the [Unity Extension](https://marketplace.visualstudio.com/items?itemName=VisualStudioToolsForUnity.vstuc).

When working with Unity, if you see this error: "The project file 'Assembly-CSharp.csproj' is in unsupported format", please update the "Visual Studio Editor" version to 2.0.20 from the Unity Package Manager.

## Learn more
Explore all the features the C# extension has to offer by looking for .NET in the Command Palette. For more information on these features, refer to the [documentation pages](https://code.visualstudio.com/docs/csharp/get-started).

For learning materials on C# and .NET, check out the following resources:

- [Learn to program using C#](https://aka.ms/csharp-certification)
- [Learn to build front-end web applications](https://dotnet.microsoft.com/en-us/learn/front-end-web-dev)
- [Learn to build back-end web applications](https://dotnet.microsoft.com/en-us/learn/back-end-web-dev)

## Found a bug?
To file a new issue, go to VS Code Help > Report Issue. In the popup UI, make sure to select "An extension" from the dropdown for "File on" and select "C# Dev Kit" for the extension dropdown. Submitting this form will automatically generate a new issue on the .NET Tools GitHub.

Alternatively, you file an issue directly on the [.NET Tools GitHub Repo](https://github.com/microsoft/vscode-dotnettools).

## Feedback
[Provide feedback](https://github.com/microsoft/vscode-dotnettools) File questions, issues, or feature requests for the extension.

[Known issues](https://github.com/microsoft/vscode-dotnettools/issues) If someone has already filed an issue that encompasses your feedback, please leave a 👍 or 👎 reaction on the issue to upvote or downvote it to help us prioritize the issue.

[Quick survey](https://www.research.net/r/8KGJ9V8?o=[o_value]&v=[v_value]&m=[m_value])  Let us know what you think of the extension by taking the quick survey.

## License
C# Dev Kit builds on the same foundations as Visual Studio for some of its functionality, it uses the same license model as Visual Studio. This means it's free for individuals, as well as academia and open-source development, the same terms that apply to Visual Studio Community. For organizations, the C# Dev Kit is included with Visual Studio Professional and Enterprise subscriptions, as well as GitHub Codespaces. **For full terms and details see the [license terms](https://aka.ms/vs/csdevkit/license)**.

[CSharpExtension]: https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp
[vscodeintellicode-csharpExtension]: https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.vscodeintellicode-csharp
[MAUIExtension]: https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-maui

## Data/Telemetry
VS Code collects usage data and sends it to Microsoft to help improve our products and services. Read our [privacy statement](https://privacy.microsoft.com/en-us/privacystatement) to learn more. If you don't wish to send usage data to Microsoft, you can set the telemetry.telemetryLevel setting to "off". Learn more in our [FAQ](https://code.visualstudio.com/docs/supporting/faq#_how-to-disable-telemetry-reporting).
