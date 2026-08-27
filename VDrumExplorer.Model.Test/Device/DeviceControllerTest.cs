// Copyright 2024 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VDrumExplorer.Model.Device;
using VDrumExplorer.Model.Midi;
using VDrumExplorer.Model.Test.Midi;

namespace VDrumExplorer.Model.Test.Device
{
    public class DeviceControllerTest
    {
        [Test]
        public async Task GetCurrentKitAsync_ReturnsKitNumberFromDeviceResponse()
        {
            var (controller, input, output) = CreateController(ModuleIdentifier.TD17);

            // Start the request for the current kit (address 0, size 1).
            var task = controller.GetCurrentKitAsync(CancellationToken.None);

            // Supply a DataSet response with value 4 (which means kit 5, since it's 0-indexed + 1).
            // Format: F0 41 10 [modelId 4 bytes] 12 [address 4 bytes] [data] [checksum] F7
            var response = BuildDataSetResponse(ModuleIdentifier.TD17, address: 0, data: new byte[] { 0x04 });
            input.SupplyMessage(new MidiMessage(response));

            var kit = await task;
            Assert.AreEqual(5, kit);
        }

        [Test]
        public async Task GetCurrentKitAsync_ReturnsKit1ForZeroValue()
        {
            var (controller, input, output) = CreateController(ModuleIdentifier.TD17);

            var task = controller.GetCurrentKitAsync(CancellationToken.None);

            var response = BuildDataSetResponse(ModuleIdentifier.TD17, address: 0, data: new byte[] { 0x00 });
            input.SupplyMessage(new MidiMessage(response));

            Assert.AreEqual(1, await task);
        }

        [Test]
        public void SetCurrentKitAsync_SendsProgramChange()
        {
            var (controller, input, output) = CreateController(ModuleIdentifier.TD17);

            controller.SetCurrentKitAsync(5, CancellationToken.None);

            // Should send a program change: channel 10, program 4 (kit 5 - 1 = 4).
            Assert.AreEqual(1, output.Messages.Count);
            var message = output.Messages[0].Data;
            // Program Change status: 0xC0 | (channel - 1) = 0xC0 | 0x09 = 0xC9
            Assert.AreEqual(0xC9, message[0]);
            Assert.AreEqual(4, message[1]);
        }

        [Test]
        public void SetCurrentKitAsync_Kit1_SendsProgramZero()
        {
            var (controller, input, output) = CreateController(ModuleIdentifier.TD17);

            controller.SetCurrentKitAsync(1, CancellationToken.None);

            Assert.AreEqual(1, output.Messages.Count);
            var message = output.Messages[0].Data;
            Assert.AreEqual(0xC9, message[0]);
            Assert.AreEqual(0x00, message[1]);
        }

        [Test]
        public void PlayNote_DelegatesToClient()
        {
            var (controller, input, output) = CreateController(ModuleIdentifier.TD17);

            controller.PlayNote(10, 36, 100);

            // PlayNote sends NoteOn followed by NoteOff (2 messages).
            Assert.AreEqual(2, output.Messages.Count);
            var noteOn = output.Messages[0].Data;
            Assert.AreEqual(0x99, noteOn[0]); // NoteOn, channel 10
            Assert.AreEqual(36, noteOn[1]);
            Assert.AreEqual(100, noteOn[2]);
        }

        [Test]
        public void Silence_DelegatesToClient()
        {
            var (controller, input, output) = CreateController(ModuleIdentifier.TD17);

            controller.Silence(10);

            Assert.AreEqual(1, output.Messages.Count);
            var message = output.Messages[0].Data;
            Assert.AreEqual(0xB9, message[0]); // Channel Command, channel 10
            Assert.AreEqual(0x78, message[1]); // All Sounds Off
            Assert.AreEqual(0x00, message[2]);
        }

        [Test]
        public void Schema_ReturnsCorrectSchema()
        {
            var (controller, input, output) = CreateController(ModuleIdentifier.TD17);
            Assert.AreSame(ModuleSchema.KnownSchemas[ModuleIdentifier.TD17].Value, controller.Schema);
        }

