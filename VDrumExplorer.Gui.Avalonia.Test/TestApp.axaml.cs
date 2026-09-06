// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Avalonia;
using Avalonia.Markup.Xaml;

namespace VDrumExplorer.Gui.Avalonia.Test;

/// <summary>
/// Test-only application for headless visual tests. Loads the same styles and
/// application resources as the production <c>App</c> (FluentTheme, RegularScaling
/// styles, margins and the KeyValueItemTemplate) so views render exactly as they
/// do in production, but deliberately does nothing in
/// <see cref="OnFrameworkInitializationCompleted"/>: the production App creates the
/// main window, sets up MIDI hardware and starts module detection, none of which is
/// desirable in a headless test environment.
/// </summary>
public class TestApp : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Do NOT create windows or set up MIDI - just complete initialization.
        base.OnFrameworkInitializationCompleted();
    }
}
