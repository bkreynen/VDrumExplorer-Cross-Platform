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
    public class ReadOnlyDataFieldViewModelTest
    {
        private readonly Model.Module module = TestData.LoadTD27Module();

        [Fact]
        public void FormattedText_ReturnsFieldFormattedText()
        {
            var field = FieldFinder.FirstOf<NumericDataField>(module.Data.LogicalRoot);
            var vm = new ReadOnlyDataFieldViewModel(field);
            Assert.Equal(field.FormattedText, vm.FormattedText);
        }

        [Fact]
        public void Description_ReturnsFieldDescription()
        {
            var field = FieldFinder.FirstOf<NumericDataField>(module.Data.LogicalRoot);
            var vm = new ReadOnlyDataFieldViewModel(field);
            Assert.Equal(field.SchemaField.Description, vm.Description);
        }

        [Fact]
        public void PropertyChanged_FromUnderlyingField_ForwardsFormattedText()
        {
            var field = FieldFinder.FirstOf<BooleanDataField>(module.Data.LogicalRoot);
            var vm = new ReadOnlyDataFieldViewModel(field);
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            // Change the underlying field value to trigger a property change
            field.Value = !field.Value;

            Assert.Contains(nameof(ReadOnlyDataFieldViewModel.FormattedText), changedProperties);
        }

        [Fact]
        public void PropertyChanged_NoSubscribers_DoesNotSubscribeToModel()
        {
            var field = FieldFinder.FirstOf<BooleanDataField>(module.Data.LogicalRoot);
            var vm = new ReadOnlyDataFieldViewModel(field);
            var changedProperties = new List<string?>();

            // Subscribe, change, unsubscribe
            void Handler(object? s, PropertyChangedEventArgs e) => changedProperties.Add(e.PropertyName);
            ((INotifyPropertyChanged)vm).PropertyChanged += Handler;
            field.Value = !field.Value;
            changedProperties.Clear();
            ((INotifyPropertyChanged)vm).PropertyChanged -= Handler;

            // After unsubscribing, model changes should not be forwarded
            field.Value = !field.Value;
            Assert.Empty(changedProperties);
        }
    }
}
