// Copyright 2023 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Linq;
using NUnit.Framework;
using VDrumExplorer.Model.Data;

namespace VDrumExplorer.Proto.Test.Helpers
{
    /// <summary>
    /// Shared proto/data assertion helpers. Consolidates the triplicated AssertDataEqual
    /// previously in ProtoIoTest (Model.Test), KitProtoTest and ModuleProtoTest,
    /// plus ProtoIoTest (Proto.Test). Also hosts AssertSnapshotsEqual used by KitAndModuleTest.
    /// </summary>
    internal static class ProtoTestHelpers
    {
        /// <summary>
        /// Asserts that two <see cref="ModuleData"/> snapshots have identical segments
        /// (address, size and payload) – the standard ModuleData equivalence check.
        /// </summary>
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

        /// <summary>
        /// Alias for <see cref="AssertDataEqual"/> used by callers that previously named it
        /// AssertSnapshotsEqual (KitAndModuleTest).
        /// </summary>
        internal static void AssertSnapshotsEqual(ModuleData expected, ModuleData actual) => AssertDataEqual(expected, actual);
    }
}
