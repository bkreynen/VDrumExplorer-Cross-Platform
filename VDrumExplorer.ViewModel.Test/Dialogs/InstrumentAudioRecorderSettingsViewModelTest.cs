// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Linq;
using VDrumExplorer.Model;
using VDrumExplorer.Model.Audio;
using VDrumExplorer.ViewModel.Dialogs;
using VDrumExplorer.ViewModel.Test.Fakes;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Dialogs
{
    public class InstrumentAudioRecorderSettingsViewModelTest
    {
        private readonly ModuleSchema schema = TestData.LoadTD27Schema();

        private InstrumentAudioRecorderSettingsViewModel CreateViewModel() =>
            CreateViewModel(new FakeAudioDeviceManager());

        private InstrumentAudioRecorderSettingsViewModel CreateViewModel(FakeAudioDeviceManager deviceManager) =>
            new InstrumentAudioRecorderSettingsViewModel(new FakeViewServices(), deviceManager, schema, "Test MIDI");

        [Fact]
        public void Constructor_SetsInstrumentGroups_WithAllPlusPresetGroups()
        {
            var vm = CreateViewModel();
            var presetGroupDescriptions = schema.InstrumentGroups
                .Where(ig => ig.Preset)
                .Select(ig => ig.Description)
                .ToList();
            // InstrumentGroups should contain "(All)" plus all preset group descriptions
            Assert.Equal(presetGroupDescriptions.Count + 1, vm.InstrumentGroups.Count);
            Assert.Equal("(All)", vm.InstrumentGroups[0]);
            foreach (var desc in presetGroupDescriptions)
            {
                Assert.Contains(desc, vm.InstrumentGroups);
            }
        }

        [Fact]
        public void Constructor_SelectedInstrumentGroup_DefaultsToAll()
        {
            var vm = CreateViewModel();
            Assert.Equal("(All)", vm.SelectedInstrumentGroup);
        }

        [Fact]
        public void RecordingTime_DefaultValue()
        {
            var vm = CreateViewModel();
            Assert.Equal(2.5m, vm.RecordingTime);
        }

        [Fact]
        public void RecordingTime_SetValidValue_UpdatesProperty()
        {
            var vm = CreateViewModel();
            vm.RecordingTime = 5.0m;
            Assert.Equal(5.0m, vm.RecordingTime);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.09)]
        public void RecordingTime_BelowMinimum_ThrowsArgumentOutOfRangeException(double value)
        {
            var vm = CreateViewModel();
            Assert.Throws<ArgumentOutOfRangeException>(() => vm.RecordingTime = (decimal)value);
        }

        [Fact]
        public void RecordToPlayDelay_DefaultValue()
        {
            var vm = CreateViewModel();
            Assert.Equal(20, vm.RecordToPlayDelay);
        }

        [Fact]
        public void RecordToPlayDelay_SetValidValue_UpdatesProperty()
        {
            var vm = CreateViewModel();
            vm.RecordToPlayDelay = 50;
            Assert.Equal(50, vm.RecordToPlayDelay);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void RecordToPlayDelay_NegativeValue_ThrowsArgumentOutOfRangeException(int value)
        {
            var vm = CreateViewModel();
            Assert.Throws<ArgumentOutOfRangeException>(() => vm.RecordToPlayDelay = value);
        }

        [Fact]
        public void RecordToPlayDelay_Zero_IsValid()
        {
            var vm = CreateViewModel();
            vm.RecordToPlayDelay = 0;
            Assert.Equal(0, vm.RecordToPlayDelay);
        }

        [Fact]
        public void KitNumber_DefaultValue_IsSchemaKits()
        {
            var vm = CreateViewModel();
            Assert.Equal(schema.Kits, vm.KitNumber);
        }

        [Fact]
        public void KitNumber_SetValidValue_UpdatesProperty()
        {
            var vm = CreateViewModel();
            vm.KitNumber = 1;
            Assert.Equal(1, vm.KitNumber);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void KitNumber_TooLow_ThrowsArgumentOutOfRangeException(int value)
        {
            var vm = CreateViewModel();
            Assert.Throws<ArgumentOutOfRangeException>(() => vm.KitNumber = value);
        }

        [Fact]
        public void KitNumber_AboveMax_ThrowsArgumentOutOfRangeException()
        {
            var vm = CreateViewModel();
            Assert.Throws<ArgumentOutOfRangeException>(() => vm.KitNumber = schema.Kits + 1);
        }

        [Fact]
        public void KitNumber_AtMax_IsValid()
        {
            var vm = CreateViewModel();
            vm.KitNumber = schema.Kits;
            Assert.Equal(schema.Kits, vm.KitNumber);
        }

        [Fact]
        public void UserSamples_DefaultValue_IsZero()
        {
            var vm = CreateViewModel();
            Assert.Equal(0, vm.UserSamples);
        }

        [Fact]
        public void UserSamples_SetValidValue_UpdatesProperty()
        {
            var vm = CreateViewModel();
            Assert.True(schema.UserSampleInstruments.Count > 0, "TD27 must have user samples — test premise requires at least one user sample instrument");
            vm.UserSamples = 1;
            Assert.Equal(1, vm.UserSamples);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void UserSamples_NegativeValue_ThrowsArgumentException(int value)
        {
            var vm = CreateViewModel();
            Assert.Throws<ArgumentException>(() => vm.UserSamples = value);
        }

        [Fact]
        public void UserSamples_AboveMax_ThrowsArgumentException()
        {
            var vm = CreateViewModel();
            Assert.Throws<ArgumentException>(() => vm.UserSamples = schema.UserSampleInstruments.Count + 1);
        }

        [Fact]
        public void UserSamples_Zero_IsValid()
        {
            var vm = CreateViewModel();
            vm.UserSamples = 0;
            Assert.Equal(0, vm.UserSamples);
        }

        [Fact]
        public void SelectedMidiChannel_DefaultValue()
        {
            var vm = CreateViewModel();
            Assert.Equal(10, vm.SelectedMidiChannel);
        }

        [Fact]
        public void SelectedMidiChannel_SetValidValue_UpdatesProperty()
        {
            var vm = CreateViewModel();
            vm.SelectedMidiChannel = 5;
            Assert.Equal(5, vm.SelectedMidiChannel);
        }

        [Fact]
        public void MidiChannels_Contains1Through16()
        {
            var vm = CreateViewModel();
            Assert.Equal(16, vm.MidiChannels.Count);
            for (int i = 0; i < 16; i++)
            {
                Assert.Equal(i + 1, vm.MidiChannels[i]);
            }
        }

        [Fact]
        public void Attack_DefaultValue()
        {
            var vm = CreateViewModel();
            Assert.Equal(80, vm.Attack);
        }

        [Fact]
        public void Attack_SetValidValue_UpdatesProperty()
        {
            var vm = CreateViewModel();
            vm.Attack = 100;
            Assert.Equal(100, vm.Attack);
        }

        [Fact]
        public void MinAttack_Is1()
        {
            var vm = CreateViewModel();
            Assert.Equal(1, vm.MinAttack);
        }

        [Fact]
        public void MaxAttack_Is127()
        {
            var vm = CreateViewModel();
            Assert.Equal(127, vm.MaxAttack);
        }

        [Fact]
        public void SelectOutputFileCommand_NotNull()
        {
            var vm = CreateViewModel();
            Assert.NotNull(vm.SelectOutputFileCommand);
        }

        [Fact]
        public void OutputFile_DefaultIsNull()
        {
            var vm = CreateViewModel();
            Assert.Null(vm.OutputFile);
        }

        [Fact]
        public void OutputFile_SetValue_UpdatesProperty()
        {
            var vm = CreateViewModel();
            vm.OutputFile = "/tmp/test.wav";
            Assert.Equal("/tmp/test.wav", vm.OutputFile);
        }

        [Fact]
        public void SelectedInstrumentGroup_SetValue_UpdatesProperty()
        {
            var vm = CreateViewModel();
            var firstGroup = vm.InstrumentGroups[1];
            vm.SelectedInstrumentGroup = firstGroup;
            Assert.Equal(firstGroup, vm.SelectedInstrumentGroup);
        }

        [Fact]
        public void InputDevices_EmptyDeviceManager_ReturnsEmptyList()
        {
            var vm = CreateViewModel(new FakeAudioDeviceManager());
            Assert.Empty(vm.InputDevices);
        }

        [Fact]
        public void InputDevices_WithDevices_ReturnsProvidedDevices()
        {
            var inputs = new[] { new FakeAudioInput("Device1"), new FakeAudioInput("Device2") };
            var deviceManager = new FakeAudioDeviceManager(inputs, new System.Collections.Generic.List<IAudioOutput>());
            var vm = CreateViewModel(deviceManager);
            Assert.Equal(2, vm.InputDevices.Count);
            Assert.Equal("Device1", vm.InputDevices[0].Name);
            Assert.Equal("Device2", vm.InputDevices[1].Name);
        }

        [Fact]
        public void SelectedInputDevice_WithMatchingDevice_SelectsMatchingDevice()
        {
            var midiName = "Test MIDI";
            var matchingInput = new FakeAudioInput($"MASTER ({midiName})");
            var inputs = new[] { matchingInput, new FakeAudioInput("Other") };
            var deviceManager = new FakeAudioDeviceManager(inputs, new System.Collections.Generic.List<IAudioOutput>());
            var vm = new InstrumentAudioRecorderSettingsViewModel(new FakeViewServices(), deviceManager, schema, midiName);
            Assert.Same(matchingInput, vm.SelectedInputDevice);
        }

        [Fact]
        public void SelectedInputDevice_NoMatchingDevice_ReturnsNull()
        {
            var inputs = new[] { new FakeAudioInput("Other1"), new FakeAudioInput("Other2") };
            var deviceManager = new FakeAudioDeviceManager(inputs, new System.Collections.Generic.List<IAudioOutput>());
            var vm = CreateViewModel(deviceManager);
            Assert.Null(vm.SelectedInputDevice);
        }

        [Fact]
        public void SelectedInputDevice_SetValue_UpdatesProperty()
        {
            var vm = CreateViewModel();
            var input = new FakeAudioInput("Test");
            vm.SelectedInputDevice = input;
            Assert.Same(input, vm.SelectedInputDevice);
        }
    }
}
