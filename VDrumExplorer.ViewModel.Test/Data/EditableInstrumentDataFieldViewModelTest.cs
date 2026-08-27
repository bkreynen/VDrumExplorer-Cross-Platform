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
        public void Description_ReturnsFieldDescription()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.Model.SchemaField.Description, vm.Description);
        }
    }
}
