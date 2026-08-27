// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Linq;
using NUnit.Framework;
using VDrumExplorer.Midi.ManagedMidi.Test.Fakes;
using VDrumExplorer.Model.Midi;

namespace VDrumExplorer.Midi.ManagedMidi.Test
{
    public class MidiInputTest
    {
        [Test]
        public void Constructor_SubscribesToMessageReceived()
        {
            var fake = new FakeManagedMidiInput();
            var input = new MidiInput(fake);
            Model.Midi.MidiMessage? received = null;
            input.MessageReceived += (_, msg) => received = msg;

            fake.SimulateMessage(new byte[] { 1, 2, 3 }, 100L);

            Assert.IsNotNull(received);
            Assert.AreEqual(new byte[] { 1, 2, 3 }, received!.Data);
            Assert.AreEqual(100L, received.Timestamp);
        }

        [Test]
        public void MessageReceived_FullBuffer_UsesDataDirectly()
        {
            var fake = new FakeManagedMidiInput();
            var input = new MidiInput(fake);
            Model.Midi.MidiMessage? received = null;
            input.MessageReceived += (_, msg) => received = msg;

            var data = new byte[] { 0x90, 0x40, 0x7F };
            fake.SimulateMessage(data, 0, data.Length, 42L);

            Assert.IsNotNull(received);
            // When Start=0 and Length=Data.Length, the same array reference is used.
            Assert.AreSame(data, received!.Data);
            Assert.AreEqual(42L, received.Timestamp);
        }

        [Test]
        public void MessageReceived_PartialBuffer_SlicesDataCorrectly()
        {
            var fake = new FakeManagedMidiInput();
            var input = new MidiInput(fake);
            Model.Midi.MidiMessage? received = null;
            input.MessageReceived += (_, msg) => received = msg;

            var data = new byte[] { 0xFF, 0x90, 0x40, 0x7F, 0xFF };
            fake.SimulateMessage(data, start: 1, length: 3, timestamp: 99L);

            Assert.IsNotNull(received);
            Assert.AreEqual(new byte[] { 0x90, 0x40, 0x7F }, received!.Data);
            Assert.AreEqual(99L, received.Timestamp);
        }

        [Test]
        public void MessageReceived_NoSubscriber_DoesNotThrow()
        {
            var fake = new FakeManagedMidiInput();
            var input = new MidiInput(fake);

            Assert.DoesNotThrow(() => fake.SimulateMessage(new byte[] { 1, 2, 3 }, 0L));
        }

        [Test]
        public void Dispose_CallsCloseAsyncOnManagedInput()
        {
            var fake = new FakeManagedMidiInput();
            var input = new MidiInput(fake);

            input.Dispose();

            Assert.IsTrue(fake.CloseAsyncCalled);
            Assert.AreEqual(1, fake.CloseAsyncCallCount);
        }

        [Test]
        public void Dispose_IsIdempotent()
        {
            var fake = new FakeManagedMidiInput();
            var input = new MidiInput(fake);

            input.Dispose();
            input.Dispose();
            input.Dispose();

            Assert.AreEqual(1, fake.CloseAsyncCallCount);
        }

        [Test]
        public void Dispose_SwallowsCloseAsyncException()
        {
            var fake = new FakeManagedMidiInput { CloseAsyncException = new InvalidOperationException("cleanup failed") };
            var input = new MidiInput(fake);

            Assert.DoesNotThrow(() => input.Dispose());
            Assert.IsTrue(fake.CloseAsyncCalled);
        }

        [Test]
        public void Dispose_AfterExceptionStillIdempotent()
        {
            var fake = new FakeManagedMidiInput { CloseAsyncException = new InvalidOperationException("cleanup failed") };
            var input = new MidiInput(fake);

            input.Dispose();
            input.Dispose();

            Assert.AreEqual(1, fake.CloseAsyncCallCount);
        }
    }
}
