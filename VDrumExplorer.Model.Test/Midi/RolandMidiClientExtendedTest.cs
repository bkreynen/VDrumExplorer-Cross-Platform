// Copyright 2023 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using VDrumExplorer.Model.Midi;

namespace VDrumExplorer.Model.Test.Midi
{
    /// <summary>
    /// Extended tests for <see cref="RolandMidiClient"/>, covering PlayNote, Silence,
    /// SendData, cancellation, and disposal.
    /// </summary>
    public class RolandMidiClientExtendedTest
    {
        [Test]
        public void PlayNote_Channel10_Note36_Velocity100()
        {
            var (client, input, output) = CreateClient(ModuleIdentifier.TD17);

            client.PlayNote(10, 36, 100);

            // PlayNote sends NoteOn followed by NoteOff.
            Assert.AreEqual(2, output.Messages.Count);

            // NoteOn: status 0x90 | (channel - 1) = 0x90 | 0x09 = 0x99
            var noteOn = output.Messages[0].Data;
            Assert.AreEqual(0x99, noteOn[0]);
            Assert.AreEqual(36, noteOn[1]);
            Assert.AreEqual(100, noteOn[2]);

            // NoteOff: status 0x80 | (channel - 1) = 0x80 | 0x09 = 0x89
            // NoteOff velocity is fixed at 0x64.
            var noteOff = output.Messages[1].Data;
            Assert.AreEqual(0x89, noteOff[0]);
            Assert.AreEqual(36, noteOff[1]);
            Assert.AreEqual(0x64, noteOff[2]);
        }

        [Test]
        public void PlayNote_Channel1()
        {
            var (client, input, output) = CreateClient(ModuleIdentifier.TD17);

            client.PlayNote(1, 60, 127);

            Assert.AreEqual(2, output.Messages.Count);
            // Channel 1: status 0x90 | 0x00 = 0x90
            Assert.AreEqual(0x90, output.Messages[0].Data[0]);
            Assert.AreEqual(0x80, output.Messages[1].Data[0]);
        }

        [Test]
        public void PlayNote_Channel16()
        {
            var (client, input, output) = CreateClient(ModuleIdentifier.TD17);

            client.PlayNote(16, 60, 1);

            Assert.AreEqual(2, output.Messages.Count);
            // Channel 16: status 0x90 | 0x0f = 0x9f
            Assert.AreEqual(0x9f, output.Messages[0].Data[0]);
            Assert.AreEqual(0x8f, output.Messages[1].Data[0]);
        }

        [Test]
        public void Silence_Channel10()
        {
            var (client, input, output) = CreateClient(ModuleIdentifier.TD17);

            client.Silence(10);

            Assert.AreEqual(1, output.Messages.Count);
            var message = output.Messages[0].Data;
            // Channel Command status: 0xB0 | (channel - 1) = 0xB0 | 0x09 = 0xB9
            Assert.AreEqual(0xB9, message[0]);
            // All Sounds Off is CC 120 (0x78)
            Assert.AreEqual(0x78, message[1]);
            // Value is 0
            Assert.AreEqual(0x00, message[2]);
        }

        [Test]
        public void Silence_Channel1()
        {
            var (client, input, output) = CreateClient(ModuleIdentifier.TD17);

            client.Silence(1);

            Assert.AreEqual(1, output.Messages.Count);
            var message = output.Messages[0].Data;
            // Channel 1: 0xB0 | 0x00 = 0xB0
            Assert.AreEqual(0xB0, message[0]);
            Assert.AreEqual(0x78, message[1]);
            Assert.AreEqual(0x00, message[2]);
        }

