using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using VDrumExplorer.Model;
using VDrumExplorer.Model.Midi;
using VDrumExplorer.ViewModel;
using Xunit;

namespace VDrumExplorer.ViewModel.Test
{
    public class DeviceViewModelExtendedTest
    {
        private sealed class EmptyMidiManager : IMidiManager
        {
            public IEnumerable<MidiInputDevice> ListInputDevices() => Array.Empty<MidiInputDevice>();
            public IEnumerable<MidiOutputDevice> ListOutputDevices() => Array.Empty<MidiOutputDevice>();
            public Task<IMidiInput> OpenInputAsync(MidiInputDevice input) => throw new NotImplementedException();
            public Task<IMidiOutput> OpenOutputAsync(MidiOutputDevice output) => throw new NotImplementedException();
        }

        private sealed class SinglePortMidiManager : IMidiManager
        {
            private readonly MidiInputDevice inputDevice;
            private readonly MidiOutputDevice outputDevice;
            private readonly IMidiInput input;
            private readonly IMidiOutput output;
            public SinglePortMidiManager(string name, IMidiInput input, IMidiOutput output)
            {
                this.input = input;
                this.output = output;
                inputDevice = new MidiInputDevice("id-in", name, "Mfg");
                outputDevice = new MidiOutputDevice("id-out", name, "Mfg");
            }
            public IEnumerable<MidiInputDevice> ListInputDevices() => new[] { inputDevice };
            public IEnumerable<MidiOutputDevice> ListOutputDevices() => new[] { outputDevice };
            public Task<IMidiInput> OpenInputAsync(MidiInputDevice input) => Task.FromResult(this.input);
            public Task<IMidiOutput> OpenOutputAsync(MidiOutputDevice output) => Task.FromResult(this.output);
        }

        private sealed class FakeMidiInput : IMidiInput
        {
            public event EventHandler<MidiMessage>? MessageReceived;
            public void Raise(MidiMessage msg) => MessageReceived?.Invoke(this, msg);
            public void Dispose() { }
        }
        private sealed class FakeMidiOutput : IMidiOutput
        {
            public List<MidiMessage> Sent { get; } = new();
            public void Send(MidiMessage message) => Sent.Add(message);
            public void Dispose() { }
        }

        /// <summary>
        /// Minimal responding fake that replies to the universal identity request (0x7E) with a synthetic TD-27 identity.
        /// This lets <see cref="DeviceViewModel.DetectModule"/> exercise the success path without real MIDI hardware.
        /// </summary>
        private sealed class RespondingFakeInput : IMidiInput
        {
            public event EventHandler<MidiMessage>? MessageReceived;
            public void Raise(MidiMessage msg) => MessageReceived?.Invoke(this, msg);
            public void Dispose() { }
        }

        private sealed class RespondingFakeOutput : IMidiOutput
        {
            private readonly RespondingFakeInput input;
            private readonly byte rawDeviceId;
            private readonly int familyCode;
            private readonly int familyNumberCode;
            private readonly int softwareRevision;
            public List<MidiMessage> Sent { get; } = new();
            public RespondingFakeOutput(RespondingFakeInput input, byte rawDeviceId, int familyCode, int familyNumberCode, int softwareRevision)
            {
                this.input = input;
                this.rawDeviceId = rawDeviceId;
                this.familyCode = familyCode;
                this.familyNumberCode = familyNumberCode;
                this.softwareRevision = softwareRevision;
            }
            public void Send(MidiMessage message)
            {
                Sent.Add(message);
                // Identity request is F0 7E 7F 06 01 F7
                var data = message.Data;
                if (data.Length == 6 && data[0] == 0xF0 && data[1] == 0x7E && data[3] == 0x06 && data[4] == 0x01)
                {
                    var reply = BuildIdentityReply(rawDeviceId, familyCode, familyNumberCode, softwareRevision);
                    // Reply after a short delay so the handler in MidiDevices.ListDeviceIdentities is already subscribed.
                    Task.Delay(10).ContinueWith(_ => input.Raise(new MidiMessage(reply)));
                }
            }
            public void Dispose() { }

