// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Microsoft.Extensions.Logging.Abstractions;
using VDrumExplorer.Model;
using VDrumExplorer.Proto;

namespace VDrumExplorer.ViewModel.Test
{
    internal static class TestData
    {
        internal static ModuleSchema LoadTD27Schema() => LoadTD27Module().Schema;

        internal static Module LoadTD27Module()
        {
            using var stream = typeof(TestData).Assembly.GetManifestResourceStream("td27.vdrum");
            return (Module)ProtoIo.ReadModel(stream!, NullLogger.Instance);
        }
    }
}
