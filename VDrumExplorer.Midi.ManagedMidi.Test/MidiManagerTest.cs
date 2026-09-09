// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManagedMidi;
using NUnit.Framework;
using VDrumExplorer.Midi.ManagedMidi.Test.Fakes;
using VDrumExplorer.Model.Midi;

namespace VDrumExplorer.Midi.ManagedMidi.Test
{
    public class MidiManagerTest
    {
        [Test]
        public void ListInputDevices_ProjectsFakeInputs()
        {
            var access = new FakeMidiAccess
            {
                Inputs = new List<IMidiPortDetails>
                {
                    new FakePortDetails("in-1", "TD-50 Input", "Roland"),
                },
            };
            var manager = new MidiManager(access);

            var devices = manager.ListInputDevices().ToList();

            Assert.AreEqual(1, devices.Count);
            Assert.AreEqual("in-1", devices[0].SystemDeviceId);
            Assert.AreEqual("TD-50 Input", devices[0].Name);
            // Manufacturer is internal on MidiDeviceBase (not visible to this assembly);
            // it is included in ToString, so assert via that.
            Assert.That(devices[0].ToString(), Does.Contain("Roland"));
        }

        [Test]
        public void ListInputDevices_NoDevices_ReturnsEmpty()
        {
            var access = new FakeMidiAccess { Inputs = new List<IMidiPortDetails>() };
            var manager = new MidiManager(access);

            var devices = manager.ListInputDevices().ToList();

            Assert.IsNotNull(devices);
            Assert.AreEqual(0, devices.Count);
        }

        [Test]
        public void ListInputDevices_MultipleDevices_AllProjected()
        {
            var access = new FakeMidiAccess
            {
                Inputs = new List<IMidiPortDetails>
                {
                    new FakePortDetails("in-1", "TD-50 Input", "Roland"),
                    new FakePortDetails("in-2", "UM-ONE Input", "Roland"),
                    new FakePortDetails("in-3", "Scarlett Input", "Focusrite"),
                },
            };
            var manager = new MidiManager(access);

            var devices = manager.ListInputDevices().ToList();

            Assert.AreEqual(3, devices.Count);
            Assert.That(
                devices.Select(d => d.SystemDeviceId).ToList(),
                Is.EquivalentTo(new[] { "in-1", "in-2", "in-3" }));
            Assert.That(
                devices.Select(d => d.Name).ToList(),
                Is.EquivalentTo(new[] { "TD-50 Input", "UM-ONE Input", "Scarlett Input" }));
        }

        [Test]
        public void ListInputDevices_CanBeEnumeratedMultipleTimes()
        {
            var access = new FakeMidiAccess
            {
                Inputs = new List<IMidiPortDetails>
                {
                    new FakePortDetails("in-1", "TD-50 Input", "Roland"),
                    new FakePortDetails("in-2", "UM-ONE Input", "Roland"),
                },
            };
            var manager = new MidiManager(access);

            var devices = manager.ListInputDevices();
            var first = devices.ToList();
            var second = devices.ToList();

            Assert.AreEqual(first.Count, second.Count);
            // MidiDeviceBase has no Equals override and each enumeration constructs
            // new instances, so compare by identity fields rather than references.
            Assert.That(second.Select(d => (d.SystemDeviceId, d.Name)).ToList(),
                Is.EquivalentTo(first.Select(d => (d.SystemDeviceId, d.Name)).ToList()));
        }

        [Test]
        public void ListOutputDevices_ProjectsFakeOutputs()
        {
            var access = new FakeMidiAccess
            {
                Outputs = new List<IMidiPortDetails>
                {
                    new FakePortDetails("out-1", "TD-50 Output", "Roland"),
                },
            };
            var manager = new MidiManager(access);

            var devices = manager.ListOutputDevices().ToList();

            Assert.AreEqual(1, devices.Count);
            Assert.AreEqual("out-1", devices[0].SystemDeviceId);
            Assert.AreEqual("TD-50 Output", devices[0].Name);
            Assert.That(devices[0].ToString(), Does.Contain("Roland"));
        }

        [Test]
        public void ListOutputDevices_NoDevices_ReturnsEmpty()
        {
            var access = new FakeMidiAccess { Outputs = new List<IMidiPortDetails>() };
            var manager = new MidiManager(access);

            var devices = manager.ListOutputDevices().ToList();

            Assert.IsNotNull(devices);
            Assert.AreEqual(0, devices.Count);
        }

