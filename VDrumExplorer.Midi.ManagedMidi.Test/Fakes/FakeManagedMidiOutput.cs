// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ManagedMidi;

namespace VDrumExplorer.Midi.ManagedMidi.Test.Fakes
{
    /// <summary>
    /// Fake implementation of <see cref="IMidiOutput"/> for testing the <see cref="MidiOutput"/> wrapper.
    /// Records all sent messages and tracks disposal.
    /// </summary>
    public sealed class FakeManagedMidiOutput : IMidiOutput
    {
        public string Id { get; set; } = "fake-output-id";
        public string Name { get; set; } = "Fake Output";
        public string Manufacturer { get; set; } = "Fake Manufacturer";

        public bool CloseAsyncCalled { get; private set; }
        public bool DisposeCalled { get; private set; }
        public int DisposeCallCount { get; private set; }

        public Exception? DisposeException { get; set; }

        private readonly List<SentMessage> sentMessages = new();

        public IReadOnlyList<SentMessage> SentMessages => sentMessages;

        public IMidiPortDetails Details => new FakePortDetails(Id, Name, Manufacturer);

        public MidiPortConnectionState Connection => MidiPortConnectionState.Open;

        public void Send(byte[] buffer, int offset, int length, long timestamp)
        {
            var data = new byte[length];
            Array.Copy(buffer, offset, data, 0, length);
            sentMessages.Add(new SentMessage(data, offset, length, timestamp));
        }

        public Task CloseAsync()
        {
            CloseAsyncCalled = true;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            DisposeCalled = true;
            DisposeCallCount++;
            if (DisposeException is not null)
            {
                throw DisposeException;
            }
        }

        /// <summary>
        /// A message that was sent to the fake output.
        /// </summary>
        public sealed class SentMessage
        {
            public byte[] Data { get; }
            public int Offset { get; }
            public int Length { get; }
            public long Timestamp { get; }

            public SentMessage(byte[] data, int offset, int length, long timestamp) =>
                (Data, Offset, Length, Timestamp) = (data, offset, length, timestamp);
        }
    }
}
