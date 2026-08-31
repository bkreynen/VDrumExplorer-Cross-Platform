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
    public class EditableStringDataFieldViewModelTest
    {
        private readonly Model.Module module = TestData.LoadTD27Module();

        private EditableStringDataFieldViewModel CreateViewModel()
        {
            var field = FieldFinder.FirstOf<StringDataField>(module.Data.LogicalRoot);
            return new EditableStringDataFieldViewModel(field);
        }

        [Fact]
        public void Text_Get_ReturnsUnderlyingFieldText()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.Model.Text, vm.Text);
        }

        [Fact]
        public void Text_Set_UpdatesUnderlyingField()
        {
            var vm = CreateViewModel();
            vm.Text = "Test";
            Assert.Equal("Test", vm.Model.Text);
            Assert.Equal("Test", vm.Text);
        }

        [Fact]
        public void Text_Set_FiresPropertyChanged()
        {
            var vm = CreateViewModel();
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            vm.Text = "Hello";
            Assert.Contains(nameof(EditableStringDataFieldViewModel.Text), changedProperties);
        }

        [Fact]
        public void MaxLength_MatchesSchemaField()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.Model.SchemaField.Length, vm.MaxLength);
        }

        [Fact]
        public void MinWidth_IsMaxLengthTimesEight()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.MaxLength * 8, vm.MinWidth);
        }

        [Fact]
        public void Description_ReturnsFieldDescription()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.Model.SchemaField.Description, vm.Description);
        }
    }
}
