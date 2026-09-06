// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using VDrumExplorer.Model;
using VDrumExplorer.Proto;
using Xunit;

namespace VDrumExplorer.Gui.Avalonia.Test;

/// <summary>
/// Loads sample module data for the visual tests. The TD-27 sample file from the repository
/// root is embedded into the test assembly, so the tests do not depend on the working
/// directory. The loaded module is deterministic, keeping the rendered screenshots stable.
/// </summary>
public static class TestData
{
    /// <summary>
    /// Loads the embedded TD-27 sample file as a full <see cref="Module"/>.
    /// </summary>
    public static Module LoadTD27Module()
    {
        using var stream = typeof(TestData).Assembly.GetManifestResourceStream("td27.vdrum");
        Assert.NotNull(stream);
        var model = ProtoIo.ReadModel(stream!, NullLogger.Instance);
        return Assert.IsType<Module>(model);
    }

    /// <summary>
    /// Loads the embedded TD-27 sample file and exports kit 1 as a standalone <see cref="Kit"/>.
    /// </summary>
    public static Kit LoadTD27Kit() => LoadTD27Module().ExportKit(1);
}
