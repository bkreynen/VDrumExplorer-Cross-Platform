// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using System.Linq;

namespace VDrumExplorer.Proto.Test
{
    public class KitProtoTest
    {
        private Model.Kit kit = null!;

        [SetUp]
        public void SetUp()
        {
            kit = TestData.LoadTD27Module().ExportKit(3);
        }

        [Test]
        public void FromModel_CreatesProtoKitWithCorrectIdentifier()
        {
            var protoKit = Kit.FromModel(kit);
            Assert.IsNotNull(protoKit.Identifier);
            Assert.AreEqual(kit.Schema.Identifier.Name, protoKit.Identifier.Name);
            Assert.AreEqual(kit.Schema.Identifier.ModelId, protoKit.Identifier.ModelId);
            Assert.AreEqual(kit.Schema.Identifier.FamilyCode, protoKit.Identifier.FamilyCode);
            Assert.AreEqual(kit.Schema.Identifier.FamilyNumberCode, protoKit.Identifier.FamilyNumberCode);
            Assert.AreEqual(kit.Schema.Identifier.SoftwareRevision, protoKit.Identifier.SoftwareRevision);
        }

        [Test]
        public void FromModel_CreatesProtoKitWithContainers()
        {
            var protoKit = Kit.FromModel(kit);
            Assert.Greater(protoKit.Containers.Count, 0);
        }

        [Test]
        public void FromModel_CreatesProtoKitWithCorrectDefaultKitNumber()
        {
            var protoKit = Kit.FromModel(kit);
            Assert.AreEqual(kit.DefaultKitNumber, protoKit.DefaultKitNumber);
        }

        [Test]
        public void FromModel_ContainersCountMatchesSnapshotSegmentCount()
        {
            var snapshot = kit.Data.CreateSnapshot();
            var protoKit = Kit.FromModel(kit);
            Assert.AreEqual(snapshot.SegmentCount, protoKit.Containers.Count);
        }

        [Test]
        public void ToModel_CreatesModelKitWithCorrectSchema()
        {
            var protoKit = Kit.FromModel(kit);
            var result = protoKit.ToModel(NullLogger.Instance);
            Assert.AreEqual(kit.Schema.Identifier, result.Schema.Identifier);
        }

        [Test]
        public void ToModel_CreatesModelKitWithData()
        {
            var protoKit = Kit.FromModel(kit);
            var result = protoKit.ToModel(NullLogger.Instance);
            Assert.IsNotNull(result.Data);
        }

        [Test]
        public void ToModel_PreservesDefaultKitNumber()
        {
            var protoKit = Kit.FromModel(kit);
            var result = protoKit.ToModel(NullLogger.Instance);
            Assert.AreEqual(kit.DefaultKitNumber, result.DefaultKitNumber);
        }

        [Test]
        public void RoundTrip_FromModelThenToModel_ProducesEquivalentKit()
        {
            var protoKit = Kit.FromModel(kit);
            var result = protoKit.ToModel(NullLogger.Instance);

            var originalSegments = kit.Data.CreateSnapshot().Segments.ToList();
            var resultSegments = result.Data.CreateSnapshot().Segments.ToList();
            Assert.AreEqual(originalSegments.Count, resultSegments.Count);

            for (int i = 0; i < originalSegments.Count; i++)
            {
                Assert.AreEqual(originalSegments[i].Address, resultSegments[i].Address, $"Address of segment {i}");
                Assert.AreEqual(originalSegments[i].Size, resultSegments[i].Size, $"Size of segment {i}");
                Assert.AreEqual(originalSegments[i].CopyData(), resultSegments[i].CopyData(), $"Data in segment {i}");
            }
        }

        [Test]
        public void RoundTrip_DefaultKitNumberPreserved()
        {
            var originalKit = TestData.LoadTD27Module().ExportKit(5);
            var protoKit = Kit.FromModel(originalKit);
            var result = protoKit.ToModel(NullLogger.Instance);
            Assert.AreEqual(originalKit.DefaultKitNumber, result.DefaultKitNumber);
        }

        [Test]
        public void ToModel_DefaultKitNumberZero_MapsToOne()
        {
            var kit = TestData.LoadTD27Module().ExportKit(1);
            var proto = Kit.FromModel(kit);
            proto.DefaultKitNumber = 0;
            var result = proto.ToModel(NullLogger.Instance);
            Assert.AreEqual(1, result.DefaultKitNumber);
        }
    }
}