        [Test]
        public void SendData_Td17_SingleByte()
        {
            var (client, input, output) = CreateClient(ModuleIdentifier.TD17);
            var data = new byte[] { 0x42 };

            client.SendData(0x00001000, data);

            Assert.AreEqual(1, output.Messages.Count);
            var message = output.Messages[0].Data;

            // Starts with SYSEX (0xF0) and Roland (0x41)
            Assert.AreEqual(0xF0, message[0]);
            Assert.AreEqual(0x41, message[1]);

            // Device ID
            Assert.AreEqual(0x10, message[2]);

            // Model ID for TD-17 (4 bytes): 0x00, 0x00, 0x00, 0x4b
            Assert.AreEqual(0x00, message[3]);
            Assert.AreEqual(0x00, message[4]);
            Assert.AreEqual(0x00, message[5]);
            Assert.AreEqual(0x4b, message[6]);

            // Command byte is DT1 (0x12)
            Assert.AreEqual(0x12, message[7]);

            // Address: 0x00001000 in big-endian = 0x00, 0x00, 0x10, 0x00
            Assert.AreEqual(0x00, message[8]);
            Assert.AreEqual(0x00, message[9]);
            Assert.AreEqual(0x10, message[10]);
            Assert.AreEqual(0x00, message[11]);

            // Data
            Assert.AreEqual(0x42, message[12]);

            // Ends with EOX (0xF7)
            Assert.AreEqual(0xF7, message[message.Length - 1]);

            // Checksum is correct — derive modelIdLength from the identifier under test, not by scanning for 0x12.
            Assert.AreEqual(ComputeExpectedChecksum(message, ModuleIdentifier.TD17.ModelIdLength), message[message.Length - 2]);
        }

        [Test]
        public void SendData_Td50X_MultiByte()
        {
            var (client, input, output) = CreateClient(ModuleIdentifier.TD50X);
            var data = new byte[] { 0x01, 0x02, 0x03 };

            client.SendData(0x10000000, data);

            Assert.AreEqual(1, output.Messages.Count);
            var message = output.Messages[0].Data;

            // Starts with SYSEX and Roland
            Assert.AreEqual(0xF0, message[0]);
            Assert.AreEqual(0x41, message[1]);

            // TD-50X has a 5-byte model ID: leading 0x00, then 0x00, 0x00, 0x00, 0x07
            Assert.AreEqual(0x00, message[3]);
            Assert.AreEqual(0x00, message[4]);
            Assert.AreEqual(0x00, message[5]);
            Assert.AreEqual(0x00, message[6]);
            Assert.AreEqual(0x07, message[7]);

            // Command byte is DT1 (0x12) at index ModelIdLength + 3 = 8
            Assert.AreEqual(0x12, message[8]);

            // Address at indices 9-12
            Assert.AreEqual(0x10, message[9]);
            Assert.AreEqual(0x00, message[10]);
            Assert.AreEqual(0x00, message[11]);
            Assert.AreEqual(0x00, message[12]);

            // Data at indices 13-15
            Assert.AreEqual(0x01, message[13]);
            Assert.AreEqual(0x02, message[14]);
            Assert.AreEqual(0x03, message[15]);

            // Ends with EOX
            Assert.AreEqual(0xF7, message[message.Length - 1]);

            // Checksum is correct — use id.ModelIdLength directly.
            Assert.AreEqual(ComputeExpectedChecksum(message, ModuleIdentifier.TD50X.ModelIdLength), message[message.Length - 2]);
        }

        [Test]
        public void SendData_VerifyChecksumCalculation()
        {
            var (client, input, output) = CreateClient(ModuleIdentifier.TD17);
            var data = new byte[] { 0x7f, 0x7f, 0x7f };

            client.SendData(0x7f7f7f7f, data);

            var message = output.Messages[0].Data;
            // Manually compute checksum: sum of bytes from dataStart to length-3, then (0x80 - (sum & 0x7f)) & 0x7f
            var expectedChecksum = ComputeExpectedChecksum(message, ModuleIdentifier.TD17.ModelIdLength);
            Assert.AreEqual(expectedChecksum, message[message.Length - 2]);
        }

