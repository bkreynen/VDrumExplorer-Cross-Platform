// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace VDrumExplorer.Model.Test
{
    public class ModuleIdentifierTest
    {
        [Test]
        public void AE01_HasCorrectValues()
        {
            var id = ModuleIdentifier.AE01;
            Assert.AreEqual("AE-01", id.Name);
            Assert.AreEqual(0x5a, id.ModelId);
            Assert.AreEqual(0x35a, id.FamilyCode);
            Assert.AreEqual(0, id.FamilyNumberCode);
            Assert.AreEqual(0, id.SoftwareRevision);
            Assert.AreEqual(4, id.ModelIdLength);
        }

        [Test]
        public void AE10_HasCorrectValues()
        {
            var id = ModuleIdentifier.AE10;
            Assert.AreEqual("AE-10", id.Name);
            Assert.AreEqual(0x2f, id.ModelId);
            Assert.AreEqual(0x32f, id.FamilyCode);
            Assert.AreEqual(0, id.FamilyNumberCode);
            Assert.AreEqual(0x00_00_01_00, id.SoftwareRevision);
            Assert.AreEqual(4, id.ModelIdLength);
        }

        [Test]
        public void TD07_HasCorrectValues()
        {
            var id = ModuleIdentifier.TD07;
            Assert.AreEqual("TD-07", id.Name);
            Assert.AreEqual(0x75, id.ModelId);
            Assert.AreEqual(0x375, id.FamilyCode);
            Assert.AreEqual(0, id.FamilyNumberCode);
            Assert.AreEqual(0, id.SoftwareRevision);
            Assert.AreEqual(4, id.ModelIdLength);
        }

        [Test]
        public void TD17_HasCorrectValues()
        {
            var id = ModuleIdentifier.TD17;
            Assert.AreEqual("TD-17", id.Name);
            Assert.AreEqual(0x4b, id.ModelId);
            Assert.AreEqual(0x34b, id.FamilyCode);
            Assert.AreEqual(0, id.FamilyNumberCode);
            Assert.AreEqual(0, id.SoftwareRevision);
            Assert.AreEqual(4, id.ModelIdLength);
        }

        [Test]
        public void TD27_HasCorrectValues()
        {
            var id = ModuleIdentifier.TD27;
            Assert.AreEqual("TD-27", id.Name);
            Assert.AreEqual(0x63, id.ModelId);
            Assert.AreEqual(0x363, id.FamilyCode);
            Assert.AreEqual(0, id.FamilyNumberCode);
            Assert.AreEqual(0, id.SoftwareRevision);
            Assert.AreEqual(4, id.ModelIdLength);
        }

        [Test]
        public void TD50_HasCorrectValues()
        {
            var id = ModuleIdentifier.TD50;
            Assert.AreEqual("TD-50", id.Name);
            Assert.AreEqual(0x24, id.ModelId);
            Assert.AreEqual(0x324, id.FamilyCode);
            Assert.AreEqual(0, id.FamilyNumberCode);
            Assert.AreEqual(0x00_01_00_00, id.SoftwareRevision);
            Assert.AreEqual(4, id.ModelIdLength);
        }

        [Test]
        public void TD50X_HasCorrectValues()
        {
            var id = ModuleIdentifier.TD50X;
            Assert.AreEqual("TD-50X", id.Name);
            Assert.AreEqual(0x07, id.ModelId);
            Assert.AreEqual(0x407, id.FamilyCode);
            Assert.AreEqual(0, id.FamilyNumberCode);
            Assert.AreEqual(0x00_01_00_00, id.SoftwareRevision);
            Assert.AreEqual(5, id.ModelIdLength);
        }

        [Test]
        public void ModelIdLength_Is5_ForTD50X()
        {
            Assert.AreEqual(5, ModuleIdentifier.TD50X.ModelIdLength);
        }

        [Test]
        public void ModelIdLength_Is4_ForAllExceptTD50X()
        {
            Assert.AreEqual(4, ModuleIdentifier.AE01.ModelIdLength);
            Assert.AreEqual(4, ModuleIdentifier.AE10.ModelIdLength);
            Assert.AreEqual(4, ModuleIdentifier.TD07.ModelIdLength);
            Assert.AreEqual(4, ModuleIdentifier.TD17.ModelIdLength);
            Assert.AreEqual(4, ModuleIdentifier.TD27.ModelIdLength);
            Assert.AreEqual(4, ModuleIdentifier.TD50.ModelIdLength);
        }

        [Test]
        public void Equals_SameValues_ReturnsTrue()
        {
            var id1 = new ModuleIdentifier("TD-27", 0x63, 0x363, 0, 0);
            var id2 = new ModuleIdentifier("TD-27", 0x63, 0x363, 0, 0);
            Assert.IsTrue(id1.Equals(id2));
            Assert.IsTrue(id1.Equals((object)id2));
        }

        [Test]
        public void Equals_DifferentName_ReturnsFalse()
        {
            var id1 = new ModuleIdentifier("TD-27", 0x63, 0x363, 0, 0);
            var id2 = new ModuleIdentifier("TD-17", 0x63, 0x363, 0, 0);
            Assert.IsFalse(id1.Equals(id2));
        }

        [Test]
        public void Equals_DifferentModelId_ReturnsFalse()
        {
            var id1 = new ModuleIdentifier("TD-27", 0x63, 0x363, 0, 0);
            var id2 = new ModuleIdentifier("TD-27", 0x64, 0x363, 0, 0);
            Assert.IsFalse(id1.Equals(id2));
        }

        [Test]
        public void Equals_DifferentFamilyCode_ReturnsFalse()
        {
            var id1 = new ModuleIdentifier("TD-27", 0x63, 0x363, 0, 0);
            var id2 = new ModuleIdentifier("TD-27", 0x63, 0x364, 0, 0);
            Assert.IsFalse(id1.Equals(id2));
        }

        [Test]
        public void Equals_DifferentFamilyNumberCode_ReturnsFalse()
        {
            var id1 = new ModuleIdentifier("TD-27", 0x63, 0x363, 0, 0);
            var id2 = new ModuleIdentifier("TD-27", 0x63, 0x363, 1, 0);
            Assert.IsFalse(id1.Equals(id2));
        }

        [Test]
        public void Equals_DifferentSoftwareRevision_ReturnsFalse()
        {
            var id1 = new ModuleIdentifier("TD-27", 0x63, 0x363, 0, 0);
            var id2 = new ModuleIdentifier("TD-27", 0x63, 0x363, 0, 1);
            Assert.IsFalse(id1.Equals(id2));
        }

        [Test]
        public void Equals_Null_ReturnsFalse()
        {
            var id = new ModuleIdentifier("TD-27", 0x63, 0x363, 0, 0);
            Assert.IsFalse(id.Equals((ModuleIdentifier)null));
            Assert.IsFalse(id.Equals((object)null));
        }

        [Test]
        public void GetHashCode_ConsistentWithEquals()
        {
            var id1 = new ModuleIdentifier("TD-27", 0x63, 0x363, 0, 0);
            var id2 = new ModuleIdentifier("TD-27", 0x63, 0x363, 0, 0);
            Assert.IsTrue(id1.Equals(id2));
            Assert.AreEqual(id1.GetHashCode(), id2.GetHashCode());
        }

        [Test]
        public void ToString_ReturnsExpectedFormat()
        {
            var id = new ModuleIdentifier("TD-27", 0x63, 0x363, 0, 0);
            Assert.AreEqual("Name: TD-27; ModelId: 99; FamilyCode: 867; FamilyNumberCode: 0; SoftwareRevision: 0", id.ToString());
        }

        [Test]
        public void WithSoftwareRevision_ReturnsNewInstanceWithSameValuesExceptRevision()
        {
            var original = ModuleIdentifier.TD27;
            var revised = original.WithSoftwareRevision(2);
            Assert.AreEqual(original.Name, revised.Name);
            Assert.AreEqual(original.ModelId, revised.ModelId);
            Assert.AreEqual(original.FamilyCode, revised.FamilyCode);
            Assert.AreEqual(original.FamilyNumberCode, revised.FamilyNumberCode);
            Assert.AreEqual(2, revised.SoftwareRevision);
            Assert.AreNotEqual(original.SoftwareRevision, revised.SoftwareRevision);
        }

        [Test]
        public void WithSoftwareRevision_PreservesModelIdLength()
        {
            var original = ModuleIdentifier.TD50X;
            var revised = original.WithSoftwareRevision(3);
            Assert.AreEqual(original.ModelIdLength, revised.ModelIdLength);
        }

        [Test]
        public void WithSoftwareRevision_ReturnsDifferentInstance()
        {
            var original = ModuleIdentifier.TD27;
            var revised = original.WithSoftwareRevision(0);
            Assert.AreNotSame(original, revised);
        }

        [Test]
        public void Equals_IgnoresModelIdLength()
        {
            // ModelIdLength is not part of equality. To prove this we create two
            // identifiers with identical Name/ModelId/FamilyCode/FamilyNumberCode/SoftwareRevision
            // but forcibly give them different ModelIdLength via reflection (which bypasses the
            // constructor's DetermineModelIdLength). They should still be equal and have equal
            // hash codes despite differing lengths.
            var id1 = new ModuleIdentifier("TD-27", 0x63, 0x363, 0, 0);
            var id2 = new ModuleIdentifier("TD-27", 0x63, 0x363, 0, 0);
            var field = typeof(ModuleIdentifier)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(f => f.Name.Contains("ModelIdLength"));
            Assert.IsNotNull(field, "ModelIdLength backing field not found");
            field!.SetValue(id1, 4);
            field.SetValue(id2, 5);
            Assert.AreEqual(4, id1.ModelIdLength);
            Assert.AreEqual(5, id2.ModelIdLength);
            Assert.AreNotEqual(id1.ModelIdLength, id2.ModelIdLength);
            Assert.IsTrue(id1.Equals(id2), "Equals should ignore ModelIdLength");
            Assert.IsTrue(id1.Equals((object)id2), "Equals(object) should ignore ModelIdLength");
            Assert.AreEqual(id1.GetHashCode(), id2.GetHashCode(), "GetHashCode should ignore ModelIdLength");
        }
    }
}
