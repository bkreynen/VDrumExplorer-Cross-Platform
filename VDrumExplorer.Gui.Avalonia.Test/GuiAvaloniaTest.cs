// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Xunit;
using VDrumExplorer.Gui.Avalonia;

namespace VDrumExplorer.Gui.Avalonia.Test
{
    public class GuiAvaloniaTest
    {
        // Placeholder test to ensure the test project builds and the Avalonia GUI assembly
        // is loaded and instrumented for code coverage. Real GUI tests will be added later.
        [Fact]
        public void ProjectLoads()
        {
            // Verifies that the Avalonia GUI assembly is loadable.
            Assert.NotNull(typeof(App).Assembly);
        }
    }
}
