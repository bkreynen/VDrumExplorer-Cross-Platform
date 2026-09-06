// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("VDrumExplorer.ViewModel.Test")]
// The Avalonia GUI visual tests construct NodeSnapshot directly (internal constructor)
// and call IsValidForTarget to build MultiPasteViewModel candidates.
[assembly: InternalsVisibleTo("VDrumExplorer.Gui.Avalonia.Test")]
