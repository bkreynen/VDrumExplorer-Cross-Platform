// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Collections.Generic;
using System.ComponentModel;
using VDrumExplorer.Model.Data.Fields;
using VDrumExplorer.ViewModel.Data;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Data
{
    public class EditableTempoDataFieldViewModelTest
    {
        private readonly Model.Module module = TestData.LoadTD27Module();

        private EditableTempoDataFieldViewModel CreateViewModel()
        {
            var field = FieldFinder.FirstOf<TempoDataField>(module.Data.LogicalRoot);
            return new EditableTempoDataFieldViewModel(field);
        }

        [Fact]
        public void TempoSync_Get_ReturnsModelValue()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.Model.TempoSync, vm.TempoSync);
        }

        [Fact]
        public void TempoSync_Set_UpdatesModel()
        {
            var vm = CreateViewModel();
            var original = vm.TempoSync;
            vm.TempoSync = !original;
            Assert.Equal(!original, vm.Model.TempoSync);
            Assert.Equal(!original, vm.TempoSync);
        }

        [Fact]
        public void NotTempoSync_IsOppositeOfTempoSync()
        {
            var vm = CreateViewModel();
            Assert.Equal(!vm.TempoSync, vm.NotTempoSync);
        }

        [Fact]
        public void TempoSync_Set_FiresPropertyChanged()
        {
            var vm = CreateViewModel();
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            vm.TempoSync = !vm.TempoSync;
            Assert.Contains(nameof(EditableTempoDataFieldViewModel.TempoSync), changedProperties);
            Assert.Contains(nameof(EditableTempoDataFieldViewModel.NotTempoSync), changedProperties);
            Assert.Contains(nameof(EditableTempoDataFieldViewModel.FormattedText), changedProperties);
        }

        [Fact]
        public void NumericValue_Get_ReturnsModelRawNumericValue()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.Model.RawNumericValue, vm.NumericValue);
        }

        [Fact]
        public void NumericValue_Set_UpdatesModel()
        {
            var vm = CreateViewModel();
            var newValue = vm.NumericValue == vm.MinNumericValue ? vm.MinNumericValue + 1 : vm.MinNumericValue;
            vm.NumericValue = newValue;
            Assert.Equal(newValue, vm.Model.RawNumericValue);
            Assert.Equal(newValue, vm.NumericValue);
        }

        [Fact]
        public void NumericValue_Set_FiresPropertyChanged()
        {
            var vm = CreateViewModel();
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            var newValue = vm.NumericValue == vm.MinNumericValue ? vm.MinNumericValue + 1 : vm.MinNumericValue;
            vm.NumericValue = newValue;
            Assert.Contains(nameof(EditableTempoDataFieldViewModel.NumericValue), changedProperties);
            Assert.Contains(nameof(EditableTempoDataFieldViewModel.FormattedText), changedProperties);
            Assert.Contains(nameof(EditableTempoDataFieldViewModel.NumericFormattedText), changedProperties);
        }

        [Fact]
        public void MusicalNote_Get_ReturnsModelValue()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.Model.MusicalNote, vm.MusicalNote);
        }

        [Fact]
        public void MusicalNote_Set_UpdatesModel()
        {
            var vm = CreateViewModel();
            // Ensure tempo sync is on so musical note is relevant
            vm.TempoSync = true;
            var newValue = vm.ValidMusicalNoteValues[0];
            if (vm.MusicalNote == newValue)
            {
                newValue = vm.ValidMusicalNoteValues[1];
            }
            vm.MusicalNote = newValue;
            Assert.Equal(newValue, vm.Model.MusicalNote);
            Assert.Equal(newValue, vm.MusicalNote);
        }

        [Fact]
        public void MusicalNote_Set_FiresPropertyChanged()
        {
            var vm = CreateViewModel();
            vm.TempoSync = true;
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            var newValue = vm.ValidMusicalNoteValues[0];
            if (vm.MusicalNote == newValue)
            {
                newValue = vm.ValidMusicalNoteValues[1];
            }
            vm.MusicalNote = newValue;
            Assert.Contains(nameof(EditableTempoDataFieldViewModel.MusicalNote), changedProperties);
            Assert.Contains(nameof(EditableTempoDataFieldViewModel.FormattedText), changedProperties);
        }

        [Fact]
        public void ValidMusicalNoteValues_IsNonEmpty()
        {
            var vm = CreateViewModel();
            Assert.NotEmpty(vm.ValidMusicalNoteValues);
        }

        [Fact]
        public void MinNumericValue_MatchesSchemaField()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.Model.SchemaField.NumericField.Min, vm.MinNumericValue);
        }

        [Fact]
        public void MaxNumericValue_MatchesSchemaField()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.Model.SchemaField.NumericField.Max, vm.MaxNumericValue);
        }

        [Fact]
        public void LargeNumericChange_IsAtLeastOne()
        {
            var vm = CreateViewModel();
            Assert.True(vm.LargeNumericChange >= 1);
        }

        [Fact]
        public void FormattedText_ReturnsModelFormattedText()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.Model.FormattedText, vm.FormattedText);
        }

        [Fact]
        public void NumericFormattedText_ReturnsModelNumericFormattedText()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.Model.NumericFormattedText, vm.NumericFormattedText);
        }

        [Fact]
        public void Description_ReturnsFieldDescription()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.Model.SchemaField.Description, vm.Description);
        }
    }
}
