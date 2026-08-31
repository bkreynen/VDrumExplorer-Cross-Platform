// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace VDrumExplorer.Proto.Test
{
    public class ModuleIdentifierProtoTest
    {
        [Test]
        public void ToModel_CreatesCorrectModuleIdentifier()
        {
            var protoId = new ModuleIdentifier
            {
                Name = "TD-27",
                ModelId = 0x63,
                FamilyCode = 0x363,
                FamilyNumberCode = 0,
                SoftwareRevision = 0
            };
            var modelId = protoId.ToModel();
            Assert.AreEqual("TD-27", modelId.Name);
            Assert.AreEqual(0x63, modelId.ModelId);
            Assert.AreEqual(0x363, modelId.FamilyCode);
            Assert.AreEqual(0, modelId.FamilyNumberCode);
            Assert.AreEqual(0, modelId.SoftwareRevision);
        }

        [Test]
        public void FromModel_CreatesCorrectProtoModuleIdentifier()
        {
            var modelId = Model.ModuleIdentifier.TD27;
            var protoId = ModuleIdentifier.FromModel(modelId);
            Assert.AreEqual(modelId.Name, protoId.Name);
            Assert.AreEqual(modelId.ModelId, protoId.ModelId);
            Assert.AreEqual(modelId.FamilyCode, protoId.FamilyCode);
            Assert.AreEqual(modelId.FamilyNumberCode, protoId.FamilyNumberCode);
            Assert.AreEqual(modelId.SoftwareRevision, protoId.SoftwareRevision);
        }

        [Test]
        public void RoundTrip_FromModelThenToModel_ProducesEquivalentIdentifier()
        {
            var modelId = Model.ModuleIdentifier.TD27;
            var protoId = ModuleIdentifier.FromModel(modelId);
            var result = protoId.ToModel();
            Assert.AreEqual(modelId, result);
        }

        [Test]
        public void RoundTrip_ToModelThenFromModel_ProducesEquivalentIdentifier()
        {
            var protoId = new ModuleIdentifier
            {
                Name = "TD-17",
                ModelId = 0x4b,
                FamilyCode = 0x34b,
                FamilyNumberCode = 0,
                SoftwareRevision = 0
            };
            var modelId = protoId.ToModel();
            var result = ModuleIdentifier.FromModel(modelId);
            Assert.AreEqual(protoId.Name, result.Name);
            Assert.AreEqual(protoId.ModelId, result.ModelId);
            Assert.AreEqual(protoId.FamilyCode, result.FamilyCode);
            Assert.AreEqual(protoId.FamilyNumberCode, result.FamilyNumberCode);
            Assert.AreEqual(protoId.SoftwareRevision, result.SoftwareRevision);
        }

        [Test]
        public void GetOrInferSchema_KnownIdentifier_ReturnsCorrectSchema()
        {
            var protoId = ModuleIdentifier.FromModel(Model.ModuleIdentifier.TD27);
            var schema = protoId.GetOrInferSchema(_ => true, NullLogger.Instance);
            Assert.AreEqual(Model.ModuleIdentifier.TD27, schema.Identifier);
        }

        [Test]
        public void GetOrInferSchema_UnknownIdentifier_ThrowsInvalidDataException()
        {
            var protoId = new ModuleIdentifier
            {
                Name = "Unknown",
                ModelId = 0xFF,
                FamilyCode = 0xFFF,
                FamilyNumberCode = 0,
                SoftwareRevision = 0
            };
            Assert.Throws<InvalidDataException>(() => protoId.GetOrInferSchema(_ => true, NullLogger.Instance));
        }

        [Test]
        public void GetOrInferSchema_NoSoftwareRevision_InfersCorrectly()
        {
            // Create an identifier with SoftwareRevision not set (HasSoftwareRevision == false)
            var protoId = new ModuleIdentifier
            {
                Name = "TD-27",
                ModelId = 0x63,
                FamilyCode = 0x363,
                FamilyNumberCode = 0
            };
            Assert.IsFalse(protoId.HasSoftwareRevision);
            var schema = protoId.GetOrInferSchema(_ => true, NullLogger.Instance);
            Assert.AreEqual("TD-27", schema.Identifier.Name);
        }

        [Test]
        public void GetOrInferSchema_MultipleMatchingRevisions_PicksLatest()
        {
            // TD-17 has revisions 0, 0x01, 0x02 — compute max dynamically so test remains valid if a new revision is added
            var expectedMaxRevision = Model.ModuleSchema.KnownSchemas.Keys
                .Where(k => k.Name == "TD-17")
                .Max(k => k.SoftwareRevision);
            var protoId = new ModuleIdentifier
            {
                Name = "TD-17",
                ModelId = 0x4b,
                FamilyCode = 0x34b,
                FamilyNumberCode = 0
            };
            Assert.IsFalse(protoId.HasSoftwareRevision);
            var schema = protoId.GetOrInferSchema(_ => true, NullLogger.Instance);
            Assert.AreEqual(expectedMaxRevision, schema.Identifier.SoftwareRevision);
        }

        [Test]
        public void GetOrInferSchema_NoMatchingRevision_ThrowsInvalidDataException()
        {
            // Create an identifier with no software revision, but a validator that rejects all
            var protoId = new ModuleIdentifier
            {
                Name = "TD-27",
                ModelId = 0x63,
                FamilyCode = 0x363,
                FamilyNumberCode = 0
            };
            Assert.IsFalse(protoId.HasSoftwareRevision);
            Assert.Throws<InvalidDataException>(() => protoId.GetOrInferSchema(_ => false, NullLogger.Instance));
        }
    }
}
