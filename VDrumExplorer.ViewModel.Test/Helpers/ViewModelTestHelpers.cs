// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VDrumExplorer.Model;
using VDrumExplorer.Model.Device;
using VDrumExplorer.Model.Midi;
using VDrumExplorer.ViewModel;
using VDrumExplorer.ViewModel.Test.Fakes;

namespace VDrumExplorer.ViewModel.Test.Helpers
{
    /// <summary>
    /// Centralized helpers for ViewModel tests to reduce duplication of <c>CreateFakeRolandClient</c>,
    /// <c>CreateDeviceViewModel</c>, and <c>WaitUntilAsync</c> helpers previously copy-pasted across
    /// 3-4 test classes.
    /// </summary>
    internal static class ViewModelTestHelpers
    {
        /// <summary>
        /// Polls <paramref name="condition"/> every 20ms until true or <paramref name="timeoutMs"/> elapsed.
        /// Replaces per-file <c>WaitUntilAsync</c> duplicates.
        /// </summary>
        internal static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 1000)
        {
            for (int i = 0; i < timeoutMs / 20; i++)
            {
                if (condition()) return;
                await Task.Delay(20).ConfigureAwait(false);
            }
        }

        internal static RolandMidiClient CreateFakeRolandClient(IMidiInput input, IMidiOutput output, string name, byte id, ModuleIdentifier identifier)
        {
            var type = typeof(RolandMidiClient);
            var ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(IMidiInput), typeof(IMidiOutput), typeof(string), typeof(string), typeof(byte), typeof(ModuleIdentifier) }, null);
            if (ctor is null) throw new InvalidOperationException("RolandMidiClient ctor not found");
            return (RolandMidiClient)ctor.Invoke(new object[] { input, output, name, name, id, identifier });
        }

        internal static DeviceController CreateDeviceController(RolandMidiClient client, ILogger? logger = null, TimeSpan? timeout = null)
        {
            var type = typeof(DeviceController);
            var ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(RolandMidiClient), typeof(ILogger), typeof(TimeSpan) }, null);
            if (ctor is null) throw new InvalidOperationException("DeviceController ctor not found");
            return (DeviceController)ctor.Invoke(new object[] { client, logger ?? NullLogger.Instance, timeout ?? TimeSpan.FromSeconds(1) });
        }

        internal static DeviceViewModel CreateDeviceViewModel(ModuleIdentifier? identifier = null, IMidiOutput? output = null, IMidiInput? input = null, TimeSpan? timeout = null, string midiName = "Test MIDI")
        {
            identifier ??= ModuleIdentifier.TD27;
            input ??= new FakeMidiInput();
            output ??= new FakeMidiOutput();
            var client = CreateFakeRolandClient(input, output, midiName, 0x10, identifier);
            var controller = CreateDeviceController(client, NullLogger.Instance, timeout);
            return new DeviceViewModel { ConnectedDevice = controller };
        }

        internal sealed class FakeMidiInput : IMidiInput
        {
            public event EventHandler<MidiMessage>? MessageReceived;
            public void Dispose() { }
            internal void Raise(MidiMessage msg) => MessageReceived?.Invoke(this, msg);
        }

        internal sealed class FakeMidiOutput : IMidiOutput
        {
            public void Send(MidiMessage message) { }
            public void Dispose() { }
        }

        internal sealed class TrackingMidiOutput : IMidiOutput
        {
            public System.Collections.Generic.List<MidiMessage> Sent { get; } = new();
            public void Send(MidiMessage message) => Sent.Add(message);
            public void Dispose() { }
        }
    }
}
