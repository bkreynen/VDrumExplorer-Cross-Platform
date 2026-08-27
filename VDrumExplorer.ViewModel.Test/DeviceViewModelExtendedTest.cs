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

        [Fact]
        public async Task DetectModule_NoDevices_SetsConnectedDeviceNull()
        {
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
