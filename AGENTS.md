# Repository Guidelines

## Project Structure & Module Organization
`KindomDataAPIServer.sln` contains two .NET Framework 4.8 projects:
- `KindomDataAPIServer/`: main WPF desktop client plus OWIN self-hosted Web API. Core areas include `Controllers/`, `DataService/`, `KindomAPI/`, `Models/`, `ViewModels/`, `Views/`, and `Common/`.
- `Tet.GeoSymbol/`: shared geology-symbol library and WPF UI helpers.

Runtime assets live beside the projects: `packages/` for restored NuGet and vendor DLLs, `docs/` and `Documentation/` for reference material, and `KindomDataAPIServer/WebTest/` for manual API smoke-test pages.

## Build, Test, and Development Commands
Run commands from the repository root in a Visual Studio 2022 Developer PowerShell.

```powershell
nuget restore KindomDataAPIServer.sln
msbuild KindomDataAPIServer.sln /p:Configuration=Debug /p:Platform="Any CPU"
msbuild KindomDataAPIServer.sln /p:Configuration=Release /p:Platform=ARM64
.\bin\Debug\KindomDataAPIServer.exe
```

`nuget restore` restores `packages.config` dependencies. The Debug build is the safest local workflow; Release maps the symbol library to its x64 release configuration. Launch the generated EXE to start the WPF shell and local API host.

## Coding Style & Naming Conventions
Use 4-space indentation and standard C# brace style. Keep public types, properties, and methods in `PascalCase`; use `camelCase` for locals and parameters. Match existing WPF naming: `ViewName.xaml` with `ViewName.xaml.cs`, and `*ViewModel.cs` for presentation logic. Keep namespaces under `KindomDataAPIServer.*` or `Tet.GeoSymbol.*`. No formatter or linter is checked in, so use Visual Studio format-on-save and keep edits consistent with nearby files.

## Testing Guidelines
There is no automated test project in this solution today. Validate changes by:
- building the solution cleanly;
- exercising affected API endpoints through `KindomDataAPIServer/WebTest/*.html` or an HTTP client;
- checking the corresponding WPF view when UI or configuration code changes.

If you add tests, keep them in a separate `*.Tests` project and name methods after the behavior being verified.

## Commit & Pull Request Guidelines
Recent history uses short summaries such as `fix` and `添加文档`. Prefer a slightly clearer imperative subject, for example `Fix well export null handling` or `Update WebTest sample payload`. Keep commits scoped to one concern.

PRs should include a concise description, affected modules, manual verification steps, and screenshots for WPF UI changes. Call out updates to `App.config`, `.reg` files, copied DLLs, or JSON files under `Configs/` because they change runtime behavior.
