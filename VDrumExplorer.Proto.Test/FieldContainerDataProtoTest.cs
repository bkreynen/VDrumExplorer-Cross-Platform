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
    }
}
