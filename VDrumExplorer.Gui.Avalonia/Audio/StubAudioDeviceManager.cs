// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Collections.Generic;
using VDrumExplorer.Model.Audio;

namespace VDrumExplorer.Gui.Avalonia.Audio;

/// <summary>
/// Stub implementation of <see cref="IAudioDeviceManager"/> that returns no audio devices.
/// This is used on Linux where NAudio is not available. Audio recording/playback features
/// will be non-functional until a cross-platform audio implementation is provided.
/// </summary>
internal sealed class StubAudioDeviceManager : IAudioDeviceManager
{
    public IReadOnlyList<IAudioInput> GetInputs() => new List<IAudioInput>();
    public IReadOnlyList<IAudioOutput> GetOutputs() => new List<IAudioOutput>();
}
