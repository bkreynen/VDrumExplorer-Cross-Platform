// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Collections.Generic;
using VDrumExplorer.Model.Audio;

namespace VDrumExplorer.Gui.Avalonia.Test.Stubs;

/// <summary>
/// Stub implementation of <see cref="IAudioDeviceManager"/> for headless tests, returning
/// no audio devices. Mirrors the internal <c>StubAudioDeviceManager</c> in the GUI project,
/// which is not visible from the test project.
/// </summary>
internal sealed class StubAudioDeviceManager : IAudioDeviceManager
{
    public IReadOnlyList<IAudioInput> GetInputs() => new List<IAudioInput>();

    public IReadOnlyList<IAudioOutput> GetOutputs() => new List<IAudioOutput>();
}
