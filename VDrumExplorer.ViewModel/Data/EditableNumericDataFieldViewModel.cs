// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.ComponentModel;
using VDrumExplorer.Model.Data.Fields;

namespace VDrumExplorer.ViewModel.Data
{
    public class EditableNumericDataFieldViewModel : DataFieldViewModel<NumericDataField>
    {
        public EditableNumericDataFieldViewModel(NumericDataField model) : base(model)
        {
        }

        protected override void OnPropertyModelChanged(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(Value));
            RaisePropertyChanged(nameof(FormattedText));
            RaisePropertyChanged(nameof(FormattedValue));
        }

        public int MinValue => Model.SchemaField.Min;
        public int MaxValue => Model.SchemaField.Max;
        public int LargeChange => Math.Max((MaxValue - MinValue) / 10, 1);

        /// <summary>
        /// Screen-reader help text (docs/accessibility.md §3): the schema description plus
        /// the min/max range, each bound formatted with the field's own unit/suffix.
        /// </summary>
        public string HelpText =>
            $"{Description}, {Model.SchemaField.FormatRawValue(Model.SchemaField.Min)} to {Model.SchemaField.FormatRawValue(Model.SchemaField.Max)}";

        public int Value
        {
            get => Model.RawValue;
            set => Model.RawValue = value;
        }

        public string FormattedText => Model.FormattedText;

        public string FormattedValue
        {
            get => Model.FormattedText;
            set
            {
                if (Model.TrySetFormattedText(value))
                {
                    RaisePropertyChanged(nameof(Value));
                    RaisePropertyChanged(nameof(FormattedText));
                    RaisePropertyChanged(nameof(FormattedValue));
                }
            }
        }
    }
}
