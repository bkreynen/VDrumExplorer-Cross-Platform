// Copyright 2023 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using NUnit.Framework.Legacy;
using VDrumExplorer.Model.Midi;

namespace VDrumExplorer.Model.Test.Midi
{
    /// <summary>
    /// Extended tests for <see cref="DataSetMessage.TryParse"/>, covering error paths,
    /// different model ID lengths, addresses, and data payloads.
    /// </summary>
    internal class DataSetMessageExtendedTest
    {
        // The TD-50X uses a 5-byte model ID; the TD-17 uses a 4-byte model ID.
        private const int Td50XModelIdLength = 5;
        private const int Td17ModelIdLength = 4;

        [Test]
        public void TryParse_TooShort_ReturnsFalse()
        {
            // Minimum valid length for TD-50X is 5 + 10 = 15 bytes.
            // A 14-byte message is too short.
            var bytes = new byte[14];
            var message = new MidiMessage(bytes);
            Assert.False(DataSetMessage.TryParse(message, Td50XModelIdLength, out var result));
            Assert.Null(result);
        }

        [Test]
        public void TryParse_ExactlyMinimumLength_WithValidHeader_ReturnsTrue()
        {
            // Minimum length with no data payload: modelIdLength + 10.
            // For TD-50X that's 15 bytes. Build a valid message with empty data.
            var bytes = BuildDataSetMessage(Td50XModelIdLength, new byte[] { 0x00, 0x00, 0x00, 0x07 }, new byte[] { 0x00, 0x00, 0x00, 0x00 }, new byte[0]);
            var message = new MidiMessage(bytes);
            Assert.True(DataSetMessage.TryParse(message, Td50XModelIdLength, out var result));
            Assert.NotNull(result);
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void TryParse_WrongSysexStart_ReturnsFalse()
        {
            var bytes = BuildDataSetMessage(Td50XModelIdLength, new byte[] { 0x00, 0x00, 0x00, 0x07 }, new byte[] { 0x00, 0x00, 0x00, 0x00 }, new byte[] { 0x58 });
            bytes[0] = 0x00; // Wrong start byte (should be 0xF0)
            var message = new MidiMessage(bytes);
            Assert.False(DataSetMessage.TryParse(message, Td50XModelIdLength, out var result));
            Assert.Null(result);
        }

        [Test]
        public void TryParse_WrongManufacturerId_ReturnsFalse()
        {
            var bytes = BuildDataSetMessage(Td50XModelIdLength, new byte[] { 0x00, 0x00, 0x00, 0x07 }, new byte[] { 0x00, 0x00, 0x00, 0x00 }, new byte[] { 0x58 });
            bytes[1] = 0x42; // Korg instead of Roland (0x41)
            var message = new MidiMessage(bytes);
            Assert.False(DataSetMessage.TryParse(message, Td50XModelIdLength, out var result));
            Assert.Null(result);
        }

        [Test]
        public void TryParse_WrongCommandByte_ReturnsFalse()
        {
            var bytes = BuildDataSetMessage(Td50XModelIdLength, new byte[] { 0x00, 0x00, 0x00, 0x07 }, new byte[] { 0x00, 0x00, 0x00, 0x00 }, new byte[] { 0x58 });
            // Command byte is at index modelIdLength + 3 = 8 for TD-50X
            bytes[Td50XModelIdLength + 3] = 0x11; // RQ1 instead of DT1 (0x12)
            var message = new MidiMessage(bytes);
            Assert.False(DataSetMessage.TryParse(message, Td50XModelIdLength, out var result));
            Assert.Null(result);
        }

        [Test]
        public void TryParse_WrongEndByte_ReturnsFalse()
        {
            var bytes = BuildDataSetMessage(Td50XModelIdLength, new byte[] { 0x00, 0x00, 0x00, 0x07 }, new byte[] { 0x00, 0x00, 0x00, 0x00 }, new byte[] { 0x58 });
            bytes[bytes.Length - 1] = 0x00; // Wrong end byte (should be 0xF7)
            var message = new MidiMessage(bytes);
            Assert.False(DataSetMessage.TryParse(message, Td50XModelIdLength, out var result));
            Assert.Null(result);
        }

        [Test]
        public void TryParse_Td50X_FiveByteModelId()
        {
            // TD-50X has a 5-byte model ID. The first byte (at index 3) is 0x00,
            // and the actual 4-byte model ID starts at index 4 (modelIdOffset = 4).
            var bytes = BuildDataSetMessage(Td50XModelIdLength, new byte[] { 0x00, 0x00, 0x00, 0x07 }, new byte[] { 0x10, 0x20, 0x30, 0x40 }, new byte[] { 0x58 });
            var message = new MidiMessage(bytes);
            Assert.True(DataSetMessage.TryParse(message, Td50XModelIdLength, out var result));
            Assert.NotNull(result);
            Assert.AreEqual(0x07, result.ModelId);
            Assert.AreEqual(0x10203040, result.Address);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(0x58, result[0]);
        }

        [Test]
        public void TryParse_Td17_FourByteModelId()
        {
            // TD-17 has a 4-byte model ID. modelIdOffset = 3.
            var bytes = BuildDataSetMessage(Td17ModelIdLength, new byte[] { 0x00, 0x00, 0x00, 0x4b }, new byte[] { 0x00, 0x00, 0x01, 0x00 }, new byte[] { 0x7f });
            var message = new MidiMessage(bytes);
            Assert.True(DataSetMessage.TryParse(message, Td17ModelIdLength, out var result));
            Assert.NotNull(result);
            Assert.AreEqual(0x4b, result.ModelId);
            Assert.AreEqual(0x00000100, result.Address);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(0x7f, result.Data[0]);
        }

        [Test]
        public void TryParse_MultiByteDataPayload()
        {
            var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var bytes = BuildDataSetMessage(Td17ModelIdLength, new byte[] { 0x00, 0x00, 0x00, 0x4b }, new byte[] { 0x00, 0x00, 0x00, 0x00 }, data);
            var message = new MidiMessage(bytes);
            Assert.True(DataSetMessage.TryParse(message, Td17ModelIdLength, out var result));
            Assert.NotNull(result);
            Assert.AreEqual(5, result.Length);
            CollectionAssert.AreEqual(data, result.Data);
            // Verify indexer access
            for (int i = 0; i < data.Length; i++)
            {
                Assert.AreEqual(data[i], result[i]);
            }
        }

        [TestCase(0x00000000)]
        [TestCase(0x00000001)]
        [TestCase(0x01020304)]
        [TestCase(0x7f7f7f7f)]
        [TestCase(0x10000000)]
        public void TryParse_DifferentAddresses(int address)
        {
            var bytes = BuildDataSetMessage(Td17ModelIdLength, new byte[] { 0x00, 0x00, 0x00, 0x4b }, ToBigEndianBytes(address), new byte[] { 0x42 });
            var message = new MidiMessage(bytes);
            Assert.True(DataSetMessage.TryParse(message, Td17ModelIdLength, out var result));
            Assert.NotNull(result);
            Assert.AreEqual(address, result.Address);
        }

        [TestCase(new byte[] { 0x00 })]
        [TestCase(new byte[] { 0x7f })]
        [TestCase(new byte[] { 0x01, 0x02 })]
        [TestCase(new byte[] { 0xff, 0xee, 0xdd, 0xcc })]
        public void TryParse_DifferentDataSizes(byte[] data)
        {
            var bytes = BuildDataSetMessage(Td50XModelIdLength, new byte[] { 0x00, 0x00, 0x00, 0x07 }, new byte[] { 0x00, 0x00, 0x00, 0x00 }, data);
            var message = new MidiMessage(bytes);
            Assert.True(DataSetMessage.TryParse(message, Td50XModelIdLength, out var result));
            Assert.NotNull(result);
            Assert.AreEqual(data.Length, result.Length);
            CollectionAssert.AreEqual(data, result.Data);
        }

        [Test]
        public void TryParse_RawDeviceId_IsParsedCorrectly()
        {
            var bytes = BuildDataSetMessage(Td17ModelIdLength, new byte[] { 0x00, 0x00, 0x00, 0x4b }, new byte[] { 0x00, 0x00, 0x00, 0x00 }, new byte[] { 0x42 });
            // Set a specific raw device ID at index 2
            bytes[2] = 0x10;
            var message = new MidiMessage(bytes);
            Assert.True(DataSetMessage.TryParse(message, Td17ModelIdLength, out var result));
            Assert.NotNull(result);
            Assert.AreEqual(0x10, result.RawDeviceId);
            Assert.AreEqual(0x11, result.DisplayDeviceId);
        }

        [Test]
        public void TryParse_PropertiesAreConsistent()
        {
            var data = new byte[] { 0xaa, 0xbb, 0xcc };
            var addressBytes = new byte[] { 0x11, 0x22, 0x33, 0x44 };
            var modelIdBytes = new byte[] { 0x00, 0x00, 0x00, 0x4b };
            var bytes = BuildDataSetMessage(Td17ModelIdLength, modelIdBytes, addressBytes, data);
            var message = new MidiMessage(bytes);

            Assert.True(DataSetMessage.TryParse(message, Td17ModelIdLength, out var result));
            Assert.NotNull(result);
            Assert.AreEqual(0x4b, result.ModelId);
            Assert.AreEqual(0x11223344, result.Address);
            Assert.AreEqual(3, result.Length);
            CollectionAssert.AreEqual(data, result.Data);
            Assert.AreEqual(data[0], result[0]);
            Assert.AreEqual(data[1], result[1]);
            Assert.AreEqual(data[2], result[2]);
        }

        [Test]
        public void TryParse_WithCorrectChecksum_ReturnsTrue()
        {
            // Verify that a message with a correctly computed Roland checksum is parsed.
            var data = new byte[] { 0x01, 0x02, 0x03 };
            var modelId = new byte[] { 0x00, 0x00, 0x00, 0x4b };
            var address = new byte[] { 0x00, 0x00, 0x01, 0x00 };
            var bytes = BuildDataSetMessage(Td17ModelIdLength, modelId, address, data);
            // Overwrite placeholder 0x00 with the correct checksum.
            bytes[bytes.Length - 2] = ComputeChecksum(bytes, Td17ModelIdLength);
            var message = new MidiMessage(bytes);
            Assert.True(DataSetMessage.TryParse(message, Td17ModelIdLength, out var result));
            Assert.NotNull(result);
            CollectionAssert.AreEqual(data, result.Data);
        }

        [Test]
        public void TryParse_WithBadChecksum_StillReturnsTrue_DocumentsNoValidation()
        {
            // DataSetMessage.TryParse intentionally does NOT validate the Roland checksum.
            // This is documented here: even with a deliberately bad checksum the message is parsed.
            // If checksum validation is ever added, this test should be changed to assert false.
            var data = new byte[] { 0x01, 0x02 };
            var modelId = new byte[] { 0x00, 0x00, 0x00, 0x4b };
            var address = new byte[] { 0x00, 0x00, 0x00, 0x00 };
            var bytes = BuildDataSetMessage(Td17ModelIdLength, modelId, address, data);
            var correct = ComputeChecksum(bytes, Td17ModelIdLength);
            bytes[bytes.Length - 2] = (byte)((correct + 1) & 0x7f); // corrupt by 1
            Assume.That(bytes[bytes.Length - 2], Is.Not.EqualTo(correct), "Setup: corrupted checksum should differ");
            var message = new MidiMessage(bytes);
            // Currently passes because TryParse does not check checksum — intentional.
            Assert.True(DataSetMessage.TryParse(message, Td17ModelIdLength, out var result),
                "TryParse should still succeed with a bad checksum because production does not validate it (documented)");
            Assert.NotNull(result);
        }

        [Test]
        public void TryParse_WithZeroPlaceholderChecksum_ReturnsTrue()
        {
            // BuildDataSetMessage uses 0x00 as a placeholder checksum; this test explicitly
            // documents that TryParse still succeeds with the placeholder (no validation).
            var bytes = BuildDataSetMessage(Td17ModelIdLength, new byte[] { 0x00, 0x00, 0x00, 0x4b }, new byte[] { 0x00, 0x00, 0x00, 0x00 }, new byte[] { 0x58 });
            Assert.AreEqual(0x00, bytes[bytes.Length - 2], "Precondition: placeholder checksum is 0x00");
            var message = new MidiMessage(bytes);
            Assert.True(DataSetMessage.TryParse(message, Td17ModelIdLength, out var result));
            Assert.NotNull(result);
        }

        private static byte ComputeChecksum(byte[] message, int modelIdLength)
        {
            // Roland checksum: (0x80 - (sum & 0x7f)) & 0x7f over bytes from dataStart to length-3.
            int dataStart = 4 + modelIdLength;
            byte sum = 0;
            for (int i = dataStart; i < message.Length - 2; i++) sum += message[i];
            return (byte)((0x80 - (sum & 0x7f)) & 0x7f);
        }

        // Helper to build a valid DT1 (Data Set) SysEx message.
        // Layout: F0 41 <devId> <modelId (modelIdLength bytes)> 12 <address (4 bytes)> <data> <checksum> F7
        // The modelId parameter provides the 4 bytes of the actual model ID;
        // for modelIdLength=5, a leading 0x00 byte is inserted before the model ID.
        // NOTE: checksum is intentionally left as 0x00 placeholder — DataSetMessage.TryParse does not validate it.
        private static byte[] BuildDataSetMessage(byte modelIdLength, byte[] modelId, byte[] address, byte[] data)
        {
            // Total length: 3 (F0, 41, devId) + modelIdLength + 1 (command) + 4 (address) + data.Length + 1 (checksum) + 1 (F7)
            int length = 3 + modelIdLength + 1 + 4 + data.Length + 1 + 1;
            var bytes = new byte[length];
            int offset = 0;
            bytes[offset++] = 0xF0; // SYSEX start
            bytes[offset++] = 0x41; // Roland manufacturer ID
            bytes[offset++] = 0x10; // Raw device ID (device 17)
            // Model ID: for modelIdLength=5, first byte is 0x00, then the 4 modelId bytes.
            // For modelIdLength=4, just the 4 modelId bytes.
            if (modelIdLength == 5)
            {
                bytes[offset++] = 0x00; // Leading zero byte for TD-50X
            }
            foreach (var b in modelId)
            {
                bytes[offset++] = b;
            }
            bytes[offset++] = 0x12; // DT1 command
            foreach (var b in address)
            {
                bytes[offset++] = b;
            }
            foreach (var b in data)
            {
                bytes[offset++] = b;
            }
            // Checksum placeholder (not validated by TryParse, but included for completeness)
            bytes[offset++] = 0x00;
            bytes[offset++] = 0xF7; // EOX
            return bytes;
        }

        private static byte[] ToBigEndianBytes(int value) =>
            new byte[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)(value >> 0) };
    }
}
