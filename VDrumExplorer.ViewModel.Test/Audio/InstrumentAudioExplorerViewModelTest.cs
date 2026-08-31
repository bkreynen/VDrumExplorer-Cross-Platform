// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VDrumExplorer.Model;
using VDrumExplorer.Model.Audio;
using VDrumExplorer.ViewModel.Audio;
using VDrumExplorer.ViewModel.Test.Fakes;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Audio
{
    public class InstrumentAudioExplorerViewModelTest
    {
        private static ModuleAudio CreateModuleAudio(ModuleSchema? schema = null, TimeSpan? duration = null, AudioFormat? format = null)
        {
            schema ??= TestData.LoadTD27Schema();
            format ??= new AudioFormat(44100, 2, 16);
            duration ??= TimeSpan.FromSeconds(1.5);

            // Use first few preset groups, each with first instrument, to create captures
            var captures = new List<InstrumentAudio>();
            var presetGroups = schema.InstrumentGroups.Where(g => g.Preset).Take(3).ToList();
            foreach (var group in presetGroups)
            {
                var instrument = group.Instruments[0];
                captures.Add(new InstrumentAudio(instrument, new byte[] { 1, 2, 3 }));
                // Add a second instrument from same group if available
                if (group.Instruments.Count > 1)
                {
                    captures.Add(new InstrumentAudio(group.Instruments[1], new byte[] { 4, 5, 6 }));
                }
            }
            // Add user sample if available to test UserSampleCount
            if (schema.UserSampleInstruments.Count > 0)
            {
                var usInstrument = schema.UserSampleInstruments[0];
                captures.Add(new InstrumentAudio(usInstrument, new byte[] { 9, 9 }));
            }
            return new ModuleAudio(schema, format, duration.Value, captures);
        }

        private sealed class TrackingAudioOutput : IAudioOutput
        {
            public string Name { get; }
            public int PlayCallCount { get; private set; }
            public AudioFormat? LastFormat { get; private set; }
            public byte[]? LastBytes { get; private set; }

            public TrackingAudioOutput(string name) => Name = name;

            public Task PlayAudioAsync(AudioFormat format, byte[] bytes, CancellationToken cancellationToken)
            {
                PlayCallCount++;
                LastFormat = format;
                LastBytes = bytes;
                return Task.CompletedTask;
            }
        }

        [Fact]
        public void Constructor_Title_WithoutFile_ContainsSchemaName()
        {
            var schema = TestData.LoadTD27Schema();
            var audio = CreateModuleAudio(schema);
            var vm = new InstrumentAudioExplorerViewModel(new FakeAudioDeviceManager(), audio, null);
            Assert.Contains(schema.Identifier.Name, vm.Title);
            Assert.Contains("Instrument Audio Explorer", vm.Title);
            Assert.DoesNotContain(" - ", vm.Title); // No file suffix when null; Title format is "Instrument Audio Explorer (TD-27 rev 0x...)"
        }

        [Fact]
        public void Constructor_Title_WithFile_ContainsFileName()
        {
            var audio = CreateModuleAudio();
            var vm = new InstrumentAudioExplorerViewModel(new FakeAudioDeviceManager(), audio, "/tmp/test.bin");
            Assert.Contains("/tmp/test.bin", vm.Title);
            Assert.Contains("Instrument Audio Explorer", vm.Title);
        }

        [Fact]
        public void Constructor_ModuleName_MatchesSchema()
        {
            var schema = TestData.LoadTD27Schema();
            var audio = CreateModuleAudio(schema);
            var vm = new InstrumentAudioExplorerViewModel(new FakeAudioDeviceManager(), audio, null);
            Assert.Equal(schema.Identifier.Name, vm.ModuleName);
        }

        [Fact]
        public void Constructor_AudioFormat_ContainsChannelsBitsFrequency()
        {
            var format = new AudioFormat(48000, 1, 24);
            var audio = CreateModuleAudio(format: format);
            var vm = new InstrumentAudioExplorerViewModel(new FakeAudioDeviceManager(), audio, null);
            Assert.Contains("48000", vm.AudioFormat);
            Assert.Contains("1", vm.AudioFormat);
            Assert.Contains("24", vm.AudioFormat);
        }

        [Fact]
        public void Constructor_OutputDevices_FromManager()
        {
            var output1 = new FakeAudioOutput("Out1");
            var output2 = new FakeAudioOutput("Out2");
            var manager = new FakeAudioDeviceManager(new List<IAudioInput>(), new List<IAudioOutput> { output1, output2 });
            var audio = CreateModuleAudio();
            var vm = new InstrumentAudioExplorerViewModel(manager, audio, null);
            Assert.Equal(2, vm.OutputDevices.Count);
            Assert.Same(output1, vm.OutputDevices[0]);
            Assert.Same(output2, vm.OutputDevices[1]);
        }

        [Fact]
        public void Constructor_SelectedOutputDevice_DefaultsToFirst()
        {
            var output1 = new FakeAudioOutput("Out1");
            var output2 = new FakeAudioOutput("Out2");
            var manager = new FakeAudioDeviceManager(new List<IAudioInput>(), new List<IAudioOutput> { output1, output2 });
            var audio = CreateModuleAudio();
            var vm = new InstrumentAudioExplorerViewModel(manager, audio, null);
            Assert.Same(output1, vm.SelectedOutputDevice);
        }

        [Fact]
        public void Constructor_SelectedOutputDevice_NoOutputs_IsNull()
        {
            var audio = CreateModuleAudio();
            var vm = new InstrumentAudioExplorerViewModel(new FakeAudioDeviceManager(), audio, null);
            Assert.Null(vm.SelectedOutputDevice);
        }

        [Fact]
        public void Constructor_Groups_GroupedByInstrumentGroup()
        {
            var schema = TestData.LoadTD27Schema();
            var audio = CreateModuleAudio(schema);
            var vm = new InstrumentAudioExplorerViewModel(new FakeAudioDeviceManager(), audio, null);
            Assert.NotEmpty(vm.Groups);
            // Each group should correspond to a distinct InstrumentGroup
            var distinctGroups = vm.Groups.Select(g => g.Group).Distinct().Count();
            Assert.Equal(vm.Groups.Count, distinctGroups);
            // All captures should be accounted for
            var totalAudio = vm.Groups.Sum(g => g.Audio.Count);
            Assert.Equal(audio.Captures.Count, totalAudio);
        }

        [Fact]
        public void Constructor_SelectedGroup_DefaultIsFirst()
        {
            var audio = CreateModuleAudio();
            var vm = new InstrumentAudioExplorerViewModel(new FakeAudioDeviceManager(), audio, null);
            Assert.Same(vm.Groups[0], vm.SelectedGroup);
        }

        [Fact]
        public void Constructor_SelectedGroup_NoGroups_IsNull()
        {
            var schema = TestData.LoadTD27Schema();
            var format = new AudioFormat(44100, 1, 16);
            var audio = new ModuleAudio(schema, format, TimeSpan.FromSeconds(1), new List<InstrumentAudio>());
            var vm = new InstrumentAudioExplorerViewModel(new FakeAudioDeviceManager(), audio, null);
            Assert.Empty(vm.Groups);
            Assert.Null(vm.SelectedGroup);
        }

        [Fact]
        public void SelectedGroup_SetToDifferentValue_ResetsSelectedAudio()
        {
            var audio = CreateModuleAudio();
            var vm = new InstrumentAudioExplorerViewModel(new FakeAudioDeviceManager(), audio, null);
            var secondGroup = vm.Groups.Count > 1 ? vm.Groups[1] : vm.Groups[0];
            vm.SelectedAudio = vm.Groups[0].Audio[0];
            Assert.NotNull(vm.SelectedAudio);
            vm.SelectedGroup = secondGroup;
            Assert.Null(vm.SelectedAudio);
            Assert.Same(secondGroup, vm.SelectedGroup);
        }

        [Fact]
        public void SelectedGroup_SetToNull_ClearsSelectedAudio()
        {
            var audio = CreateModuleAudio();
            var vm = new InstrumentAudioExplorerViewModel(new FakeAudioDeviceManager(), audio, null);
            vm.SelectedAudio = vm.Groups[0].Audio[0];
            vm.SelectedGroup = null;
            Assert.Null(vm.SelectedGroup);
            Assert.Null(vm.SelectedAudio);
        }

        [Fact]
        public void SelectedGroup_SetSameValue_DoesNotFireExtraChange()
        {
            var audio = CreateModuleAudio();
            var vm = new InstrumentAudioExplorerViewModel(new FakeAudioDeviceManager(), audio, null);
            var handler = new List<string>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => handler.Add(e.PropertyName!);
            var current = vm.SelectedGroup;
            handler.Clear();
            vm.SelectedGroup = current;
            // SetProperty with same value returns false, so no change for SelectedGroup and no reset of SelectedAudio
            Assert.Empty(handler);
        }

        [Fact]
        public void DurationSeconds_MatchesModel()
        {
            var duration = TimeSpan.FromSeconds(2.5);
            var audio = CreateModuleAudio(duration: duration);
            var vm = new InstrumentAudioExplorerViewModel(new FakeAudioDeviceManager(), audio, null);
            Assert.Equal(2.5, vm.DurationSeconds, 3);
        }

        [Fact]
        public void UserSampleCount_WithUserSamples_CountsCorrectly()
        {
            var schema = TestData.LoadTD27Schema();
            var audio = CreateModuleAudio(schema);
            var vm = new InstrumentAudioExplorerViewModel(new FakeAudioDeviceManager(), audio, null);
            var expected = audio.Captures.Count(c => !c.Instrument.Group.Preset);
            // ViewModel computes UserSampleCount as first non-preset group's Audio.Count
            // If we have exactly one user sample, it should be 1
            // More generally, verify it counts correctly via the groups
            var userGroup = vm.Groups.FirstOrDefault(g => !g.Group.Preset);
            var expectedFromGroups = userGroup?.Audio.Count ?? 0;
            Assert.Equal(expectedFromGroups, vm.UserSampleCount);
        }

        [Fact]
        public void UserSampleCount_NoUserSamples_IsZero()
        {
            var schema = TestData.LoadTD27Schema();
            // Create audio with only preset instruments, no user samples
            var format = new AudioFormat(44100, 1, 16);
            var captures = new List<InstrumentAudio>();
            var presetGroup = schema.InstrumentGroups.First(g => g.Preset);
            captures.Add(new InstrumentAudio(presetGroup.Instruments[0], new byte[] { 1 }));
            var audio = new ModuleAudio(schema, format, TimeSpan.FromSeconds(1), captures);
            var vm = new InstrumentAudioExplorerViewModel(new FakeAudioDeviceManager(), audio, null);
            Assert.Equal(0, vm.UserSampleCount);
        }

        [Fact]
        public void SelectedAudio_SetAndGet_RaisesPropertyChanged()
        {
            var audio = CreateModuleAudio();
            var vm = new InstrumentAudioExplorerViewModel(new FakeAudioDeviceManager(), audio, null);
            var handler = new List<string>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => handler.Add(e.PropertyName!);
            var toSelect = vm.Groups[0].Audio[0];
            vm.SelectedAudio = toSelect;
            Assert.Same(toSelect, vm.SelectedAudio);
            Assert.Contains(nameof(InstrumentAudioExplorerViewModel.SelectedAudio), handler);
        }

        [Fact]
        public void SelectedOutputDevice_SetAndGet_RaisesPropertyChanged()
        {
            var out1 = new FakeAudioOutput("Out1");
            var out2 = new FakeAudioOutput("Out2");
            var manager = new FakeAudioDeviceManager(new List<IAudioInput>(), new List<IAudioOutput> { out1, out2 });
            var vm = new InstrumentAudioExplorerViewModel(manager, CreateModuleAudio(), null);
            var handler = new List<string>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => handler.Add(e.PropertyName!);
            vm.SelectedOutputDevice = out2;
            Assert.Same(out2, vm.SelectedOutputDevice);
            Assert.Contains(nameof(InstrumentAudioExplorerViewModel.SelectedOutputDevice), handler);
        }

        [Fact]
        public async Task PlayAudio_WithNullSelectedOutputDevice_DoesNotCallPlay()
        {
            var vm = new InstrumentAudioExplorerViewModel(new FakeAudioDeviceManager(), CreateModuleAudio(), null);
            vm.SelectedOutputDevice = null;
            vm.SelectedAudio = vm.Groups[0].Audio[0];
            // Should return without throwing
            await vm.PlayAudio();
        }

        [Fact]
        public async Task PlayAudio_WithNullSelectedAudio_DoesNotCallPlay()
        {
            var output = new TrackingAudioOutput("Track");
            var manager = new FakeAudioDeviceManager(new List<IAudioInput>(), new List<IAudioOutput> { output });
            var vm = new InstrumentAudioExplorerViewModel(manager, CreateModuleAudio(), null);
            vm.SelectedAudio = null;
            await vm.PlayAudio();
            Assert.Equal(0, output.PlayCallCount);
        }

        [Fact]
        public async Task PlayAudio_WithValidOutputAndAudio_CallsPlay()
        {
            var output = new TrackingAudioOutput("Track");
            var manager = new FakeAudioDeviceManager(new List<IAudioInput>(), new List<IAudioOutput> { output });
            var audio = CreateModuleAudio();
            var vm = new InstrumentAudioExplorerViewModel(manager, audio, null);
            var toPlay = vm.Groups[0].Audio[0];
            vm.SelectedAudio = toPlay;
            // SelectedOutputDevice already first (our tracking output)
            await vm.PlayAudio();
            Assert.Equal(1, output.PlayCallCount);
            Assert.Same(audio.Format, output.LastFormat);
            Assert.Same(toPlay.Audio, output.LastBytes);
        }

        [Fact]
        public async Task PlayAudio_WhenBothNull_DoesNotThrow()
        {
            var vm = new InstrumentAudioExplorerViewModel(new FakeAudioDeviceManager(), CreateModuleAudio(), null);
            vm.SelectedOutputDevice = null;
            vm.SelectedAudio = null;
            await vm.PlayAudio();
        }
    }
}
