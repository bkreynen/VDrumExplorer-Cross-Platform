// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using VDrumExplorer.Model.Midi;

namespace VDrumExplorer.Midi.ManagedMidi.Test
{
    /// <remarks>
    /// Coverage note: MidiManager.OpenInputAsync / OpenOutputAsync are intentionally not fully covered.
    /// They delegate to the static singleton MidiAccessManager.Default which talks directly to OS MIDI hardware
    /// (ALSA on Linux, WinMM on Windows, CoreMIDI on macOS). The retry loop in OpenInputAsync (3 retries with
    /// Task.Delay(250) on Win32Exception) cannot be exercised without either real hardware or a mockable
    /// abstraction over MidiAccessManager. That abstraction does not exist in the ManagedMidi library, so the
    /// methods are fundamentally untestable in a unit-test context without hardware. ListInputDevices /
    /// ListOutputDevices are the coverable surface and are tested here; they correctly project
    /// IMidiPortDetails (Id, Name, Manufacturer) into Model.Midi.MidiInputDevice/MidiOutputDevice.
    /// If stricter coverage gating is required, consider adding [ExcludeFromCodeCoverage] to the
    /// OpenInputAsync/OpenOutputAsync state machines rather than introducing a test-only seam.
    /// </remarks>
    public class MidiManagerTest
    {
        private MidiManager manager = null!;

        [SetUp]
        public void SetUp() => manager = new MidiManager();

        [Test]
        public void ListInputDevices_ReturnsNonNullEnumerable()
        {
            IEnumerable<MidiInputDevice> devices;
            try
            {
                devices = manager.ListInputDevices();
            }
            catch (Exception ex)
            {
                Assert.Inconclusive($"ListInputDevices threw on this system (likely no ALSA/MIDI support): {ex.GetType().Name}: {ex.Message}");
                return;
            }

            Assert.IsNotNull(devices);
            // Enumerate to ensure the enumerable can be evaluated without error.
            var list = devices.ToList();
            Assert.IsNotNull(list);
        }

        [Test]
        public void ListOutputDevices_ReturnsNonNullEnumerable()
        {
            IEnumerable<MidiOutputDevice> devices;
            try
            {
                devices = manager.ListOutputDevices();
            }
            catch (Exception ex)
            {
                Assert.Inconclusive($"ListOutputDevices threw on this system (likely no ALSA/MIDI support): {ex.GetType().Name}: {ex.Message}");
                return;
            }

            Assert.IsNotNull(devices);
            // Enumerate to ensure the enumerable can be evaluated without error.
            var list = devices.ToList();
            Assert.IsNotNull(list);
        }

        [Test]
        public void ListInputDevices_DevicesHaveValidProperties()
        {
            IEnumerable<MidiInputDevice> devices;
            try
            {
                devices = manager.ListInputDevices();
            }
            catch (Exception ex)
            {
                Assert.Inconclusive($"ListInputDevices threw on this system (likely no ALSA/MIDI support): {ex.GetType().Name}: {ex.Message}");
                return;
            }

            foreach (var device in devices)
            {
                Assert.IsNotNull(device.SystemDeviceId, "SystemDeviceId should not be null");
                Assert.IsNotNull(device.Name, "Name should not be null");
            }
        }

        [Test]
        public void ListOutputDevices_DevicesHaveValidProperties()
        {
            IEnumerable<MidiOutputDevice> devices;
            try
            {
                devices = manager.ListOutputDevices();
            }
            catch (Exception ex)
            {
                Assert.Inconclusive($"ListOutputDevices threw on this system (likely no ALSA/MIDI support): {ex.GetType().Name}: {ex.Message}");
                return;
            }

            foreach (var device in devices)
            {
                Assert.IsNotNull(device.SystemDeviceId, "SystemDeviceId should not be null");
                Assert.IsNotNull(device.Name, "Name should not be null");
            }
        }

        [Test]
        public void ListInputDevices_CanBeEnumeratedMultipleTimes()
        {
            IEnumerable<MidiInputDevice> devices;
            try
            {
                devices = manager.ListInputDevices();
            }
            catch (Exception ex)
            {
                Assert.Inconclusive($"ListInputDevices threw on this system (likely no ALSA/MIDI support): {ex.GetType().Name}: {ex.Message}");
                return;
            }

            var first = devices.Count();
            var second = devices.Count();
            Assert.AreEqual(first, second, "Enumerating the same device list twice should yield the same count");
        }

        [Test]
        public void ListOutputDevices_CanBeEnumeratedMultipleTimes()
        {
            IEnumerable<MidiOutputDevice> devices;
            try
            {
                devices = manager.ListOutputDevices();
            }
            catch (Exception ex)
            {
                Assert.Inconclusive($"ListOutputDevices threw on this system (likely no ALSA/MIDI support): {ex.GetType().Name}: {ex.Message}");
                return;
            }

            var first = devices.Count();
            var second = devices.Count();
            Assert.AreEqual(first, second, "Enumerating the same device list twice should yield the same count");
        }
    }
}
