// Copyright 2023 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using VDrumExplorer.Model.Midi;

namespace VDrumExplorer.Model.Test.Helpers
{
    /// <summary>
    /// Shared MIDI SysEx test helpers. Consolidates the duplicated BuildDataSetMessage /
    /// ComputeChecksum logic that was previously triplicated across
    /// DataSetMessageExtendedTest, RolandMidiClientExtendedTest and DeviceControllerTest.
    /// Keeps a single implementation of the Roland DT1 checksum and message layout so
    /// dead-write fixes and modelIdLength handling stay in one place.
    /// </summary>
    internal static class MidiTestHelpers
    {
        /// <summary>
        /// Builds a valid Roland DT1 (Data Set) SysEx message.
        /// Layout: F0 41 &lt;devId&gt; &lt;modelId (modelIdLength bytes)&gt; 12 &lt;address (4)&gt; &lt;data&gt; &lt;checksum&gt; F7
        /// For modelIdLength==5 a leading 0x00 byte is inserted before the 4-byte modelId value.
        /// Checksum is written correctly using <see cref="ComputeChecksum(byte[], int)"/>.
        /// </summary>
        internal static byte[] BuildDataSetMessage(int modelIdLength, byte[] modelId, byte[] address, byte[] data)
        {
            int length = 3 + modelIdLength + 1 + 4 + data.Length + 1 + 1;
            var bytes = new byte[length];
            int offset = 0;
            bytes[offset++] = 0xF0; // SysEx
            bytes[offset++] = 0x41; // Roland
            bytes[offset++] = 0x10; // raw device id (device 17)
            if (modelIdLength == 5)
            {
                bytes[offset++] = 0x00; // leading zero for TD-50X etc.
            }
            foreach (var b in modelId)
            {
                bytes[offset++] = b;
            }
            bytes[offset++] = 0x12; // DT1
            foreach (var b in address)
            {
                bytes[offset++] = b;
            }
            foreach (var b in data)
            {
                bytes[offset++] = b;
            }
            // Compute and write checksum in place (avoids dead-write of placeholder 0x00 then overwrite).
            bytes[bytes.Length - 2] = ComputeChecksum(bytes, modelIdLength);
            bytes[bytes.Length - 1] = 0xF7; // EOX
            return bytes;
        }

        /// <summary>
        /// Builds a DT1 response for a given <see cref="ModuleIdentifier"/> – convenience overload
        /// that uses the identifier's <see cref="ModuleIdentifier.ModelIdLength"/> and writes the
        /// model id big-endian at the offset expected by <see cref="RolandMidiClient"/> (preserves dead-write fix).
        /// </summary>
        internal static byte[] BuildDataSetResponse(ModuleIdentifier id, int address, byte[] data)
        {
            int modelIdLength = id.ModelIdLength;
            int totalLength = 3 + modelIdLength + 1 + 4 + data.Length + 1 + 1;
            var message = new byte[totalLength];
            message[0] = 0xF0;
            message[1] = 0x41;
            message[2] = 0x10;
            // Write model id big-endian at offset (modelIdLength-1). For 5-byte ids index 3 stays 0x00
            // because the array is zero-initialised – matches RolandMidiClient.WriteBigEndianInt32.
            WriteBigEndianInt32(message, modelIdLength - 1, id.ModelId);
            int index = modelIdLength + 3;
            message[index++] = 0x12;
            message[index++] = (byte)(address >> 24);
            message[index++] = (byte)(address >> 16);
            message[index++] = (byte)(address >> 8);
            message[index++] = (byte)(address >> 0);
            foreach (var b in data)
            {
                message[index++] = b;
            }
            message[message.Length - 2] = ComputeChecksum(message, modelIdLength);
            message[message.Length - 1] = 0xF7;
            return message;
        }

        /// <summary>
        /// Builds a DT1 message with a placeholder 0x00 checksum (no validation) – used by tests
        /// that intentionally verify DataSetMessage.TryParse accepts a zero/bad checksum.
        /// Prefer <see cref="BuildDataSetMessage(int,byte[],byte[],byte[])"/> for valid-checksum messages.
        /// </summary>
        internal static byte[] BuildDataSetMessageWithPlaceholderChecksum(int modelIdLength, byte[] modelId, byte[] address, byte[] data)
        {
            var bytes = BuildDataSetMessage(modelIdLength, modelId, address, data);
            bytes[bytes.Length - 2] = 0x00;
            return bytes;
        }

        /// <summary>
        /// Roland checksum: (0x80 - (sum &amp; 0x7f)) &amp; 0x7f over bytes from 4+modelIdLength to length-3 inclusive.
        /// Same as ComputeExpectedChecksum in RolandMidiClientExtendedTest – unified here.
        /// </summary>
        internal static byte ComputeChecksum(byte[] message, int modelIdLength)
        {
            int dataStart = 4 + modelIdLength;
            byte sum = 0;
            for (int i = dataStart; i < message.Length - 2; i++)
            {
                sum += message[i];
            }
            return (byte)((0x80 - (sum & 0x7f)) & 0x7f);
        }

        /// <summary>
        /// Alias kept for readability in tests that previously called ComputeExpectedChecksum.
        /// </summary>
        internal static byte ComputeExpectedChecksum(byte[] message, int modelIdLength) => ComputeChecksum(message, modelIdLength);

        internal static byte[] ToBigEndianBytes(int value) =>
            new byte[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)(value >> 0) };

        private static void WriteBigEndianInt32(byte[] data, int offset, int value)
        {
            unchecked
            {
                data[offset++] = (byte)(value >> 24);
                data[offset++] = (byte)(value >> 16);
                data[offset++] = (byte)(value >> 8);
                data[offset++] = (byte)(value >> 0);
            }
        }
    }
}
