// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Collections.Generic;
using System.ComponentModel;
using VDrumExplorer.Model.Data.Fields;

namespace VDrumExplorer.ViewModel.Data
{
    public class EditableEnumDataFieldViewModel : DataFieldViewModel<EnumDataField>
    {
        public EditableEnumDataFieldViewModel(EnumDataField model) : base(model)
        {
        }

        protected override void OnPropertyModelChanged(object sender, PropertyChangedEventArgs e) =>
            RaisePropertyChanged(nameof(Value));

        public IReadOnlyList<string> ValidValues => Model.SchemaField.Values;

        /// <summary>
        /// Screen-reader help text (docs/accessibility.md §3): the schema description
        /// followed by the allowed enum values (the full list is fine — screen readers
        /// let users re-read it).
        /// </summary>
        public string HelpText => $"{Description}: {string.Join(", ", ValidValues)}";

        public string Value
        {
            get => Model.Value;
            set => Model.Value = value;
        }
    }
}