            private static byte[] BuildIdentityReply(byte rawDeviceId, int familyCode, int familyNumberCode, int revision)
            {
                // 15 bytes: F0 7E <id> 06 02 <Mfr Roland 0x41> <FC lo> <FC hi> <FNC lo> <FNC hi> <rev b3> <rev b2> <rev b1> <rev b0> F7
                var data = new byte[15];
                data[0] = 0xF0;
                data[1] = 0x7E;
                data[2] = rawDeviceId;
                data[3] = 0x06;
                data[4] = 0x02;
                data[5] = 0x41;
                data[6] = (byte)(familyCode & 0xFF);
                data[7] = (byte)((familyCode >> 8) & 0xFF);
                data[8] = (byte)(familyNumberCode & 0xFF);
                data[9] = (byte)((familyNumberCode >> 8) & 0xFF);
                data[10] = (byte)((revision >> 24) & 0xFF);
                data[11] = (byte)((revision >> 16) & 0xFF);
                data[12] = (byte)((revision >> 8) & 0xFF);
                data[13] = (byte)(revision & 0xFF);
                data[14] = 0xF7;
                return data;
            }
        }

        private sealed class RespondingMidiManager : IMidiManager
        {
            private readonly RespondingFakeInput input;
            private readonly RespondingFakeOutput output;
            private readonly MidiInputDevice inputDevice;
            private readonly MidiOutputDevice outputDevice;
            public RespondingMidiManager(string name, byte rawDeviceId, int familyCode, int familyNumberCode, int softwareRevision)
            {
                input = new RespondingFakeInput();
                output = new RespondingFakeOutput(input, rawDeviceId, familyCode, familyNumberCode, softwareRevision);
                inputDevice = new MidiInputDevice("id-in", name, "Mfg");
                outputDevice = new MidiOutputDevice("id-out", name, "Mfg");
            }
            public IEnumerable<MidiInputDevice> ListInputDevices() => new[] { inputDevice };
            public IEnumerable<MidiOutputDevice> ListOutputDevices() => new[] { outputDevice };
            public Task<IMidiInput> OpenInputAsync(MidiInputDevice device) => Task.FromResult<IMidiInput>(input);
            public Task<IMidiOutput> OpenOutputAsync(MidiOutputDevice device) => Task.FromResult<IMidiOutput>(output);
            public IReadOnlyList<MidiMessage> Sent => output.Sent;
        }

        [Fact]
        public async Task DetectModule_NoDevices_SetsConnectedDeviceNull()
        {
            // Headless CI without hardware: Manager returns no ports, so DetectModule must leave ConnectedDevice null.
            var original = MidiDevices.Manager;
            try
            {
                MidiDevices.Manager = new EmptyMidiManager();
                var vm = new DeviceViewModel();
                await vm.DetectModule(NullLogger.Instance);
                Assert.Null(vm.ConnectedDevice);
                Assert.False(vm.DeviceConnected);
                Assert.Equal("(None)", vm.ConnectedDeviceName);
            }
            finally
            {
                MidiDevices.Manager = original;
            }
        }

        [Fact]
        public async Task DetectModule_WithNoMatchingPorts_SetsNull()
        {
            // Headless: input/output names differ => no common port => still null.
            var original = MidiDevices.Manager;
            try
            {
                // Manager with no common name => DetectRolandMidiClientsAsync yields nothing
                var input = new FakeMidiInput();
                var output = new FakeMidiOutput();
                // Create manager with mismatched names: input "A", output "B" => no common name
                var manager = new MismatchedManager(input, output);
                MidiDevices.Manager = manager;
                var vm = new DeviceViewModel();
                await vm.DetectModule(NullLogger.Instance);
                Assert.Null(vm.ConnectedDevice);
            }
            finally
            {
                MidiDevices.Manager = original;
            }
        }

