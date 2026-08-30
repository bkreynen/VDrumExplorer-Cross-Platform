// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using NUnit.Framework;
using VDrumExplorer.Midi.ManagedMidi.Test.Fakes;
using VDrumExplorer.Model.Midi;

namespace VDrumExplorer.Midi.ManagedMidi.Test
{
    public class MidiOutputTest
    {
        [Test]
        public void Send_CallsManagedOutputWithCorrectData()
        {
            var fake = new FakeManagedMidiOutput();
            var output = new MidiOutput(fake);

            var data = new byte[] { 0x90, 0x40, 0x7F };
            var message = new Model.Midi.MidiMessage(data, 12345L);
            output.Send(message);

            Assert.AreEqual(1, fake.SentMessages.Count);
            var sent = fake.SentMessages[0];
            Assert.AreEqual(data, sent.Data);
            // FakeManagedMidiOutput.Send defensively copies the buffer — mutating the original must not affect the stored copy.
            data[0] = 0xFF;
            Assert.AreEqual(0x90, sent.Data[0], "Fake should have copied buffer; mutation of original must not leak into SentMessage.Data");
            Assert.AreEqual(0, sent.Offset);
            Assert.AreEqual(3, sent.Length);
            Assert.AreEqual(12345L, sent.Timestamp);
        }

        [Test]
        public void Send_MultipleMessages_AllRecorded()
        {
            var fake = new FakeManagedMidiOutput();
            var output = new MidiOutput(fake);

            var msg1 = new Model.Midi.MidiMessage(new byte[] { 0x90, 0x40, 0x7F }, 1L);
            var msg2 = new Model.Midi.MidiMessage(new byte[] { 0x80, 0x40, 0x00 }, 2L);
            output.Send(msg1);
            output.Send(msg2);

            Assert.AreEqual(2, fake.SentMessages.Count);
            Assert.AreEqual(new byte[] { 0x90, 0x40, 0x7F }, fake.SentMessages[0].Data);
            Assert.AreEqual(1L, fake.SentMessages[0].Timestamp);
            Assert.AreEqual(new byte[] { 0x80, 0x40, 0x00 }, fake.SentMessages[1].Data);
            Assert.AreEqual(2L, fake.SentMessages[1].Timestamp);
        }

        [Test]
        public void Send_EmptyData_DoesNotThrow()
        {
            var fake = new FakeManagedMidiOutput();
            var output = new MidiOutput(fake);

            var message = new Model.Midi.MidiMessage(Array.Empty<byte>(), 0L);
            output.Send(message);

            Assert.AreEqual(1, fake.SentMessages.Count);
            Assert.AreEqual(0, fake.SentMessages[0].Data.Length);
        }

        [Test]
        public void Dispose_CallsManagedOutputDispose()
        {
            var fake = new FakeManagedMidiOutput();
            var output = new MidiOutput(fake);

            output.Dispose();

            Assert.IsTrue(fake.DisposeCalled);
            Assert.AreEqual(1, fake.DisposeCallCount);
        }

        [Test]
        public void Dispose_IsIdempotent()
        {
            var fake = new FakeManagedMidiOutput();
            var output = new MidiOutput(fake);

            output.Dispose();
            output.Dispose();
            output.Dispose();

            Assert.AreEqual(1, fake.DisposeCallCount);
        }

        [Test]
        public void Dispose_SwallowsUnderlyingException()
        {
            var fake = new FakeManagedMidiOutput { DisposeException = new InvalidOperationException("cleanup failed") };
            var output = new MidiOutput(fake);

            Assert.DoesNotThrow(() => output.Dispose());
            Assert.IsTrue(fake.DisposeCalled);
        }

        [Test]
        public void Dispose_AfterExceptionStillIdempotent()
        {
            var fake = new FakeManagedMidiOutput { DisposeException = new InvalidOperationException("cleanup failed") };
            var output = new MidiOutput(fake);

            output.Dispose();
            output.Dispose();

            Assert.AreEqual(1, fake.DisposeCallCount);
        }
    }
}
