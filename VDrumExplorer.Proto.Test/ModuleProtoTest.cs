// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using System.Linq;

namespace VDrumExplorer.Proto.Test
{
    public class ModuleProtoTest
    {
        private Model.Module module = null!;

        [SetUp]
        public void SetUp()
        {
            module = TestData.LoadTD27Module();
        }

        [Test]
        public void FromModel_CreatesProtoModuleWithCorrectIdentifier()
        {
            var protoModule = Module.FromModel(module);
            Assert.IsNotNull(protoModule.Identifier);
            Assert.AreEqual(module.Schema.Identifier.Name, protoModule.Identifier.Name);
            Assert.AreEqual(module.Schema.Identifier.ModelId, protoModule.Identifier.ModelId);
            Assert.AreEqual(module.Schema.Identifier.FamilyCode, protoModule.Identifier.FamilyCode);
            Assert.AreEqual(module.Schema.Identifier.FamilyNumberCode, protoModule.Identifier.FamilyNumberCode);
            Assert.AreEqual(module.Schema.Identifier.SoftwareRevision, protoModule.Identifier.SoftwareRevision);
        }

        [Test]
        public void FromModel_CreatesProtoModuleWithContainers()
        {
            var protoModule = Module.FromModel(module);
            Assert.Greater(protoModule.Containers.Count, 0);
        }

        [Test]
        public void FromModel_ContainersCountMatchesSnapshotSegmentCount()
        {
            var snapshot = module.Data.CreateSnapshot();
            var protoModule = Module.FromModel(module);
            Assert.AreEqual(snapshot.SegmentCount, protoModule.Containers.Count);
        }

        [Test]
        public void ToModel_CreatesModelModuleWithCorrectSchema()
        {
            var protoModule = Module.FromModel(module);
            var result = protoModule.ToModel(NullLogger.Instance);
            Assert.AreEqual(module.Schema.Identifier, result.Schema.Identifier);
        }

        [Test]
        public void ToModel_CreatesModelModuleWithData()
        {
            var protoModule = Module.FromModel(module);
            var result = protoModule.ToModel(NullLogger.Instance);
            Assert.IsNotNull(result.Data);
        }

        [Test]
        public void RoundTrip_FromModelThenToModel_ProducesEquivalentModule()
        {
            var protoModule = Module.FromModel(module);
            var result = protoModule.ToModel(NullLogger.Instance);

            var originalSegments = module.Data.CreateSnapshot().Segments.ToList();
            var resultSegments = result.Data.CreateSnapshot().Segments.ToList();
            Assert.AreEqual(originalSegments.Count, resultSegments.Count);

            for (int i = 0; i < originalSegments.Count; i++)
            {
                Assert.AreEqual(originalSegments[i].Address, resultSegments[i].Address, $"Address of segment {i}");
                Assert.AreEqual(originalSegments[i].Size, resultSegments[i].Size, $"Size of segment {i}");
                Assert.AreEqual(originalSegments[i].CopyData(), resultSegments[i].CopyData(), $"Data in segment {i}");
            }
        }
    }
}