        [Fact]
        public async Task DetectModule_MultipleCalls_SecondAlsoHandlesNull()
        {
            // Headless: repeated DetectModule with empty manager stays null and does not throw.
            var original = MidiDevices.Manager;
            try
            {
                MidiDevices.Manager = new EmptyMidiManager();
                var vm = new DeviceViewModel();
                await vm.DetectModule(NullLogger.Instance);
                Assert.Null(vm.ConnectedDevice);
                await vm.DetectModule(NullLogger.Instance);
                Assert.Null(vm.ConnectedDevice);
            }
            finally
            {
                MidiDevices.Manager = original;
            }
        }

        [Fact]
        public async Task DetectModule_WithSinglePortButNoIdentity_ReturnsNull()
        {
            // Headless CI without identity response — success path requires hardware/emulated reply, this branch stays null.
            var original = MidiDevices.Manager;
            try
            {
                var input = new FakeMidiInput();
                var output = new FakeMidiOutput();
                var manager = new SinglePortMidiManager("TestPort", input, output);
                MidiDevices.Manager = manager;
                var vm = new DeviceViewModel();
                // ListDeviceIdentities will send identity request and wait 1 sec, but input never replies => 0 identities => null
                await vm.DetectModule(NullLogger.Instance);
                Assert.Null(vm.ConnectedDevice);
            }
            finally
            {
                MidiDevices.Manager = original;
            }
        }

        [Fact]
        public async Task DetectModule_WithRespondingDevice_SetsConnectedDevice()
        {
            // Success path: synthetic identity reply matching TD-27 should create a ConnectedDevice.
            var original = MidiDevices.Manager;
            try
            {
                // TD-27 identifiers: family 0x363, familyNumber 0, revision 0, rawDeviceId 0x10
                var td27 = ModuleIdentifier.TD27;
                var manager = new RespondingMidiManager("TestPort", 0x10, td27.FamilyCode, td27.FamilyNumberCode, td27.SoftwareRevision);
                MidiDevices.Manager = manager;
                var vm = new DeviceViewModel();
                await vm.DetectModule(NullLogger.Instance);
                Assert.NotNull(vm.ConnectedDevice);
                Assert.True(vm.DeviceConnected);
                Assert.Contains("TD-27", vm.ConnectedDeviceName);
                Assert.Equal(td27, vm.ConnectedDevice!.Schema.Identifier);
                // Ensure at least one identity request was sent
                Assert.NotEmpty(manager.Sent);
                Assert.Contains(manager.Sent, m => m.Data.Length == 6 && m.Data[1] == 0x7E && m.Data[4] == 0x01);
            }
            finally
            {
                MidiDevices.Manager = original;
            }
        }

        [Fact]
        public async Task DetectModule_WithRespondingDevice_WrongFamily_SetsNull()
        {
            // Negative case for responding fake: unknown family => no matching schema => still null but proves request/response path ran.
            var original = MidiDevices.Manager;
            try
            {
                var manager = new RespondingMidiManager("TestPort", 0x10, 0x999, 0x999, 0x999);
                MidiDevices.Manager = manager;
                var vm = new DeviceViewModel();
                await vm.DetectModule(NullLogger.Instance);
                Assert.Null(vm.ConnectedDevice);
                Assert.NotEmpty(manager.Sent);
            }
            finally
            {
                MidiDevices.Manager = original;
            }
        }

        private sealed class MismatchedManager : IMidiManager
        {
            private readonly IMidiInput input;
            private readonly IMidiOutput output;
            public MismatchedManager(IMidiInput input, IMidiOutput output) { this.input = input; this.output = output; }
            public IEnumerable<MidiInputDevice> ListInputDevices() => new[] { new MidiInputDevice("in1", "InputA", "Mfg") };
            public IEnumerable<MidiOutputDevice> ListOutputDevices() => new[] { new MidiOutputDevice("out1", "OutputB", "Mfg") };
            public Task<IMidiInput> OpenInputAsync(MidiInputDevice input) => Task.FromResult(this.input);
            public Task<IMidiOutput> OpenOutputAsync(MidiOutputDevice output) => Task.FromResult(this.output);
        }
    }
}
