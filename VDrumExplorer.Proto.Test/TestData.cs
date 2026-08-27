// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Microsoft.Extensions.Logging.Abstractions;

namespace VDrumExplorer.Proto.Test
{
    internal static class TestData
    {
        internal static Model.Module LoadTD27Module()
        {
            using var stream = typeof(TestData).Assembly.GetManifestResourceStream("td27.vdrum")
                ?? throw new System.InvalidOperationException("Embedded resource 'td27.vdrum' not found.");
            return (Model.Module)ProtoIo.ReadModel(stream, NullLogger.Instance);
        }
    }
}
