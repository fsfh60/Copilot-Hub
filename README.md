# CopilotHub

A Windows desktop application that aggregates multiple GitHub Copilot CLI sessions into a single tabbed interface.

![Windows](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)
![.NET 10](https://img.shields.io/badge/.NET-10.0-purple)
![License](https://img.shields.io/badge/license-MIT-green)
[![Release](https://img.shields.io/github/v/release/fsfh60/Copilot-Hub)](https://github.com/fsfh60/Copilot-Hub/releases/latest)

## Features

- **Multi-session tabs** — Run multiple Copilot sessions simultaneously in different repos
- **Live output streaming** — See Copilot responses in real time
- **Model selection** — Choose your preferred model (claude-opus-4.6, gpt-5, etc.)
- **File change tracking** — Automatically detects files modified by Copilot
- **Inline file editor** — Click any modified file to open and edit it
- **Side-by-side diff viewer** — Double-click any modified file to see the diff against HEAD
- **Embedded terminal** — Run shell commands (git, dotnet, etc.) per session
- **Toast notifications** — Get notified when sessions complete
- **Session isolation** — Each tab has its own Copilot process, terminal, and file watcher
- **Theme detection** — Automatically matches your Windows light/dark theme

---

## Installation

### Quick Install (Recommended)

Open PowerShell and run:

```powershell
irm https://raw.githubusercontent.com/fsfh60/Copilot-Hub/main/install.ps1 | iex
```

This will:
- Download the latest release
- Verify SHA256 checksum
- Install to `%LOCALAPPDATA%\CopilotHub`
- Add to your PATH
- Create a desktop shortcut

### Direct Download

**Windows x64 (most common):**
```powershell
curl -L -o CopilotHub.exe https://github.com/fsfh60/Copilot-Hub/releases/latest/download/CopilotHub.exe
```

**Windows ARM64:**
```powershell
# Replace TAG with the version, e.g. v1.2.0
curl -L -o CopilotHub.exe https://github.com/fsfh60/Copilot-Hub/releases/latest/download/CopilotHub-TAG-windows-arm64.exe
```

**Using wget:**
```bash
wget https://github.com/fsfh60/Copilot-Hub/releases/latest/download/CopilotHub.exe
```

### Linux / macOS (WSL or Wine)

```bash
curl -fsSL https://raw.githubusercontent.com/fsfh60/Copilot-Hub/main/install.sh | bash
```

> **Note:** CopilotHub is a WPF desktop app and requires Windows. On Linux, use Wine or WSL with Windows interop.

### Manual Download

Go to the [Releases](https://github.com/fsfh60/Copilot-Hub/releases/latest) page and download:

| File | Description |
|------|-------------|
| `CopilotHub.exe` | Standalone executable (Windows x64) |
| `CopilotHub-vX.Y.Z-windows-x64.zip` | Zip archive (Windows x64) |
| `CopilotHub-vX.Y.Z-windows-arm64.exe` | Standalone executable (Windows ARM64) |
| `checksums-sha256.txt` | SHA256 checksums for verification |

### Verify Download

```powershell
# Check the hash
(Get-FileHash CopilotHub.exe -Algorithm SHA256).Hash

# Compare with published checksums
curl -L https://github.com/fsfh60/Copilot-Hub/releases/latest/download/checksums-sha256.txt
```

---

## Prerequisites

- **Windows 10 or 11**
- **[GitHub Copilot CLI](https://docs.github.com/en/copilot/github-copilot-in-the-cli)** installed and authenticated
  ```powershell
  winget install GitHub.CopilotCLI
  copilot auth login
  ```

---

## Usage

1. Launch `CopilotHub.exe`
2. Click **＋ New Session** to create a Copilot session
3. Choose a working directory, model, and extra arguments
4. Type prompts and press **Enter** to send (Shift+Enter for newline)
5. Modified files appear in the right panel — click to edit, double-click for diff
6. Use the embedded terminal at the bottom for git/shell commands

---

## Build from Source

```bash
git clone https://github.com/fsfh60/Copilot-Hub.git
cd Copilot-Hub
dotnet build
dotnet run --project CopilotHub.App
```

### Run Tests

```bash
dotnet test
```

### Publish Self-Contained Executable

```bash
dotnet publish CopilotHub.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

---

## Releases

Releases are fully automated. When a new tag is pushed:

```bash
git tag v1.2.0
git push origin v1.2.0
```

GitHub Actions will automatically:
1. Run all tests
2. Build for Windows x64 and ARM64
3. Generate SHA256 checksums
4. Create a GitHub Release with all assets

---

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
