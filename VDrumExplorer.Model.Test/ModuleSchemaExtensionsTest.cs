// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using VDrumExplorer.Model.Schema.Fields;

namespace VDrumExplorer.Model.Test
{
    public class ModuleSchemaExtensionsTest
    {
        private ModuleSchema schema;

        [SetUp]
        public void Setup()
        {
            schema = TestData.LoadTD27().Schema;
        }

        [Test]
        public void Kits_CountMatchesExpectedForTD27()
        {
            Assert.AreEqual(100, schema.Kits);
        }

        [Test]
        public void UserSamples_CountMatchesExpectedForTD27()
        {
            Assert.AreEqual(500, schema.UserSamples);
        }

        [Test]
        public void Kit1Root_IsNotNull()
        {
            Assert.IsNotNull(schema.Kit1Root);
        }

        [Test]
        public void GetKitRoot_ReturnsCorrectRootForValidKitNumber()
        {
            var root1 = schema.GetKitRoot(1);
            Assert.IsNotNull(root1);
            Assert.AreEqual(1, root1.KitNumber);

            var root50 = schema.GetKitRoot(50);
            Assert.IsNotNull(root50);
            Assert.AreEqual(50, root50.KitNumber);
        }

        [Test]
        public void GetKitRoot_ReturnsDifferentRootsForDifferentKits()
        {
            var root1 = schema.GetKitRoot(1);
            var root2 = schema.GetKitRoot(2);
            Assert.AreNotSame(root1, root2);
        }

        [Test]
        public void GetKitRoot_ThrowsForZeroKitNumber()
        {
            // kitRoots is 0-indexed internally, accessed via kitNumber - 1.
            // Kit number 0 would access index -1, causing an IndexOutOfRangeException.
            Assert.Throws<System.IndexOutOfRangeException>(() => schema.GetKitRoot(0));
        }

        [Test]
        public void GetKitRoot_ThrowsForKitNumberTooLarge()
        {
            Assert.Throws<System.IndexOutOfRangeException>(() => schema.GetKitRoot(101));
        }

        [Test]
        public void GetTriggerRoot_ReturnsCorrectRootForValidTrigger()
        {
            var triggerRoot = schema.GetTriggerRoot(1, 1);
            Assert.IsNotNull(triggerRoot);
        }

        [Test]
        public void GetTriggerRoot_ReturnsCorrectRootForDifferentTriggers()
        {
            var trigger1 = schema.GetTriggerRoot(1, 1);
            var trigger2 = schema.GetTriggerRoot(1, 2);
            Assert.AreNotSame(trigger1, trigger2);
        }

        [Test]
        public void GetTriggerRoot_ReturnsCorrectRootForDifferentKits()
        {
            var triggerKit1 = schema.GetTriggerRoot(1, 1);
            var triggerKit2 = schema.GetTriggerRoot(2, 1);
            Assert.AreNotSame(triggerKit1, triggerKit2);
        }

        [Test]
        public void GetMainInstrumentField_ReturnsInstrumentField()
        {
            // GetMainInstrumentField is internal, but accessible via InternalsVisibleTo.
            var field = schema.GetMainInstrumentField(1, 1);
            Assert.IsNotNull(field);
            Assert.IsInstanceOf<InstrumentField>(field);
        }

        [Test]
        public void GetMainInstrumentField_ReturnsFieldForDifferentTriggers()
        {
            var field1 = schema.GetMainInstrumentField(1, 1);
            var field2 = schema.GetMainInstrumentField(1, 2);
            Assert.AreNotSame(field1, field2);
        }

        [Test]
        public void Identifier_MatchesTD27()
        {
            Assert.AreEqual(ModuleIdentifier.TD27, schema.Identifier);
        }

        [Test]
        public void PhysicalRoot_IsNotNull()
        {
            Assert.IsNotNull(schema.PhysicalRoot);
        }

        [Test]
        public void LogicalRoot_IsNotNull()
        {
            Assert.IsNotNull(schema.LogicalRoot);
        }
    }
}
