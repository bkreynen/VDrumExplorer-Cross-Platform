// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.ComponentModel;
using VDrumExplorer.Model.Data.Fields;

namespace VDrumExplorer.ViewModel.Data
{
    public class EditableStringDataFieldViewModel : DataFieldViewModel<StringDataField>
    {
        public EditableStringDataFieldViewModel(StringDataField model) : base(model)
        {
        }

        protected override void OnPropertyModelChanged(object sender, PropertyChangedEventArgs e) =>
            RaisePropertyChanged(nameof(Text));

        public int MaxLength => Model.SchemaField.Length;

        /// <summary>
        /// Screen-reader help text (docs/accessibility.md §3): the schema description plus
        /// the maximum string length.
        /// </summary>
        public string HelpText =>
            MaxLength == 1
                ? $"{Description}, up to {MaxLength} character"
                : $"{Description}, up to {MaxLength} characters";

        // TODO: This is horrible, as it's really a GUI concern. We need to be able to guess a "reasonable average character width".
        public int MinWidth => MaxLength * 8;

        public string Text
        {
            get => Model.Text;
            set => Model.Text = value;
        }
    }
}
