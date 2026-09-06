// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Runtime.CompilerServices;
// The interaction tests call AvaloniaViewServices.ParseFilter directly (internal method)
// to verify the WPF-style filter string parsing.
[assembly: InternalsVisibleTo("VDrumExplorer.Gui.Avalonia.Test")]
