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
using VDrumExplorer.Model.Test.Helpers;
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

        // Thin wrapper over the shared helper — preserves the dead-write fix (writing
        // model id at offset modelIdLength-1 so index 3 stays 0x00 for 5-byte ids).
        private static byte[] BuildDataSetResponse(ModuleIdentifier id, int address, byte[] data) =>
            MidiTestHelpers.BuildDataSetResponse(id, address, data);
    }
}
