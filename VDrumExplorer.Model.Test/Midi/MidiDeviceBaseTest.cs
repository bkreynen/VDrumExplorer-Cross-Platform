// Copyright 2023 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using VDrumExplorer.Model.Midi;

namespace VDrumExplorer.Model.Test.Midi
{
    /// <summary>
    /// Tests for <see cref="MidiDeviceBase"/>, <see cref="MidiInputDevice"/>, and <see cref="MidiOutputDevice"/>.
    /// The Manufacturer property is internal, but accessible via InternalsVisibleTo.
    /// </summary>
    public class MidiDeviceBaseTest
    {
        [Test]
        public void MidiInputDevice_Constructor_SetsProperties()
        {
            var device = new MidiInputDevice("sys1", "Input Device", "Roland");

            Assert.AreEqual("sys1", device.SystemDeviceId);
            Assert.AreEqual("Input Device", device.Name);
            Assert.AreEqual("Roland", device.Manufacturer);
        }

        [Test]
        public void MidiOutputDevice_Constructor_SetsProperties()
        {
            var device = new MidiOutputDevice("sys2", "Output Device", "Yamaha");

            Assert.AreEqual("sys2", device.SystemDeviceId);
            Assert.AreEqual("Output Device", device.Name);
            Assert.AreEqual("Yamaha", device.Manufacturer);
        }

        [Test]
        public void MidiInputDevice_ToString_ReturnsExpectedFormat()
        {
            var device = new MidiInputDevice("dev0", "TD-17", "Roland");

            // Format: "{SystemDeviceId}: {Name} ({Manufacturer})"
            Assert.AreEqual("dev0: TD-17 (Roland)", device.ToString());
        }

        [Test]
        public void MidiOutputDevice_ToString_ReturnsExpectedFormat()
        {
            var device = new MidiOutputDevice("dev1", "TD-50X", "Roland");

            Assert.AreEqual("dev1: TD-50X (Roland)", device.ToString());
        }

        [Test]
        public void MidiInputDevice_IsMidiDeviceBase()
        {
            MidiDeviceBase device = new MidiInputDevice("sys", "name", "manufacturer");
            Assert.AreEqual("sys", device.SystemDeviceId);
            Assert.AreEqual("name", device.Name);
        }

        [Test]
        public void MidiOutputDevice_IsMidiDeviceBase()
        {
            MidiDeviceBase device = new MidiOutputDevice("sys", "name", "manufacturer");
            Assert.AreEqual("sys", device.SystemDeviceId);
            Assert.AreEqual("name", device.Name);
        }

        [Test]
        public void Constructor_WithEmptyStrings_SetsEmptyProperties()
        {
            var device = new MidiInputDevice("", "", "");

            Assert.AreEqual("", device.SystemDeviceId);
            Assert.AreEqual("", device.Name);
            Assert.AreEqual("", device.Manufacturer);
            Assert.AreEqual(":  ()", device.ToString());
        }

        [Test]
        public void Constructor_WithSpecialCharacters_PreservesValues()
        {
            var device = new MidiOutputDevice("sys-123", "My Device (USB)", "Korg Inc.");

            Assert.AreEqual("sys-123", device.SystemDeviceId);
            Assert.AreEqual("My Device (USB)", device.Name);
            Assert.AreEqual("Korg Inc.", device.Manufacturer);
            Assert.AreEqual("sys-123: My Device (USB) (Korg Inc.)", device.ToString());
        }
    }
}
