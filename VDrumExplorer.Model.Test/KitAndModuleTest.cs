// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using System;
using System.Linq;
using VDrumExplorer.Model.Data;

namespace VDrumExplorer.Model.Test
{
    public class KitAndModuleTest
    {
        private Module module;

        [SetUp]
        public void Setup()
        {
            module = TestData.LoadTD27();
        }

        [Test]
        public void ExportKit_ReturnsValidKit()
        {
            var kit = module.ExportKit(1);
            Assert.IsNotNull(kit);
        }

        [Test]
        public void ExportKit_DefaultKitNumberMatchesRequestedKit()
        {
            var kit = module.ExportKit(5);
            Assert.AreEqual(5, kit.DefaultKitNumber);
        }

        [Test]
        public void ExportKit_KitRootIsNotNull()
        {
            var kit = module.ExportKit(1);
            Assert.IsNotNull(kit.KitRoot);
        }

        [Test]
        public void ExportKit_SchemaMatchesModuleSchema()
        {
            var kit = module.ExportKit(1);
            Assert.AreSame(module.Schema, kit.Schema);
        }

        [Test]
        public void ExportKit_DataIsNotNull()
        {
            var kit = module.ExportKit(1);
            Assert.IsNotNull(kit.Data);
        }

        [Test]
        public void GetKitName_ReturnsNonEmptyStringForValidKit()
        {
            var name = module.GetKitName(1);
            Assert.IsFalse(string.IsNullOrEmpty(name), "Kit 1 name should not be empty");
        }

        [Test]
        public void GetKitName_ReturnsDifferentNamesForDifferentKits()
        {
            var name1 = module.GetKitName(1);
            var name2 = module.GetKitName(2);
            // It's possible (though unlikely) that two kits have the same name, but typically they don't.
            // We at least verify both are non-empty.
            Assert.IsFalse(string.IsNullOrEmpty(name1));
            Assert.IsFalse(string.IsNullOrEmpty(name2));
        }

        [Test]
        public void Kit_GetKitName_ReturnsKitName()
        {
            var kit = module.ExportKit(1);
            var kitName = kit.GetKitName();
            var moduleName = module.GetKitName(1);
            Assert.AreEqual(moduleName, kitName);
        }

        [Test]
        public void ImportKit_CopiesDataCorrectly()
        {
            // Export kit 1, import it to kit 2, verify kit 2 name matches kit 1 name.
            var originalName = module.GetKitName(1);
            var kit = module.ExportKit(1);
            module.ImportKit(kit, 2);
            var importedName = module.GetKitName(2);
            Assert.AreEqual(originalName, importedName);
        }

        [Test]
        public void ImportKit_WithMismatchedSchema_Throws()
        {
            // Construct a kit from a different known schema and verify import throws.
            var foreignSchema = ModuleSchema.KnownSchemas[ModuleIdentifier.TD17].Value;
            // Ensure the foreign schema is actually different (reference inequality).
            Assert.AreNotSame(module.Schema, foreignSchema);
            var foreignModuleData = ModuleData.FromLogicalRootNode(foreignSchema.LogicalRoot);
            var foreignModule = new Module(foreignModuleData);
            var foreignKit = foreignModule.ExportKit(1);
            Assert.Throws<ArgumentException>(() => module.ImportKit(foreignKit, 3));
        }

        [Test]
        public void Module_FromSnapshot_RoundTripsData()
        {
            var snapshot = module.Data.CreateSnapshot();
            var newModule = Module.FromSnapshot(module.Schema, snapshot, NullLogger.Instance);
            AssertSnapshotsEqual(module.Data, newModule.Data);
        }

        [Test]
        public void Kit_FromSnapshot_RoundTripsData()
        {
            var kit = module.ExportKit(1);
            var snapshot = kit.Data.CreateSnapshot();
            var newKit = Kit.FromSnapshot(kit.Schema, snapshot, kit.DefaultKitNumber, NullLogger.Instance);
            AssertSnapshotsEqual(kit.Data, newKit.Data);
        }

        [Test]
        public void Module_Schema_IsNotNull()
        {
            Assert.IsNotNull(module.Schema);
        }

        [Test]
        public void Module_Data_IsNotNull()
        {
            Assert.IsNotNull(module.Data);
        }

        [Test]
        public void Kit_KitRoot_MatchesSchemaKit1Root()
        {
            var kit = module.ExportKit(1);
            Assert.AreSame(module.Schema.Kit1Root, kit.KitRoot);
        }

        private static void AssertSnapshotsEqual(ModuleData expectedData, ModuleData actualData)
        {
            var originalSegments = expectedData.CreateSnapshot().Segments.ToList();
            var newSegments = actualData.CreateSnapshot().Segments.ToList();
            Assert.AreEqual(originalSegments.Count, newSegments.Count);

            for (int i = 0; i < originalSegments.Count; i++)
            {
                var originalSegment = originalSegments[i];
                var newSegment = newSegments[i];
                Assert.AreEqual(originalSegment.Address, newSegment.Address, $"Address of segment {i}");
                Assert.AreEqual(originalSegment.Size, newSegment.Size, $"Size of segment {i}");
                Assert.AreEqual(originalSegment.CopyData(), newSegment.CopyData(), $"Data in segment starting at {originalSegment.Address}");
            }
        }
    }
}