        [Test]
        public void InputName_AndOutputName_ReturnClientNames()
        {
            var (controller, input, output) = CreateController(ModuleIdentifier.TD17);
            Assert.AreEqual("TD-17", controller.InputName);
            Assert.AreEqual("TD-17", controller.OutputName);
        }

        [Test]
        public void Dispose_DoesNotThrow()
        {
            var (controller, input, output) = CreateController(ModuleIdentifier.TD17);
            Assert.DoesNotThrow(() => controller.Dispose());
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var (controller, input, output) = CreateController(ModuleIdentifier.TD17);
            controller.Dispose();
            Assert.DoesNotThrow(() => controller.Dispose());
        }

        // Helper to create a DeviceController with fake MIDI input/output.
        private static (DeviceController controller, FakeMidiInput input, FakeMidiOutput output) CreateController(ModuleIdentifier id)
        {
            var input = new FakeMidiInput();
            var output = new FakeMidiOutput();
            var client = new RolandMidiClient(input, output, id.Name, id.Name, 0x10, id);
            var controller = new DeviceController(client, NullLogger.Instance);
            return (controller, input, output);
        }

        // Builds a Roland DataSet (DT1) response message for a given module identifier.
        // Format: F0 41 10 [modelId bytes] 12 [address 4 bytes] [data bytes] [checksum] F7
        private static byte[] BuildDataSetResponse(ModuleIdentifier id, int address, byte[] data)
        {
            int modelIdLength = id.ModelIdLength;
            // Message layout: F0, 41, 10, [modelId bytes], 12, [4 address bytes], [data], [checksum], F7
            int totalLength = 3 + modelIdLength + 1 + 4 + data.Length + 1 + 1;
            var message = new byte[totalLength];
            int index = 0;
            message[index++] = 0xF0; // SysEx
            message[index++] = 0x41; // Roland
            message[index++] = 0x10; // Device ID
            // Model ID: written big-endian across modelIdLength bytes, with the first byte being 0.
            // The RolandMidiClient writes it at offset (modelIdLength - 1) as a big-endian int32.
            // For a 4-byte model ID, that's bytes 3-6. For 5-byte (TD-50X), bytes 3-7.
            // We replicate that logic here.
            message[index++] = 0x00; // leading 0 byte
            // Write the remaining (modelIdLength - 1) bytes of the model ID big-endian.
            // The model ID is stored as an int; we need to write it big-endian starting from the most significant byte.
            // RolandMidiClient.WriteBigEndianInt32 writes at offset (modelIdLength - 1), so for 4-byte it writes at index 3.
            // We've already written the leading 0 at index 3, so now write the rest.
            // Actually, let's just replicate the exact logic: write big-endian int32 at offset (modelIdLength - 1).
            int modelId = id.ModelId;
            int modelIdOffset = modelIdLength - 1;
            // Reset the modelIdOffset position (we wrote 0x00 there, but WriteBigEndianInt32 overwrites it)
            message[modelIdOffset] = (byte)(modelId >> 24);
            message[modelIdOffset + 1] = (byte)(modelId >> 16);
            message[modelIdOffset + 2] = (byte)(modelId >> 8);
            message[modelIdOffset + 3] = (byte)(modelId >> 0);
            index = modelIdLength + 3;
            message[index++] = 0x12; // DT1 (Data Set)
            // Address: big-endian 4 bytes
            message[index++] = (byte)(address >> 24);
            message[index++] = (byte)(address >> 16);
            message[index++] = (byte)(address >> 8);
            message[index++] = (byte)(address >> 0);
            // Data
            foreach (var b in data)
            {
                message[index++] = b;
            }
            // Checksum: sum of bytes from (4 + modelIdLength) to (length - 3), stored as (0x80 - (sum & 0x7f)) & 0x7f
            int dataStart = 4 + modelIdLength;
            byte sum = 0;
            for (int i = dataStart; i < message.Length - 2; i++)
            {
                sum += message[i];
            }
            message[message.Length - 2] = (byte)((0x80 - (sum & 0x7f)) & 0x7f);
            message[message.Length - 1] = 0xF7; // EOX
            return message;
        }
    }
}
