// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Avalonia;
using Avalonia.Headless;
using Avalonia.Media;
// Required so the assembly-level attribute below can resolve TestAppBuilder: assembly
// attributes only see the file's using directives and the global namespace, not the
// types declared later in this file's own namespace.
using VDrumExplorer.Gui.Avalonia.Test;

// Configures the headless Avalonia application used by all [AvaloniaFact] tests.
// TestApp loads the same styles and resources as the production App (FluentTheme,
// RegularScaling styles and application resources) so that the views under test
// render exactly as they do in production, without the production window/MIDI setup.
[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace VDrumExplorer.Gui.Avalonia.Test;

/// <summary>
/// Provides the headless <see cref="AppBuilder"/> used by the Avalonia test framework.
/// The Skia renderer is combined with a headless platform so rendering works without a
/// display server, while still producing real pixel output that can be captured and
/// compared against baseline screenshots.
/// </summary>
public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                // false = enable pixel output for screenshots (Skia renders to real buffers
                // instead of a draw-command recorder with no pixels).
                UseHeadlessDrawing = false
            })
            // Same font as the production app (Program.cs), so text renders consistently.
            .WithInterFont()
            // Pin the default font family to the bundled Inter font: the fontconfig-free
            // Skia native binary (SkiaSharp.NativeAssets.Linux.NoDependencies) cannot
            // discover a system default font, which would otherwise throw
            // InvalidOperationException ("Default font family name can't be null or empty").
            .With(new FontManagerOptions { DefaultFamilyName = "fonts:Inter#Inter" })
            .LogToTrace();
}
