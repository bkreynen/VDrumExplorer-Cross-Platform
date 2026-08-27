// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using ManagedMidi;

namespace VDrumExplorer.Midi.ManagedMidi.Test.Fakes
{
    /// <summary>
    /// Fake implementation of <see cref="IMidiPortDetails"/> for testing.
    /// </summary>
    public sealed class FakePortDetails : IMidiPortDetails
    {
        public string Id { get; }
        public string Manufacturer { get; }
        public string Name { get; }
        public string Version { get; }

        public FakePortDetails(string id, string name, string manufacturer, string version = "1.0") =>
            (Id, Name, Manufacturer, Version) = (id, name, manufacturer, version);
    }
}
