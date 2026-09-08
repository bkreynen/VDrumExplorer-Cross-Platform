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
    /// Fake implementation of <see cref="IMidiAccess"/> for testing <see cref="MidiManager"/>
    /// without OS MIDI hardware. Serves configurable port lists and configurable
    /// open-failure behavior so the retry loop in <see cref="MidiManager.OpenInputAsync"/>
    /// can be exercised deterministically.
    /// </summary>
    public sealed class FakeMidiAccess : IMidiAccess
    {
        public IEnumerable<IMidiPortDetails> Inputs { get; set; } = new List<IMidiPortDetails>();
        public IEnumerable<IMidiPortDetails> Outputs { get; set; } = new List<IMidiPortDetails>();

        // Not used by MidiManager; a fresh instance satisfies the read-only interface member.
        public MidiAccessExtensionManager ExtensionManager { get; } = new MidiAccessExtensionManager();

        public int OpenInputCallCount { get; private set; }
        public int OpenOutputCallCount { get; private set; }

        // When set, OpenInputAsync throws this exception every time (simulates a persistent "device in use" error).
        public Exception? OpenInputException { get; set; }

        // When set, OpenOutputAsync throws this exception every time.
        public Exception? OpenOutputException { get; set; }

        // Number of times OpenInputAsync should throw before succeeding (simulates a transient "device in use" error
        // that clears after N attempts). Ignored while OpenInputException is set.
        public int OpenInputFailuresBeforeSuccess { get; set; } = 0;

        private int openInputFailureCount = 0;

        public Task<IMidiInput> OpenInputAsync(string portId)
        {
            OpenInputCallCount++;
            if (OpenInputException is not null)
            {
                throw OpenInputException;
            }
            if (openInputFailureCount < OpenInputFailuresBeforeSuccess)
            {
                openInputFailureCount++;
                throw new InvalidOperationException("Device in use (simulated)");
            }
            return Task.FromResult<IMidiInput>(new FakeManagedMidiInput { Id = portId });
        }

        public Task<IMidiOutput> OpenOutputAsync(string portId)
        {
            OpenOutputCallCount++;
            if (OpenOutputException is not null)
            {
                throw OpenOutputException;
            }
            return Task.FromResult<IMidiOutput>(new FakeManagedMidiOutput { Id = portId });
        }
    }
}
