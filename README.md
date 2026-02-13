# CopilotHub

A Windows desktop application that aggregates multiple GitHub Copilot CLI sessions into a single tabbed interface.

## Features

- **Multi-session tabs** — Run multiple Copilot sessions simultaneously in different repos
- **Live output streaming** — See Copilot responses in real time
- **Model selection** — Choose your preferred model (claude-opus-4.6, gpt-5, etc.)
- **File change tracking** — Automatically detects files modified by Copilot
- **Side-by-side diff viewer** — Click any modified file to see the diff against HEAD
- **Embedded terminal** — Run shell commands (git, dotnet, etc.) per session
- **Toast notifications** — Get notified when sessions complete
- **Session isolation** — Each tab has its own Copilot process, terminal, and file watcher

## Requirements

- Windows 10/11
- [GitHub Copilot CLI](https://docs.github.com/en/copilot/github-copilot-in-the-cli) installed and authenticated

## Download

Grab the latest `CopilotHub.App.exe` from [Releases](../../releases).

## Build from Source

```bash
git clone https://github.com/fsfh60/CopilotHub.git
cd CopilotHub
dotnet build
dotnet run --project CopilotHub.App
```

## Publish Self-Contained Executable

```bash
dotnet publish CopilotHub.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Architecture

| Project | Role |
|---|---|
| `CopilotHub.App` | WPF UI (MVVM with CommunityToolkit.Mvvm) |
| `CopilotHub.Core` | Domain models and service interfaces |
| `CopilotHub.Infrastructure` | External adapters (Git, CLI, FileSystem, Notifications) |
| `CopilotHub.Tests` | Unit tests (xUnit + FluentAssertions) |

## Tech Stack

- .NET 10 / WPF
- CommunityToolkit.Mvvm
- LibGit2Sharp
- DiffPlex
- Serilog
- Microsoft.Toolkit.Uwp.Notifications

## License

MIT
