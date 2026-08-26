#!/bin/bash
set -e
cd "$(dirname "$0")"
echo "Building VDrumExplorer Avalonia GUI..."
dotnet build VDrumExplorer.Gui.Avalonia/VDrumExplorer.Gui.Avalonia.csproj
echo "Launching VDrumExplorer..."
dotnet run --project VDrumExplorer.Gui.Avalonia/VDrumExplorer.Gui.Avalonia.csproj --no-build
