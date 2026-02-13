#!/bin/bash
# CopilotHub Install Script
# Usage: curl -fsSL https://raw.githubusercontent.com/fsfh60/Copilot-Hub/main/install.sh | bash
#
# NOTE: CopilotHub is a Windows desktop (WPF) application.
# This script downloads the exe for use with Wine or on WSL with Windows interop.

set -euo pipefail

REPO="fsfh60/Copilot-Hub"
INSTALL_DIR="${HOME}/.local/bin"

echo ""
echo "  ╔══════════════════════════════════════╗"
echo "  ║       CopilotHub Installer           ║"
echo "  ╚══════════════════════════════════════╝"
echo ""

# Detect OS
OS=$(uname -s | tr '[:upper:]' '[:lower:]')
ARCH=$(uname -m)

case "$ARCH" in
    x86_64|amd64) ARCH="x64" ;;
    aarch64|arm64) ARCH="arm64" ;;
    *) echo "  ERROR: Unsupported architecture: $ARCH"; exit 1 ;;
esac

echo "  Detected: $OS/$ARCH"

if [ "$OS" != "linux" ] && [ "$OS" != "darwin" ]; then
    echo "  ERROR: Unsupported OS. Use install.ps1 for Windows."
    exit 1
fi

echo ""
echo "  ⚠  CopilotHub is a Windows desktop (WPF) application."
echo "     This will download the .exe for use with Wine or WSL interop."
echo ""

# Get latest release tag
echo "  Fetching latest release..."
TAG=$(curl -fsSL "https://api.github.com/repos/$REPO/releases/latest" | grep '"tag_name"' | sed -E 's/.*"tag_name": *"([^"]+)".*/\1/')

if [ -z "$TAG" ]; then
    echo "  ERROR: Could not determine latest release."
    exit 1
fi

echo "  Latest version: $TAG"

# Determine asset
if [ "$ARCH" = "x64" ]; then
    ASSET_NAME="CopilotHub.exe"
else
    ASSET_NAME="CopilotHub-${TAG}-windows-arm64.exe"
fi

DOWNLOAD_URL="https://github.com/$REPO/releases/download/$TAG/$ASSET_NAME"
CHECKSUM_URL="https://github.com/$REPO/releases/download/$TAG/checksums-sha256.txt"

# Create install directory
mkdir -p "$INSTALL_DIR"

DEST="$INSTALL_DIR/CopilotHub.exe"

# Download
echo "  Downloading $ASSET_NAME..."
curl -fSL -o "$DEST" "$DOWNLOAD_URL"

# Verify checksum
echo "  Verifying checksum..."
CHECKSUMS=$(curl -fsSL "$CHECKSUM_URL" 2>/dev/null || true)
if [ -n "$CHECKSUMS" ]; then
    EXPECTED=$(echo "$CHECKSUMS" | grep "$ASSET_NAME" | awk '{print $1}')
    if [ -n "$EXPECTED" ]; then
        ACTUAL=$(sha256sum "$DEST" | awk '{print $1}')
        if [ "$ACTUAL" = "$EXPECTED" ]; then
            echo "  ✓ SHA256 checksum verified"
        else
            echo "  ✗ SHA256 mismatch!"
            echo "    Expected: $EXPECTED"
            echo "    Got:      $ACTUAL"
            rm -f "$DEST"
            exit 1
        fi
    fi
else
    echo "  Warning: Could not download checksums, skipping verification"
fi

chmod +x "$DEST"

echo ""
echo "  ✓ CopilotHub $TAG installed to $DEST"
echo ""
echo "  To run (WSL with Windows interop):"
echo "    cmd.exe /c CopilotHub.exe"
echo ""
echo "  To run (Wine):"
echo "    wine $DEST"
echo ""