        [Test]
        public void SendData_WithPayloadContainingCommandByte_DoesNotConfuseChecksum()
        {
            // Guard against the previous scan fragility: payload containing 0x12 should not
            // be mistaken for the command byte when computing the checksum.
            var (client, input, output) = CreateClient(ModuleIdentifier.TD17);
            var data = new byte[] { 0x12, 0x11, 0x12 }; // contains command-like bytes
            client.SendData(0x00001000, data);
            var message = output.Messages[0].Data;
            var expected = ComputeExpectedChecksum(message, ModuleIdentifier.TD17.ModelIdLength);
            Assert.AreEqual(expected, message[message.Length - 2],
                "Checksum should be derived from modelIdLength, not by scanning for 0x12 in payload");
            // Also verify command byte is still at the correct fixed offset
            Assert.AreEqual(0x12, message[ModuleIdentifier.TD17.ModelIdLength + 3]);
        }

        [Test]
        public void RequestDataAsync_PreCancelledToken_ThrowsTaskCanceledException()
        {
            var (client, input, output) = CreateClient(ModuleIdentifier.TD17);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // A pre-cancelled token should cause the task to be cancelled.
            // The request message is still sent before the cancellation is observed.
            Assert.ThrowsAsync<TaskCanceledException>(() => client.RequestDataAsync(0, 1, cts.Token));
        }

        [Test]
        public void RequestDataAsync_InvalidSize_ThrowsArgumentOutOfRangeException()
        {
            var (client, input, output) = CreateClient(ModuleIdentifier.TD17);

            // Size 0 is below the minimum of 1.
            Assert.Throws<System.ArgumentOutOfRangeException>(() => client.RequestDataAsync(0, 0, default));
            // Size 0x180 is above the maximum of 0x17f.
            Assert.Throws<System.ArgumentOutOfRangeException>(() => client.RequestDataAsync(0, 0x180, default));
        }

        [Test]
        public void Dispose_DoesNotThrow()
        {
            var (client, input, output) = CreateClient(ModuleIdentifier.TD17);
            Assert.DoesNotThrow(() => client.Dispose());
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var (client, input, output) = CreateClient(ModuleIdentifier.TD17);
            client.Dispose();
            // The FakeMidiInput/FakeMidiOutput Dispose methods are no-ops, so this should not throw.
            Assert.DoesNotThrow(() => client.Dispose());
        }

        [Test]
        public void Identifier_IsSetCorrectly()
        {
            var (client, input, output) = CreateClient(ModuleIdentifier.TD17);
            Assert.AreSame(ModuleIdentifier.TD17, client.Identifier);
        }

        [Test]
        public void InputName_AndOutputName_AreSetCorrectly()
        {
            var (client, input, output) = CreateClient(ModuleIdentifier.TD17);
            Assert.AreEqual("TD-17", client.InputName);
            Assert.AreEqual("TD-17", client.OutputName);
        }

        // Helper to create a client with the standard fake input/output and device ID 0x10.
        private static (RolandMidiClient client, FakeMidiInput input, FakeMidiOutput output) CreateClient(ModuleIdentifier id)
        {
            var input = new FakeMidiInput();
            var output = new FakeMidiOutput();
            var client = new RolandMidiClient(input, output, id.Name, id.Name, 0x10, id);
            return (client, input, output);
        }

        // Computes the Roland checksum for a SysEx message.
        // The checksum covers bytes from index (4 + modelIdLength) to length - 3 (inclusive),
        // and is stored at length - 2 as (0x80 - (sum & 0x7f)) & 0x7f.
        // modelIdLength is derived from the identifier under test (id.ModelIdLength), not by
        // scanning for 0x12/0x11 in the payload — scanning is fragile if data contains those bytes.
        private static byte ComputeExpectedChecksum(byte[] message, int modelIdLength)
        {
            int dataStart = 4 + modelIdLength;
            byte sum = 0;
            for (int i = dataStart; i < message.Length - 2; i++)
            {
                sum += message[i];
            }
            return (byte)((0x80 - (sum & 0x7f)) & 0x7f);
        }

        // Backwards-compatible overload for any legacy callers (should not be used in new code).
        private static byte ComputeExpectedChecksum(byte[] message) => ComputeExpectedChecksum(message, 4);
    }
}
