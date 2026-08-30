// Copyright 2023 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Linq;
using NUnit.Framework;
using VDrumExplorer.Model.Data;

namespace VDrumExplorer.Model.Test.Helpers
{
    /// <summary>
    /// Shared proto/data assertion helpers for Model.Test. Mirrors
    /// VDrumExplorer.Proto.Test.Helpers.ProtoTestHelpers to avoid duplication
    /// across test assemblies that cannot share a project reference.
    /// Consolidates the duplicated AssertDataEqual/AssertSnapshotsEqual logic
    /// previously triplicated in ProtoIoTest and KitAndModuleTest.
    /// </summary>
    internal static class ProtoTestHelpers
    {
        internal static void AssertDataEqual(ModuleData expected, ModuleData actual)
        {
            var expectedSegments = expected.CreateSnapshot().Segments.ToList();
            var actualSegments = actual.CreateSnapshot().Segments.ToList();
            Assert.AreEqual(expectedSegments.Count, actualSegments.Count, "Segment count");
            for (int i = 0; i < expectedSegments.Count; i++)
            {
                var exp = expectedSegments[i];
                var act = actualSegments[i];
                Assert.AreEqual(exp.Address, act.Address, $"Address of segment {i}");
                Assert.AreEqual(exp.Size, act.Size, $"Size of segment {i}");
                Assert.AreEqual(exp.CopyData(), act.CopyData(), $"Data in segment starting at {exp.Address}");
            }
        }

        internal static void AssertSnapshotsEqual(ModuleData expected, ModuleData actual) => AssertDataEqual(expected, actual);
    }
}
