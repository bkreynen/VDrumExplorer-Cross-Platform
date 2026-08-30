// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Google.Protobuf;
using NUnit.Framework;
using System.Linq;
using VDrumExplorer.Model.Data;

namespace VDrumExplorer.Proto.Test
{
    public class FieldContainerDataProtoTest
    {
        [Test]
        public void FromModel_CreatesProtoFieldContainerDataWithCorrectAddress()
        {
            var address = Model.ModuleAddress.FromDisplayValue(0x0100_0000);
            var data = new byte[] { 1, 2, 3, 4 };
            var segment = new DataSegment(address, data);
            var proto = FieldContainerData.FromModel(segment);
            Assert.AreEqual(address.DisplayValue, proto.Address);
        }

        [Test]
        public void FromModel_CreatesProtoFieldContainerDataWithCorrectData()
        {
            var address = Model.ModuleAddress.FromDisplayValue(0x0100_0000);
            var data = new byte[] { 1, 2, 3, 4 };
            var segment = new DataSegment(address, data);
            var proto = FieldContainerData.FromModel(segment);
            Assert.That(proto.Data.ToByteArray(), Is.EqualTo(data));
        }

        [Test]
        public void ToModel_CreatesDataSegmentWithCorrectAddress()
        {
            var proto = new FieldContainerData
            {
                Address = 0x0100_0000,
                Data = ByteString.CopyFrom(new byte[] { 10, 20, 30 })
            };
            var segment = proto.ToModel();
            Assert.AreEqual(0x0100_0000, segment.Address.DisplayValue);
        }

        [Test]
        public void ToModel_CreatesDataSegmentWithCorrectData()
        {
            var proto = new FieldContainerData
            {
                Address = 0x0100_0000,
                Data = ByteString.CopyFrom(new byte[] { 10, 20, 30 })
            };
            var segment = proto.ToModel();
            Assert.That(segment.CopyData(), Is.EqualTo(new byte[] { 10, 20, 30 }));
        }

        [Test]
        public void RoundTrip_FromModelThenToModel_PreservesAddressAndData()
        {
            var address = Model.ModuleAddress.FromDisplayValue(0x0200_0100);
            var data = new byte[] { 0xFF, 0x00, 0x42, 0x7F, 0x80 };
            var segment = new DataSegment(address, data);
            var proto = FieldContainerData.FromModel(segment);
            var result = proto.ToModel();
            Assert.AreEqual(segment.Address, result.Address);
            Assert.That(result.CopyData(), Is.EqualTo(data));
        }

        [Test]
        public void RoundTrip_ToModelThenFromModel_PreservesAddressAndData()
        {
            var originalData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            var proto = new FieldContainerData
            {
                Address = 0x0300_0200,
                Data = ByteString.CopyFrom(originalData)
            };
            var segment = proto.ToModel();
            var result = FieldContainerData.FromModel(segment);
            Assert.AreEqual(proto.Address, result.Address);
            Assert.That(result.Data.ToByteArray(), Is.EqualTo(originalData));
        }

        [Test]
        public void FromModel_WithVariousAddressesAndDataSizes()
        {
            var testCases = new[]
            {
                (Model.ModuleAddress.FromDisplayValue(0x0000_0000), new byte[] { 0 }),
                (Model.ModuleAddress.FromDisplayValue(0x0100_0000), new byte[] { 1, 2 }),
                (Model.ModuleAddress.FromDisplayValue(0x7F7F_7F7F), new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }),
            };

            foreach (var (address, data) in testCases)
            {
                var segment = new DataSegment(address, data);
                var proto = FieldContainerData.FromModel(segment);
                var result = proto.ToModel();
                Assert.AreEqual(address, result.Address, $"Address mismatch for {address}");
                Assert.That(result.CopyData(), Is.EqualTo(data), $"Data mismatch for address {address}");
            }
        }

        [Test]
        public void FromModel_FromActualModuleSnapshot()
        {
            var module = TestData.LoadTD27Module();
            var snapshot = module.Data.CreateSnapshot();
            var firstSegment = snapshot.Segments.First();
            var proto = FieldContainerData.FromModel(firstSegment);
            var result = proto.ToModel();
            Assert.AreEqual(firstSegment.Address, result.Address);
            Assert.That(result.CopyData(), Is.EqualTo(firstSegment.CopyData()));
        }

        [Test]
        public void RoundTrip_MidAddress_LowBitTransition_0x01000080()
        {
            // 0x01000080 as a *logical* mid value exercises the 0x80 low-bit carry path:
            // logical 0x01000080 has bit 7 set, but display encoding is 7-bit per byte, so
            // ModuleAddress must compensate (operator+ adds 0x80/0x8000/0x800000 when crossing).
            // Using FromLogicalValue avoids the invalid display 0x01000080 (whose low byte has 0x80 set,
            // which FromDisplayValue rejects). The round-trip via FieldContainerData must preserve the address.
            var address = Model.ModuleAddress.FromLogicalValue(0x01000080);
            var data = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
            var segment = new DataSegment(address, data);
            var proto = FieldContainerData.FromModel(segment);
            Assert.AreEqual(address.DisplayValue, proto.Address, "Proto should preserve display encoding of logical 0x01000080");
            var result = proto.ToModel();
            Assert.AreEqual(address, result.Address, "Round-trip should preserve non-aligned mid address (logical 0x01000080)");
            Assert.That(result.CopyData(), Is.EqualTo(data));
            // Also verify the display value is valid (top bit clear in each byte) and is the expected 7-bit packing.
            Assert.AreEqual(0, proto.Address & 0x80808080, "Display value must have top bit clear in each byte");
        }

        [Test]
        public void FromModel_MidAddresses_RoundTrip()
        {
            var midAddresses = new[]
            {
                Model.ModuleAddress.FromLogicalValue(0x01000080),
                Model.ModuleAddress.FromDisplayValue(0x02007F00), // high bit of middle byte transition — valid display
                Model.ModuleAddress.FromDisplayValue(0x10000000),
            };
            var data = new byte[] { 1, 2, 3 };
            foreach (var addr in midAddresses)
            {
                var segment = new DataSegment(addr, data);
                var proto = FieldContainerData.FromModel(segment);
                var result = proto.ToModel();
                Assert.AreEqual(addr, result.Address, $"Mid address {addr.DisplayValue:x8} should round-trip");
                Assert.That(result.CopyData(), Is.EqualTo(data));
            }
        }
    }
}
