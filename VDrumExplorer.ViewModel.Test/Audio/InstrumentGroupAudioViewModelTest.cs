// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Linq;
using VDrumExplorer.Model;
using VDrumExplorer.Model.Audio;
using VDrumExplorer.ViewModel.Audio;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Audio
{
    public class InstrumentGroupAudioViewModelTest
    {
        [Fact]
        public void Constructor_SetsGroupAndAudio()
        {
            var module = TestData.LoadTD27Module();
            var group = module.Schema.InstrumentGroups[0];
            var instrument = group.Instruments[0];
            var audio = new InstrumentAudio(instrument, new byte[] { 1, 2, 3 });
            var audioList = new[] { audio };

            var vm = new InstrumentGroupAudioViewModel(group, audioList);
            Assert.Same(group, vm.Group);
            Assert.Same(audioList, vm.Audio);
        }

        [Fact]
        public void Constructor_WithEmptyAudioList_SetsEmptyAudio()
        {
            var module = TestData.LoadTD27Module();
            var group = module.Schema.InstrumentGroups[0];
            var vm = new InstrumentGroupAudioViewModel(group, System.Array.Empty<InstrumentAudio>());
            Assert.Same(group, vm.Group);
            Assert.Empty(vm.Audio);
        }

        [Fact]
        public void FromGrouping_CreatesViewModelWithGroupingKeyAndElements()
        {
            var module = TestData.LoadTD27Module();
            // Find a group with at least two instruments, so we can create two audio entries.
            var group = module.Schema.InstrumentGroups.First(g => g.Instruments.Count >= 2);
            var instrument1 = group.Instruments[0];
            var instrument2 = group.Instruments[1];
            var audio1 = new InstrumentAudio(instrument1, new byte[] { 1 });
            var audio2 = new InstrumentAudio(instrument2, new byte[] { 2 });

            var grouping = new[] { audio1, audio2 }.GroupBy(a => a.Instrument.Group).Single();
            var vm = InstrumentGroupAudioViewModel.FromGrouping(grouping);
            Assert.Same(group, vm.Group);
            Assert.Equal(2, vm.Audio.Count);
            Assert.Same(audio1, vm.Audio[0]);
            Assert.Same(audio2, vm.Audio[1]);
        }

        [Fact]
        public void FromGrouping_WithSingleElement_CreatesViewModelWithOneAudio()
        {
            var module = TestData.LoadTD27Module();
            var group = module.Schema.InstrumentGroups[0];
            var instrument = group.Instruments[0];
            var audio = new InstrumentAudio(instrument, new byte[] { 1 });

            var grouping = new[] { audio }.GroupBy(a => a.Instrument.Group).Single();
            var vm = InstrumentGroupAudioViewModel.FromGrouping(grouping);
            Assert.Same(group, vm.Group);
            Assert.Single(vm.Audio);
            Assert.Same(audio, vm.Audio[0]);
        }

        [Fact]
        public void FromGrouping_WithMultipleGroups_CreatesSeparateViewModels()
        {
            var module = TestData.LoadTD27Module();
            var group0 = module.Schema.InstrumentGroups[0];
            var group1 = module.Schema.InstrumentGroups[1];
            var audio0 = new InstrumentAudio(group0.Instruments[0], new byte[] { 1 });
            var audio1 = new InstrumentAudio(group1.Instruments[0], new byte[] { 2 });

            var groupings = new[] { audio0, audio1 }.GroupBy(a => a.Instrument.Group).ToList();
            Assert.Equal(2, groupings.Count);

            var vm0 = InstrumentGroupAudioViewModel.FromGrouping(groupings[0]);
            var vm1 = InstrumentGroupAudioViewModel.FromGrouping(groupings[1]);
            Assert.Same(group0, vm0.Group);
            Assert.Same(group1, vm1.Group);
            Assert.Single(vm0.Audio);
            Assert.Single(vm1.Audio);
        }
    }
}
