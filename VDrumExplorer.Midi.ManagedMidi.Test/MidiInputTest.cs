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
        public void MessageReceived_WithOffset_SlicesData()
        {
            var fake = new FakeManagedMidiInput();
            var midiInput = new MidiInput(fake);
            Model.Midi.MidiMessage? received = null;
            midiInput.MessageReceived += (_, m) => received = m;

            // TF: Length == Data.Length (5==5 true) && Start == 0 (1==0 false) => false => else branch via Start != 0.
            // Data is 5 bytes, Start 1, Length 5 => Skip(1).Take(5) yields 4 bytes {F0,41,10,F7}.
            // This proves the TF branch (first operand true, second false) is covered distinct from FT (false/true) and FF (false/false).
            fake.SimulateMessageWithOffset(new byte[] { 0x00, 0xF0, 0x41, 0x10, 0xF7 }, start: 1, length: 5);

            Assert.NotNull(received);
            Assert.AreEqual(new byte[] { 0xF0, 0x41, 0x10, 0xF7 }, received!.Data);
        }

        [Test]
        public void MessageReceived_SameLengthNonZeroStart_SlicesCorrectly()
        {
            var fake = new FakeManagedMidiInput();
            var input = new MidiInput(fake);
            Model.Midi.MidiMessage? received = null;
            input.MessageReceived += (_, msg) => received = msg;

            // Data.Length == Length (3 == 3) but Start != 0, so condition is (true && false) => false => else branch.
            // Covers the second sub-condition jump that was previously at 50% coverage.
            var data = new byte[] { 0x90, 0x40, 0x7F };
            fake.SimulateMessage(data, start: 1, length: 3, timestamp: 77L);

            Assert.IsNotNull(received);
            // Skip(1).Take(3) on a 3-element array yields 2 elements.
            Assert.AreEqual(new byte[] { 0x40, 0x7F }, received!.Data);
            Assert.AreEqual(77L, received.Timestamp);
        }

        [Test]
        public void MessageReceived_StartZeroButShorterLength_SlicesCorrectly()
        {
            var fake = new FakeManagedMidiInput();
            var input = new MidiInput(fake);
            Model.Midi.MidiMessage? received = null;
            input.MessageReceived += (_, msg) => received = msg;

            // Start == 0 but Length != Data.Length (3 != 4) => first condition false => else branch via Skip/Take.
            var data = new byte[] { 0x90, 0x40, 0x7F, 0x00 };
            fake.SimulateMessage(data, start: 0, length: 3, timestamp: 55L);

            Assert.IsNotNull(received);
            Assert.AreEqual(new byte[] { 0x90, 0x40, 0x7F }, received!.Data);
            Assert.AreEqual(55L, received.Timestamp);
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

        [Test]
        public void Dispose_Unsubscribes_MessageNoLongerForwarded()
        {
            var fake = new FakeManagedMidiInput();
            var input = new MidiInput(fake);
            Model.Midi.MidiMessage? received = null;
            input.MessageReceived += (_, msg) => received = msg;

            input.Dispose();

            // After Dispose, the wrapper unsubscribes from the underlying managed input.
            // Subsequent messages from the managed layer must NOT be forwarded to the wrapper's subscribers.
            fake.SimulateMessage(new byte[] { 1, 2, 3 }, 123L);

            Assert.IsNull(received, "MessageReceived should not be forwarded after Dispose (wrapper unsubscribed)");
        }

        [Test]
        public void Dispose_DoesNotUnsubscribe_MessageStillForwarded_Documentation()
        {
            // Documentation of previous behavior (pre-fix): without unsubscribe, MessageReceived would still forward
            // after Dispose, leaking messages. The production fix now unsubscribes (see MidiInput.Dispose),
            // so this test verifies the post-fix contract: after Dispose, no forwarding occurs.
            // If this test fails, it means the unsubscribe was removed or broken.
            var fake = new FakeManagedMidiInput();
            var input = new MidiInput(fake);
            int count = 0;
            input.MessageReceived += (_, __) => count++;
            input.Dispose();
            fake.SimulateMessage(new byte[] { 9, 9, 9 }, 999L);
            Assert.AreEqual(0, count, "Post-fix: Dispose must unsubscribe, so count must stay 0; pre-fix would have been 1");
        }
    }
}
