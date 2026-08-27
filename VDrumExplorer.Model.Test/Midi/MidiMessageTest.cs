// Copyright 2023 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using NUnit.Framework.Legacy;
using VDrumExplorer.Model.Midi;

namespace VDrumExplorer.Model.Test.Midi
{
    /// <summary>
    /// Tests for <see cref="MidiMessage"/>.
    /// </summary>
    public class MidiMessageTest
    {
        [Test]
        public void Constructor_ByteArray_SetsDataAndStatus()
        {
            var bytes = new byte[] { 0x90, 0x3C, 0x40 };
            var message = new MidiMessage(bytes);

            CollectionAssert.AreEqual(bytes, message.Data);
            Assert.AreEqual(0x90, message.Status);
            // Default timestamp is 0.
            Assert.AreEqual(0L, message.Timestamp);
        }

        [Test]
        public void Constructor_ByteArrayAndTimestamp_SetsDataStatusAndTimestamp()
        {
            var bytes = new byte[] { 0x80, 0x3C, 0x00 };
            var message = new MidiMessage(bytes, 12345L);

            CollectionAssert.AreEqual(bytes, message.Data);
            Assert.AreEqual(0x80, message.Status);
            Assert.AreEqual(12345L, message.Timestamp);
        }

        [Test]
        public void Constructor_WithNullTimestamp_SetsTimestampToZero()
        {
            var bytes = new byte[] { 0xC0, 0x00 };
            var message = new MidiMessage(bytes, 0L);

            CollectionAssert.AreEqual(bytes, message.Data);
            Assert.AreEqual(0xC0, message.Status);
            Assert.AreEqual(0L, message.Timestamp);
        }

        [Test]
        public void Constructor_StoresArrayReference_NoDefensiveCopy()
        {
            // MidiMessage stores the array reference directly (no defensive copy).
            // Modifying the original array affects the message's Data.
            var bytes = new byte[] { 0x90, 0x40, 0x60 };
            var message = new MidiMessage(bytes);

            // Modify the original array.
            bytes[0] = 0x80;

            // The message's Data reflects the change (no defensive copy was made).
            Assert.AreEqual(0x80, message.Data[0]);
            Assert.AreEqual(0x80, message.Status);
        }

        [Test]
        public void Constructor_WithTimestamp_ModifyingArrayAffectsMessage()
        {
            var bytes = new byte[] { 0xB0, 0x07, 0x7F };
            var message = new MidiMessage(bytes, 999L);

            bytes[1] = 0x0A;

            Assert.AreEqual(0x0A, message.Data[1]);
            Assert.AreEqual(999L, message.Timestamp);
        }

        [Test]
        public void Status_ReturnsFirstByte()
        {
            // NoteOn status byte
            var noteOn = new MidiMessage(new byte[] { 0x90, 0x3C, 0x40 });
            Assert.AreEqual(0x90, noteOn.Status);

            // NoteOff status byte
            var noteOff = new MidiMessage(new byte[] { 0x80, 0x3C, 0x40 });
            Assert.AreEqual(0x80, noteOff.Status);

            // Control Change status byte
            var cc = new MidiMessage(new byte[] { 0xB0, 0x07, 0x7F });
            Assert.AreEqual(0xB0, cc.Status);

            // Program Change status byte
            var pc = new MidiMessage(new byte[] { 0xC0, 0x00 });
            Assert.AreEqual(0xC0, pc.Status);
        }

        [Test]
        public void Constructor_SingleByte_StatusIsThatByte()
        {
            var message = new MidiMessage(new byte[] { 0xF0 });
            Assert.AreEqual(0xF0, message.Status);
            Assert.AreEqual(1, message.Data.Length);
        }

        [Test]
        public void Constructor_EmptyArray_StatusThrowsIndexOutOfRange()
        {
            // With an empty array, Status accesses Data[0] which throws IndexOutOfRangeException.
            var message = new MidiMessage(new byte[0]);
            Assert.Throws<System.IndexOutOfRangeException>(() =>
            {
                var _ = message.Status;
            });
        }

        [Test]
        public void Constructor_TypicalMidiMessages()
        {
            // NoteOn: channel 1, middle C (60), velocity 100
            var noteOn = new MidiMessage(new byte[] { 0x90, 60, 100 });
            Assert.AreEqual(0x90, noteOn.Status);
            Assert.AreEqual(60, noteOn.Data[1]);
            Assert.AreEqual(100, noteOn.Data[2]);

            // NoteOff: channel 1, middle C (60), velocity 0
            var noteOff = new MidiMessage(new byte[] { 0x80, 60, 0 });
            Assert.AreEqual(0x80, noteOff.Status);

            // Control Change: channel 1, controller 7 (volume), value 127
            var cc = new MidiMessage(new byte[] { 0xB0, 7, 127 });
            Assert.AreEqual(0xB0, cc.Status);
            Assert.AreEqual(7, cc.Data[1]);
            Assert.AreEqual(127, cc.Data[2]);

            // Program Change: channel 1, program 0
            var pc = new MidiMessage(new byte[] { 0xC0, 0 });
            Assert.AreEqual(0xC0, pc.Status);
            Assert.AreEqual(0, pc.Data[1]);
        }

        [Test]
        public void Constructor_NegativeTimestamp_IsPreserved()
        {
            var bytes = new byte[] { 0xF0 };
            var message = new MidiMessage(bytes, -1L);
            Assert.AreEqual(-1L, message.Timestamp);
        }

        [Test]
        public void Constructor_LargeTimestamp_IsPreserved()
        {
            var bytes = new byte[] { 0xF0 };
            var message = new MidiMessage(bytes, long.MaxValue);
            Assert.AreEqual(long.MaxValue, message.Timestamp);
        }
    }
}
