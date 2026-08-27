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
    public class EditableEnumDataFieldViewModelTest
    {
        private readonly Model.Module module = TestData.LoadTD27Module();

        private EditableEnumDataFieldViewModel CreateViewModel()
        {
            var field = FieldFinder.FirstOf<EnumDataField>(module.Data.LogicalRoot);
            return new EditableEnumDataFieldViewModel(field);
        }

        [Fact]
        public void ValidValues_IsNonEmpty()
        {
            var vm = CreateViewModel();
            Assert.NotEmpty(vm.ValidValues);
        }

        [Fact]
        public void ValidValues_MatchesSchemaFieldValues()
        {
            var vm = CreateViewModel();
            Assert.Same(vm.Model.SchemaField.Values, vm.ValidValues);
        }

        [Fact]
        public void Value_Get_ReturnsUnderlyingFieldValue()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.Model.Value, vm.Value);
        }

        [Fact]
        public void Value_SetValidValue_UpdatesUnderlyingField()
        {
            var vm = CreateViewModel();
            var newValue = vm.ValidValues[vm.ValidValues.Count - 1];
            if (vm.Value == newValue)
            {
                newValue = vm.ValidValues[0];
            }
            vm.Value = newValue;
            Assert.Equal(newValue, vm.Model.Value);
            Assert.Equal(newValue, vm.Value);
        }

        [Fact]
        public void Value_Set_FiresPropertyChanged()
        {
            var vm = CreateViewModel();
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            var newValue = vm.ValidValues[vm.ValidValues.Count - 1];
            if (vm.Value == newValue)
            {
                newValue = vm.ValidValues[0];
            }
            vm.Value = newValue;
            Assert.Contains(nameof(EditableEnumDataFieldViewModel.Value), changedProperties);
        }

        [Fact]
        public void Description_ReturnsFieldDescription()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.Model.SchemaField.Description, vm.Description);
        }
    }
}
