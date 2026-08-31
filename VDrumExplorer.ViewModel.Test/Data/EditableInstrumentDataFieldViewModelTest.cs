// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using VDrumExplorer.Model;
using VDrumExplorer.Model.Data.Fields;
using VDrumExplorer.ViewModel.Data;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Data
{
    public class EditableInstrumentDataFieldViewModelTest
    {
        private readonly Model.Module module = TestData.LoadTD27Module();

        private EditableInstrumentDataFieldViewModel CreateViewModel()
        {
            var field = FieldFinder.FirstOf<InstrumentDataField>(module.Data.LogicalRoot);
            return new EditableInstrumentDataFieldViewModel(field);
        }

        [Fact]
        public void InstrumentGroups_IsNonEmpty()
        {
            var vm = CreateViewModel();
            Assert.NotEmpty(vm.InstrumentGroups);
        }

        [Fact]
        public void InstrumentGroups_MatchesSchema()
        {
            var vm = CreateViewModel();
            Assert.Same(vm.Model.Schema.InstrumentGroups, vm.InstrumentGroups);
        }

        [Fact]
        public void IsPreset_InitiallyTrue_ForPresetInstrument()
        {
            var vm = CreateViewModel();
            // The first instrument field should default to a preset instrument
            Assert.True(vm.IsPreset);
            Assert.False(vm.IsUserSample);
        }

        [Fact]
        public void Group_Get_ReturnsInstrumentGroup()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.Model.Instrument.Group, vm.Group);
        }

        [Fact]
        public void Group_Set_UpdatesInstrument()
        {
            var vm = CreateViewModel();
            var newGroup = vm.InstrumentGroups.First(g => g != vm.Group);
            vm.Group = newGroup;
            Assert.Equal(newGroup, vm.Model.Instrument.Group);
            Assert.Equal(newGroup, vm.Group);
        }

        [Fact]
        public void Group_Set_FiresPropertyChanged()
        {
            var vm = CreateViewModel();
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            var newGroup = vm.InstrumentGroups.First(g => g != vm.Group);
            vm.Group = newGroup;
            Assert.Contains(nameof(EditableInstrumentDataFieldViewModel.Group), changedProperties);
            Assert.Contains(nameof(EditableInstrumentDataFieldViewModel.Instrument), changedProperties);
        }

        [Fact]
        public void Instrument_Get_ReturnsModelInstrument()
        {
            var vm = CreateViewModel();
            Assert.Same(vm.Model.Instrument, vm.Instrument);
        }

        [Fact]
        public void Instrument_Set_UpdatesModel()
        {
            var vm = CreateViewModel();
            var newInstrument = vm.Group.Instruments.First(i => i != vm.Instrument);
            vm.Instrument = newInstrument;
            Assert.Same(newInstrument, vm.Model.Instrument);
            Assert.Same(newInstrument, vm.Instrument);
        }

        [Fact]
        public void Instrument_SetNull_DoesNotUpdateModel()
        {
            var vm = CreateViewModel();
            var original = vm.Model.Instrument;
            vm.Instrument = null!;
            Assert.Same(original, vm.Model.Instrument);
        }

        [Fact]
        public void Instrument_Set_FiresPropertyChanged()
        {
            var vm = CreateViewModel();
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            var newInstrument = vm.Group.Instruments.First(i => i != vm.Instrument);
            vm.Instrument = newInstrument;
            Assert.Contains(nameof(EditableInstrumentDataFieldViewModel.Instrument), changedProperties);
        }

        [Fact]
        public void UserSample_Get_ReturnsNullForPreset()
        {
            var vm = CreateViewModel();
            Assert.True(vm.IsPreset);
            Assert.Null(vm.UserSample);
        }

        [Fact]
        public void Group_SetToUserSample_MakesIsUserSampleTrue()
        {
            var vm = CreateViewModel();
            // TD-27 schema must have user samples; ensure premise explicitly fails if not.
            Assert.True(vm.Model.Schema.UserSampleInstruments.Count > 0, "TD27 must have user samples for this test");
            var userGroup = vm.InstrumentGroups.FirstOrDefault(g => !g.Preset);
            Assert.NotNull(userGroup);
            // Initially preset instrument
            Assert.True(vm.IsPreset);
            Assert.False(vm.IsUserSample);

            vm.Group = userGroup!;

            Assert.False(vm.IsPreset);
            Assert.True(vm.IsUserSample);
            Assert.NotNull(vm.UserSample);
            Assert.Equal(1, vm.UserSample);
            // Group property reflects new group
            Assert.Same(userGroup, vm.Group);
            // Instrument should be first user sample instrument
            Assert.Same(vm.Model.Schema.UserSampleInstruments[0], vm.Instrument);
        }

        [Fact]
        public void IsUserSample_AfterSwitchToPreset_ReturnsFalse()
        {
            var vm = CreateViewModel();
            Assert.True(vm.Model.Schema.UserSampleInstruments.Count > 0, "TD27 must have user samples");
            var userGroup = vm.InstrumentGroups.First(g => !g.Preset);
            var presetGroup = vm.InstrumentGroups.First(g => g.Preset);
            vm.Group = userGroup;
            Assert.True(vm.IsUserSample);
            vm.Group = presetGroup;
            Assert.True(vm.IsPreset);
            Assert.False(vm.IsUserSample);
            Assert.Null(vm.UserSample);
        }

        [Fact]
        public void InstrumentGroups_ContainsUserSampleGroupWhenAvailable()
        {
            var vm = CreateViewModel();
            if (vm.Model.Schema.UserSamples > 0)
            {
                Assert.Contains(vm.InstrumentGroups, g => !g.Preset);
                Assert.Equal("User samples", vm.InstrumentGroups.First(g => !g.Preset).Description);
            }
            else
            {
                Assert.DoesNotContain(vm.InstrumentGroups, g => !g.Preset);
            }
        }

        [Fact]
        public void Description_ReturnsFieldDescription()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.Model.SchemaField.Description, vm.Description);
        }
    }
}
