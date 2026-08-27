// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VDrumExplorer.Model.Audio;

namespace VDrumExplorer.ViewModel.Test.Fakes
{
    /// <summary>
    /// Minimal implementation of <see cref="IAudioDeviceManager"/> for testing purposes.
    /// Returns empty lists for both inputs and outputs.
    /// </summary>
    internal sealed class FakeAudioDeviceManager : IAudioDeviceManager
    {
        private readonly IReadOnlyList<IAudioInput> inputs;
        private readonly IReadOnlyList<IAudioOutput> outputs;

        internal FakeAudioDeviceManager() : this(new List<IAudioInput>(), new List<IAudioOutput>())
        {
        }

        internal FakeAudioDeviceManager(IReadOnlyList<IAudioInput> inputs, IReadOnlyList<IAudioOutput> outputs)
        {
            this.inputs = inputs;
            this.outputs = outputs;
        }

        public IReadOnlyList<IAudioInput> GetInputs() => inputs;

        public IReadOnlyList<IAudioOutput> GetOutputs() => outputs;
    }

    /// <summary>
    /// Minimal implementation of <see cref="IAudioInput"/> for testing purposes.
    /// </summary>
    internal sealed class FakeAudioInput : IAudioInput
    {
        public string Name { get; }
        public AudioFormat AudioFormat { get; }

        internal FakeAudioInput(string name) : this(name, new AudioFormat(44100, 1, 16))
        {
        }

        internal FakeAudioInput(string name, AudioFormat format)
        {
            Name = name;
            AudioFormat = format;
        }

        public Task<byte[]> RecordAudioAsync(System.TimeSpan duration, CancellationToken cancellationToken) =>
            Task.FromResult(System.Array.Empty<byte>());
    }

    /// <summary>
    /// Minimal implementation of <see cref="IAudioOutput"/> for testing purposes.
    /// </summary>
    internal sealed class FakeAudioOutput : IAudioOutput
    {
        public string Name { get; }

        internal FakeAudioOutput(string name)
        {
            Name = name;
        }

        public Task PlayAudioAsync(AudioFormat format, byte[] bytes, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
