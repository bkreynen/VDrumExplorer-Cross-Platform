// Copyright 2024 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Linq;
using VDrumExplorer.Model.Data.Fields;
using VDrumExplorer.Model.Data.Logical;
using VDrumExplorer.Model.Schema.Logical;

namespace VDrumExplorer.Model.Test.Data.Logical
{
    public class DataFieldFormattableStringTest
    {
        private Module module = null!;
        private DataFieldFormattableString formattable = null!;
        private FieldFormattableString schemaFormattable = null!;

        [SetUp]
        public void SetUp()
        {
            module = TestData.LoadTD27();
            // Find a DataFieldFormattableString with non-empty format paths.
            // The DataTreeNode.Format is a DataFieldFormattableString.
            // We look for a tree node whose schema Format has non-empty format paths.
            var nodeWithFormatPaths = module.Data.LogicalRoot.SchemaNode.DescendantsAndSelf()
                .First(n => n.Format.FormatPaths.Count > 0);
            schemaFormattable = nodeWithFormatPaths.Format;
            formattable = new DataFieldFormattableString(module.Data, schemaFormattable);
        }

        [Test]
        public void Text_ReturnsNonEmptyString()
        {
            Assert.IsFalse(string.IsNullOrEmpty(formattable.Text));
        }

        [Test]
        public void Text_ReturnsFormattedString()
        {
            // The format string should contain the format placeholders, but the result should not
            // contain raw {0} or {1} placeholders (they should be replaced with field values).
            ClassicAssert.IsFalse(formattable.Text.Contains("{0}"));
            ClassicAssert.IsFalse(formattable.Text.Contains("{1}"));
        }

        [Test]
        public void Text_WithEmptyFormatPaths_ReturnsFormatString()
        {
            // Find a node with no format paths (just a literal format string).
            var nodeWithoutFormatPaths = module.Data.LogicalRoot.SchemaNode.DescendantsAndSelf()
                .First(n => n.Format.FormatPaths.Count == 0);
            var schemaFormat = nodeWithoutFormatPaths.Format;
            var dataFormat = new DataFieldFormattableString(module.Data, schemaFormat);

            // When there are no format paths, Text should just be the format string.
            Assert.AreEqual(schemaFormat.FormatString, dataFormat.Text);
        }

        [Test]
        public void PropertyChanged_FiresWhenUnderlyingFieldChanges()
        {
            // Find a node whose first format path resolves to a field we can change.
            // Format paths typically resolve to StringDataField (e.g. kit names) or
            // NumericDataField; we handle both so the test is robust to schema changes.
            var nodeWithChangeableFormat = module.Data.LogicalRoot.SchemaNode.DescendantsAndSelf()
                .First(n => n.Format.FormatPaths.Count > 0 &&
                            module.Data.GetDataField(n.Format.Container.ResolveField(n.Format.FormatPaths[0])) is StringDataField or NumericDataField);
            var changeableSchemaFormattable = nodeWithChangeableFormat.Format;
            var changeableFormattable = new DataFieldFormattableString(module.Data, changeableSchemaFormattable);

            // The DataFieldFormattableString subscribes to field changes when a handler is attached.
            var recorder = new NotifyChangeRecorder(changeableFormattable);

            // Find the underlying data field and change it.
            var formatPath = changeableSchemaFormattable.FormatPaths[0];
            var field = changeableSchemaFormattable.Container.ResolveField(formatPath);
            var dataField = module.Data.GetDataField(field);

            // Change the field value to trigger PropertyChanged.
            switch (dataField)
            {
                case StringDataField stringField:
                    // Use TrySetFormattedText to safely change the value, toggling between two short strings.
                    var newText = stringField.FormattedText == "A" ? "B" : "A";
                    if (!stringField.TrySetFormattedText(newText))
                    {
                        // If single-char strings aren't valid, just append a character or reset.
                        stringField.Reset();
                    }
                    break;
                case NumericDataField numericField:
                    var newValue = numericField.RawValue == numericField.SchemaField.Min
                        ? numericField.SchemaField.Min + 1
                        : numericField.SchemaField.Min;
                    numericField.RawValue = newValue;
                    break;
            }

            // Verify that at least one PropertyChanged event was fired for "Text".
            CollectionAssert.Contains(recorder.ChangedProperties, nameof(changeableFormattable.Text));
        }

        [Test]
        public void PropertyChanged_DoesNotFireBeforeSubscription()
        {
            // Find a node whose first format path resolves to a changeable field so we can exercise forwarding.
            var nodeWithChangeableFormat = module.Data.LogicalRoot.SchemaNode.DescendantsAndSelf()
                .First(n => n.Format.FormatPaths.Count > 0 &&
                            module.Data.GetDataField(n.Format.Container.ResolveField(n.Format.FormatPaths[0])) is StringDataField or NumericDataField);
            var changeableSchemaFormattable = nodeWithChangeableFormat.Format;
            var newFormattable = new DataFieldFormattableString(module.Data, changeableSchemaFormattable);

            var formatPath = changeableSchemaFormattable.FormatPaths[0];
            var field = changeableSchemaFormattable.Container.ResolveField(formatPath);
            var dataField = module.Data.GetDataField(field);

            void ChangeField(VDrumExplorer.Model.Data.Fields.IDataField f)
            {
                switch (f)
                {
                    case StringDataField stringField:
                        var newText = stringField.FormattedText == "A" ? "B" : "A";
                        if (!stringField.TrySetFormattedText(newText))
                        {
                            stringField.Reset();
                        }
                        break;
                    case NumericDataField numericField:
                        var newValue = numericField.RawValue == numericField.SchemaField.Min
                            ? numericField.SchemaField.Min + 1
                            : numericField.SchemaField.Min;
                        numericField.RawValue = newValue;
                        break;
                }
            }

            // Change underlying field BEFORE subscribing — must not be observed.
            ChangeField(dataField);

            var recorder = new NotifyChangeRecorder(newFormattable);
            Assert.IsEmpty(recorder.ChangedProperties);

            // Change again AFTER subscribing — must now fire Text.
            ChangeField(dataField);
            CollectionAssert.Contains(recorder.ChangedProperties, nameof(newFormattable.Text));
        }
    }
}
