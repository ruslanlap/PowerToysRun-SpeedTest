# Community.PowerToys.Run.Plugin.Templates

[![build](https://github.com/hlaueriksson/Community.PowerToys.Run.Plugin.Templates/actions/workflows/build.yml/badge.svg)](https://github.com/hlaueriksson/Community.PowerToys.Run.Plugin.Templates/actions/workflows/build.yml)
[![Snyk Security Scan](https://github.com/hlaueriksson/Community.PowerToys.Run.Plugin.Templates/actions/workflows/snyk.yml/badge.svg)](https://github.com/hlaueriksson/Community.PowerToys.Run.Plugin.Templates/actions/workflows/snyk.yml)
[![Community.PowerToys.Run.Plugin.Templates](https://img.shields.io/nuget/v/Community.PowerToys.Run.Plugin.Templates.svg?label=Community.PowerToys.Run.Plugin.Templates)](https://www.nuget.org/packages/Community.PowerToys.Run.Plugin.Templates)
[![Mentioned in Awesome PowerToys Run Plugins](https://awesome.re/mentioned-badge.svg)](https://github.com/hlaueriksson/awesome-powertoys-run-plugins)

These `dotnet new` templates simplify creating PowerToys Run plugin projects and solutions.

![dotnet new list PowerToys](https://raw.githubusercontent.com/hlaueriksson/Community.PowerToys.Run.Plugin.Templates/main/terminal.png)

## Installation

Install:

```cmd
dotnet new install Community.PowerToys.Run.Plugin.Templates
```

Update installed templates:

```cmd
dotnet new update
```

Uninstall:

```cmd
dotnet new uninstall Community.PowerToys.Run.Plugin.Templates
```

## Usage

Help:

```cmd
dotnet new ptrun-sln --help
dotnet new ptrun-proj --help
dotnet new ptrun-scripts --help
```

Create a solution in the output directory `MyPlugin`:

```cmd
dotnet new ptrun-sln -o MyPlugin
```

Create a directory and then a solution:

```cmd
mkdir MyPlugin
cd MyPlugin
dotnet new ptrun-sln
```

Create a project in the output directory `Community.PowerToys.Run.Plugin.MyPlugin`:

```cmd
dotnet new ptrun-proj -o Community.PowerToys.Run.Plugin.MyPlugin
```

Plugin author:

```cmd
dotnet new ptrun-sln --plugin-author octocat
```

Create scripts in the current directory:

```cmd
dotnet new ptrun-scripts
```

Visual Studio:

![Visual Studio - Create a new project](https://raw.githubusercontent.com/hlaueriksson/Community.PowerToys.Run.Plugin.Templates/main/vs-new-project.png)

## Output

With `ptrun-sln`:

```
MyPlugin
|   MyPlugin.sln
|   
+---Community.PowerToys.Run.Plugin.MyPlugin
|   |   Community.PowerToys.Run.Plugin.MyPlugin.csproj
|   |   Main.cs
|   |   plugin.json
|   |   
|   \---Images
|           myplugin.dark.png
|           myplugin.light.png
|           
\---Community.PowerToys.Run.Plugin.MyPlugin.UnitTests
        Community.PowerToys.Run.Plugin.MyPlugin.UnitTests.csproj
        MainTests.cs
```

![Visual Studio](https://raw.githubusercontent.com/hlaueriksson/Community.PowerToys.Run.Plugin.Templates/main/vs.png)

With `ptrun-proj`:

```
Community.PowerToys.Run.Plugin.MyPlugin
|   Community.PowerToys.Run.Plugin.MyPlugin.csproj
|   Main.cs
|   plugin.json
|   
\---Images
        myplugin.dark.png
        myplugin.light.png
```

With `ptrun-scripts`:

```
Community.PowerToys.Run.Plugin.MyPlugin
|   deploy.ps1
|   pack.ps1
|   releasenotes.ps1
```

## Disclaimer

These are not official Microsoft PowerToys templates.
