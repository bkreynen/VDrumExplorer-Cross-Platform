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
    public class EditableBooleanDataFieldViewModelTest
    {
        private readonly Model.Module module = TestData.LoadTD27Module();

        private EditableBooleanDataFieldViewModel CreateViewModel()
        {
            var field = FieldFinder.FirstOf<BooleanDataField>(module.Data.LogicalRoot);
            return new EditableBooleanDataFieldViewModel(field);
        }

        [Fact]
        public void Value_Get_ReturnsUnderlyingFieldValue()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.Model.Value, vm.Value);
        }

        [Fact]
        public void Value_SetTrue_UpdatesUnderlyingField()
        {
            var vm = CreateViewModel();
            vm.Value = true;
            Assert.True(vm.Model.Value);
            Assert.True(vm.Value);
        }

        [Fact]
        public void Value_SetFalse_UpdatesUnderlyingField()
        {
            var vm = CreateViewModel();
            vm.Value = true;
            vm.Value = false;
            Assert.False(vm.Model.Value);
            Assert.False(vm.Value);
        }

        [Fact]
        public void Value_Set_FiresPropertyChanged()
        {
            var vm = CreateViewModel();
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            vm.Value = !vm.Value;
            Assert.Contains(nameof(EditableBooleanDataFieldViewModel.Value), changedProperties);
        }

        [Fact]
        public void Description_ReturnsFieldDescription()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.Model.SchemaField.Description, vm.Description);
        }
    }
}
