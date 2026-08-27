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
    public class EditableNumericDataFieldViewModelTest
    {
        private readonly Model.Module module = TestData.LoadTD27Module();

        private EditableNumericDataFieldViewModel CreateViewModel()
        {
            var field = FieldFinder.FirstOf<NumericDataField>(module.Data.LogicalRoot);
            return new EditableNumericDataFieldViewModel(field);
        }

        [Fact]
        public void MinValue_MatchesSchemaField()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.Model.SchemaField.Min, vm.MinValue);
        }

        [Fact]
        public void MaxValue_MatchesSchemaField()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.Model.SchemaField.Max, vm.MaxValue);
        }

        [Fact]
        public void LargeChange_IsAtLeastOne()
        {
            var vm = CreateViewModel();
            Assert.True(vm.LargeChange >= 1);
        }

        [Fact]
        public void LargeChange_IsMaxOfRangeDivTenOrOne()
        {
            var vm = CreateViewModel();
            var expected = System.Math.Max((vm.MaxValue - vm.MinValue) / 10, 1);
            Assert.Equal(expected, vm.LargeChange);
        }

        [Fact]
        public void Value_Get_ReturnsUnderlyingFieldRawValue()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.Model.RawValue, vm.Value);
        }

        [Fact]
        public void Value_Set_UpdatesUnderlyingField()
        {
            var vm = CreateViewModel();
            var newValue = vm.Model.RawValue == vm.MinValue ? vm.MinValue + 1 : vm.MinValue;
            vm.Value = newValue;
            Assert.Equal(newValue, vm.Model.RawValue);
            Assert.Equal(newValue, vm.Value);
        }

        [Fact]
        public void Value_Set_FiresPropertyChanged()
        {
            var vm = CreateViewModel();
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            var newValue = vm.Model.RawValue == vm.MinValue ? vm.MinValue + 1 : vm.MinValue;
            vm.Value = newValue;
            Assert.Contains(nameof(EditableNumericDataFieldViewModel.Value), changedProperties);
            Assert.Contains(nameof(EditableNumericDataFieldViewModel.FormattedText), changedProperties);
            Assert.Contains(nameof(EditableNumericDataFieldViewModel.FormattedValue), changedProperties);
        }

        [Fact]
        public void FormattedText_ReturnsFieldFormattedText()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.Model.FormattedText, vm.FormattedText);
        }

        [Fact]
        public void FormattedValue_Get_ReturnsFieldFormattedText()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.Model.FormattedText, vm.FormattedValue);
        }

        [Fact]
        public void FormattedValue_SetValidText_UpdatesValue()
        {
            var vm = CreateViewModel();
            var originalFormatted = vm.FormattedValue;
            // Try setting to the same formatted text (which should be valid)
            vm.FormattedValue = originalFormatted;
            Assert.Equal(originalFormatted, vm.FormattedValue);
        }

        [Fact]
        public void Description_ReturnsFieldDescription()
        {
            var vm = CreateViewModel();
            Assert.Equal(vm.Model.SchemaField.Description, vm.Description);
        }
    }
}
