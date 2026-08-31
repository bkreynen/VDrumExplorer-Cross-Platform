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

        public async Task<byte[]> RecordAudioAsync(System.TimeSpan duration, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Simulate recording time so that cancellation and timing branches can be exercised.
            // Use BytesPerSecond to produce a realistically sized buffer.
            await Task.Delay(duration, cancellationToken).ConfigureAwait(false);
            var byteCount = (int)(duration.TotalSeconds * AudioFormat.BytesPerSecond);
            // Ensure at least one byte per millisecond for trivial durations used in tests (e.g. 10ms)
            if (byteCount == 0 && duration.TotalMilliseconds > 0)
            {
                byteCount = (int)duration.TotalMilliseconds;
            }
            return new byte[byteCount];
        }
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