        [Test]
        public void ListOutputDevices_MultipleDevices_AllProjected()
        {
            var access = new FakeMidiAccess
            {
                Outputs = new List<IMidiPortDetails>
                {
                    new FakePortDetails("out-1", "TD-50 Output", "Roland"),
                    new FakePortDetails("out-2", "UM-ONE Output", "Roland"),
                    new FakePortDetails("out-3", "Scarlett Output", "Focusrite"),
                },
            };
            var manager = new MidiManager(access);

            var devices = manager.ListOutputDevices().ToList();

            Assert.AreEqual(3, devices.Count);
            Assert.That(
                devices.Select(d => d.SystemDeviceId).ToList(),
                Is.EquivalentTo(new[] { "out-1", "out-2", "out-3" }));
            Assert.That(
                devices.Select(d => d.Name).ToList(),
                Is.EquivalentTo(new[] { "TD-50 Output", "UM-ONE Output", "Scarlett Output" }));
        }

        [Test]
        public void ListOutputDevices_CanBeEnumeratedMultipleTimes()
        {
            var access = new FakeMidiAccess
            {
                Outputs = new List<IMidiPortDetails>
                {
                    new FakePortDetails("out-1", "TD-50 Output", "Roland"),
                    new FakePortDetails("out-2", "UM-ONE Output", "Roland"),
                },
            };
            var manager = new MidiManager(access);

            var devices = manager.ListOutputDevices();
            var first = devices.ToList();
            var second = devices.ToList();

            Assert.AreEqual(first.Count, second.Count);
            // MidiDeviceBase has no Equals override and each enumeration constructs
            // new instances, so compare by identity fields rather than references.
            Assert.That(second.Select(d => (d.SystemDeviceId, d.Name)).ToList(),
                Is.EquivalentTo(first.Select(d => (d.SystemDeviceId, d.Name)).ToList()));
        }

        [Test]
        public async Task OpenInputAsync_Success_ReturnsNonNullInput()
        {
            var access = new FakeMidiAccess();
            var manager = new MidiManager(access);
            var device = new MidiInputDevice("td50-in", "TD-50", "Roland");

            var input = await manager.OpenInputAsync(device);

            Assert.IsNotNull(input);
            Assert.AreEqual(1, access.OpenInputCallCount);
        }

        [Test]
        public async Task OpenInputAsync_SucceedsAfterTwoFailures()
        {
            // Simulates the transient "device in use" error clearing after 2 attempts:
            // call 1 and 2 throw, call 3 succeeds. The retry loop delays 250ms between
            // attempts, so this test takes ~500ms.
            var access = new FakeMidiAccess { OpenInputFailuresBeforeSuccess = 2 };
            var manager = new MidiManager(access);
            var device = new MidiInputDevice("td50-in", "TD-50", "Roland");

            var input = await manager.OpenInputAsync(device);

            Assert.IsNotNull(input);
            Assert.AreEqual(3, access.OpenInputCallCount);
        }

        [Test]
        public void OpenInputAsync_ExhaustsRetries_ThrowsAfterFourthAttempt()
        {
            // Retry loop: catch when (failures < 3) catches failures 0, 1, 2 (3 catches),
            // then on the 4th attempt failures=3 so the filter is false and the exception
            // propagates. OpenInputAsync is therefore called 4 times in total.
            var access = new FakeMidiAccess
            {
                OpenInputException = new InvalidOperationException("Device in use (persistent)"),
            };
            var manager = new MidiManager(access);
            var device = new MidiInputDevice("td50-in", "TD-50", "Roland");

            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => manager.OpenInputAsync(device));

            Assert.AreEqual("Device in use (persistent)", ex!.Message);
            Assert.AreEqual(4, access.OpenInputCallCount);
        }

        [Test]
        public async Task OpenOutputAsync_Success_ReturnsNonNullOutput()
        {
            var access = new FakeMidiAccess();
            var manager = new MidiManager(access);
            var device = new MidiOutputDevice("td50-out", "TD-50", "Roland");

            var output = await manager.OpenOutputAsync(device);

            Assert.IsNotNull(output);
            Assert.AreEqual(1, access.OpenOutputCallCount);
        }

        [Test]
        public void OpenOutputAsync_ThrowsWhenAccessThrows_NoRetry()
        {
            var access = new FakeMidiAccess
            {
                OpenOutputException = new InvalidOperationException("Output unavailable"),
            };
            var manager = new MidiManager(access);
            var device = new MidiOutputDevice("td50-out", "TD-50", "Roland");

            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => manager.OpenOutputAsync(device));

            Assert.AreEqual("Output unavailable", ex!.Message);
            Assert.AreEqual(1, access.OpenOutputCallCount);
        }
    }
}
