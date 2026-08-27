// Copyright 2023 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using VDrumExplorer.Model.Midi;

namespace VDrumExplorer.Model.Test.Midi
{
    /// <summary>
    /// Tests for <see cref="DeviceIdentity"/>.
    /// The constructor and RawDeviceId are internal, but accessible via InternalsVisibleTo.
    /// </summary>
    public class DeviceIdentityTest
    {
        [Test]
        public void Constructor_SetsAllProperties()
        {
            var identity = new DeviceIdentity(0x10, ManufacturerId.Roland, 0x0102, 0x0304, 0x0506);

            Assert.AreEqual(0x10, identity.RawDeviceId);
            Assert.AreEqual(ManufacturerId.Roland, identity.ManufacturerId);
            Assert.AreEqual(0x0102, identity.FamilyCode);
            Assert.AreEqual(0x0304, identity.FamilyNumberCode);
            Assert.AreEqual(0x0506, identity.SoftwareRevision);
        }

        [Test]
        public void DisplayDeviceId_IsRawDeviceIdPlusOne()
        {
            var identity = new DeviceIdentity(0x10, ManufacturerId.Roland, 0, 0, 0);
            Assert.AreEqual(0x10, identity.RawDeviceId);
            Assert.AreEqual(0x11, identity.DisplayDeviceId);
        }

        [TestCase(0x00, 0x01)]
        [TestCase(0x0F, 0x10)]
        [TestCase(0x10, 0x11)]
        [TestCase(0x1F, 0x20)]
        [TestCase(0x7E, 0x7F)]
        public void DisplayDeviceId_RawDeviceIdPlusOne(byte rawDeviceId, int expectedDisplayDeviceId)
        {
            var identity = new DeviceIdentity(rawDeviceId, ManufacturerId.Roland, 0, 0, 0);
            Assert.AreEqual(expectedDisplayDeviceId, identity.DisplayDeviceId);
        }

        [Test]
        public void Constructor_WithDifferentManufacturers()
        {
            var roland = new DeviceIdentity(0x10, ManufacturerId.Roland, 0, 0, 0);
            Assert.AreEqual(ManufacturerId.Roland, roland.ManufacturerId);

            var korg = new DeviceIdentity(0x10, ManufacturerId.Korg, 0, 0, 0);
            Assert.AreEqual(ManufacturerId.Korg, korg.ManufacturerId);

            var yamaha = new DeviceIdentity(0x10, ManufacturerId.Yamaha, 0, 0, 0);
            Assert.AreEqual(ManufacturerId.Yamaha, yamaha.ManufacturerId);
        }

        [Test]
        public void ToString_ReturnsExpectedFormat()
        {
            var identity = new DeviceIdentity(0x10, ManufacturerId.Roland, 0x0102, 0x0304, 0x0506);

            // Format: "{DisplayDeviceId}: {ManufacturerId} product {FamilyCode}/{FamilyNumberCode}, revision {SoftwareRevision}"
            Assert.AreEqual("17: Roland product 258/772, revision 1286", identity.ToString());
        }

        [Test]
        public void ToString_WithZeroValues()
        {
            var identity = new DeviceIdentity(0x00, ManufacturerId.Roland, 0, 0, 0);
            Assert.AreEqual("1: Roland product 0/0, revision 0", identity.ToString());
        }

        [Test]
        public void ToString_WithMaxValues()
        {
            var identity = new DeviceIdentity(0x7E, ManufacturerId.Akai, 0x7FFF, 0x7FFF, 0x7FFFFFFF);
            // DisplayDeviceId = 0x7E + 1 = 0x7F = 127
            Assert.AreEqual("127: Akai product 32767/32767, revision 2147483647", identity.ToString());
        }

        [Test]
        public void FamilyCode_AndFamilyNumberCode_AreIndependent()
        {
            var identity = new DeviceIdentity(0x10, ManufacturerId.Roland, 100, 200, 0);
            Assert.AreEqual(100, identity.FamilyCode);
            Assert.AreEqual(200, identity.FamilyNumberCode);
            Assert.AreNotEqual(identity.FamilyCode, identity.FamilyNumberCode);
        }

        [Test]
        public void SoftwareRevision_IsSetCorrectly()
        {
            var identity = new DeviceIdentity(0x10, ManufacturerId.Roland, 0, 0, 42);
            Assert.AreEqual(42, identity.SoftwareRevision);
        }
    }
}
