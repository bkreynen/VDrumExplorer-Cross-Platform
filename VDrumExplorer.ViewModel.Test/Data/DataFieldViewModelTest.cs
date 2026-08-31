// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using VDrumExplorer.Model.Data.Fields;
using VDrumExplorer.ViewModel.Data;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Data
{
    public class DataFieldViewModelTest
    {
        private readonly Model.Module module = TestData.LoadTD27Module();

        [Fact]
        public void CreateViewModel_BooleanField_NotReadOnly_ReturnsEditableBoolean()
        {
            var field = FieldFinder.FirstOf<BooleanDataField>(module.Data.LogicalRoot);
            var vm = DataFieldViewModel.CreateViewModel(field, false);
            Assert.IsType<EditableBooleanDataFieldViewModel>(vm);
        }

        [Fact]
        public void CreateViewModel_BooleanField_ReadOnly_ReturnsReadOnly()
        {
            var field = FieldFinder.FirstOf<BooleanDataField>(module.Data.LogicalRoot);
            var vm = DataFieldViewModel.CreateViewModel(field, true);
            Assert.IsType<ReadOnlyDataFieldViewModel>(vm);
        }

        [Fact]
        public void CreateViewModel_EnumField_NotReadOnly_ReturnsEditableEnum()
        {
            var field = FieldFinder.FirstOf<EnumDataField>(module.Data.LogicalRoot);
            var vm = DataFieldViewModel.CreateViewModel(field, false);
            Assert.IsType<EditableEnumDataFieldViewModel>(vm);
        }

        [Fact]
        public void CreateViewModel_EnumField_ReadOnly_ReturnsReadOnly()
        {
            var field = FieldFinder.FirstOf<EnumDataField>(module.Data.LogicalRoot);
            var vm = DataFieldViewModel.CreateViewModel(field, true);
            Assert.IsType<ReadOnlyDataFieldViewModel>(vm);
        }

        [Fact]
        public void CreateViewModel_NumericField_NotReadOnly_ReturnsEditableNumeric()
        {
            var field = FieldFinder.FirstOf<NumericDataField>(module.Data.LogicalRoot);
            var vm = DataFieldViewModel.CreateViewModel(field, false);
            Assert.IsType<EditableNumericDataFieldViewModel>(vm);
        }

        [Fact]
        public void CreateViewModel_NumericField_ReadOnly_ReturnsReadOnly()
        {
            var field = FieldFinder.FirstOf<NumericDataField>(module.Data.LogicalRoot);
            var vm = DataFieldViewModel.CreateViewModel(field, true);
            Assert.IsType<ReadOnlyDataFieldViewModel>(vm);
        }

        [Fact]
        public void CreateViewModel_StringField_NotReadOnly_ReturnsEditableString()
        {
            var field = FieldFinder.FirstOf<StringDataField>(module.Data.LogicalRoot);
            var vm = DataFieldViewModel.CreateViewModel(field, false);
            Assert.IsType<EditableStringDataFieldViewModel>(vm);
        }

        [Fact]
        public void CreateViewModel_StringField_ReadOnly_ReturnsReadOnly()
        {
            var field = FieldFinder.FirstOf<StringDataField>(module.Data.LogicalRoot);
            var vm = DataFieldViewModel.CreateViewModel(field, true);
            Assert.IsType<ReadOnlyDataFieldViewModel>(vm);
        }

        [Fact]
        public void CreateViewModel_InstrumentField_NotReadOnly_ReturnsEditableInstrument()
        {
            var field = FieldFinder.FirstOf<InstrumentDataField>(module.Data.LogicalRoot);
            var vm = DataFieldViewModel.CreateViewModel(field, false);
            Assert.IsType<EditableInstrumentDataFieldViewModel>(vm);
        }

        [Fact]
        public void CreateViewModel_InstrumentField_ReadOnly_ReturnsReadOnly()
        {
            var field = FieldFinder.FirstOf<InstrumentDataField>(module.Data.LogicalRoot);
            var vm = DataFieldViewModel.CreateViewModel(field, true);
            Assert.IsType<ReadOnlyDataFieldViewModel>(vm);
        }

        [Fact]
        public void CreateViewModel_TempoField_NotReadOnly_ReturnsEditableTempo()
        {
            var field = FieldFinder.FirstOf<TempoDataField>(module.Data.LogicalRoot);
            var vm = DataFieldViewModel.CreateViewModel(field, false);
            Assert.IsType<EditableTempoDataFieldViewModel>(vm);
        }

        [Fact]
        public void CreateViewModel_TempoField_ReadOnly_ReturnsReadOnly()
        {
            var field = FieldFinder.FirstOf<TempoDataField>(module.Data.LogicalRoot);
            var vm = DataFieldViewModel.CreateViewModel(field, true);
            Assert.IsType<ReadOnlyDataFieldViewModel>(vm);
        }

        [Fact]
        public void CreateViewModel_OverlayField_NotReadOnly_ThrowsArgumentException()
        {
            var field = FieldFinder.FirstOf<OverlayDataField>(module.Data.LogicalRoot);
            Assert.Throws<ArgumentException>(() => DataFieldViewModel.CreateViewModel(field, false));
        }

        [Fact]
        public void CreateViewModel_OverlayField_ReadOnly_ThrowsArgumentException()
        {
            var field = FieldFinder.FirstOf<OverlayDataField>(module.Data.LogicalRoot);
            Assert.Throws<ArgumentException>(() => DataFieldViewModel.CreateViewModel(field, true));
        }
    }
}
