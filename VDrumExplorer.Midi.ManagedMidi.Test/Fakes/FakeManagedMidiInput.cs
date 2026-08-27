// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Threading.Tasks;
using ManagedMidi;

namespace VDrumExplorer.Midi.ManagedMidi.Test.Fakes
{
    /// <summary>
    /// Fake implementation of <see cref="IMidiInput"/> for testing the <see cref="MidiInput"/> wrapper.
    /// Allows tests to simulate incoming MIDI messages and track disposal.
    /// </summary>
    public sealed class FakeManagedMidiInput : IMidiInput
    {
        public string Id { get; set; } = "fake-input-id";
        public string Name { get; set; } = "Fake Input";
        public string Manufacturer { get; set; } = "Fake Manufacturer";
        public bool IsBusy { get; set; } = false;

        public bool CloseAsyncCalled { get; private set; }
        public bool DisposeCalled { get; private set; }
        public int CloseAsyncCallCount { get; private set; }
        public int DisposeCallCount { get; private set; }

        public Exception? CloseAsyncException { get; set; }

        public event EventHandler<MidiReceivedEventArgs>? MessageReceived;

        public IMidiPortDetails Details => new FakePortDetails(Id, Name, Manufacturer);

        public MidiPortConnectionState Connection => MidiPortConnectionState.Open;

        /// <summary>
        /// Simulates a MIDI message being received by raising the <see cref="MessageReceived"/> event.
        /// </summary>
        /// <param name="data">The full data buffer.</param>
        /// <param name="start">The start offset within the data.</param>
        /// <param name="length">The number of bytes from the start.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public void SimulateMessage(byte[] data, int start, int length, long timestamp)
        {
            var args = new MidiReceivedEventArgs
            {
                Data = data,
                Start = start,
                Length = length,
                Timestamp = timestamp,
            };
            MessageReceived?.Invoke(this, args);
        }

        /// <summary>
        /// Simulates a MIDI message with the full buffer (Start=0, Length=Data.Length).
        /// </summary>
        public void SimulateMessage(byte[] data, long timestamp) =>
            SimulateMessage(data, 0, data.Length, timestamp);

        public Task CloseAsync()
        {
            CloseAsyncCalled = true;
            CloseAsyncCallCount++;
            if (CloseAsyncException is not null)
            {
                throw CloseAsyncException;
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            DisposeCalled = true;
            DisposeCallCount++;
        }
    }
}
