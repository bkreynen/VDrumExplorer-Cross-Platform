// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Collections.Generic;
using System.Linq;
using VDrumExplorer.Model.Data.Fields;
using VDrumExplorer.Model.Data.Logical;

namespace VDrumExplorer.ViewModel.Test.Data
{
    /// <summary>
    /// Helper methods to find data fields of various types within a loaded module.
    /// </summary>
    internal static class FieldFinder
    {
        /// <summary>
        /// Collects all data fields from the logical tree of a module, including
        /// fields nested within overlay fields.
        /// </summary>
        internal static List<IDataField> CollectAllFields(DataTreeNode root)
        {
            var fields = new List<IDataField>();
            CollectFields(root, fields);
            return fields;
        }

        private static void CollectFields(DataTreeNode node, List<IDataField> fields)
        {
            foreach (var detail in node.Details)
            {
                if (detail is FieldContainerDataNodeDetail fc)
                {
                    CollectFromFields(fc.Fields, fields);
                }
            }
            foreach (var child in node.Children)
            {
                CollectFields(child, fields);
            }
        }

        private static void CollectFromFields(IEnumerable<IDataField> source, List<IDataField> fields)
        {
            foreach (var field in source)
            {
                fields.Add(field);
                if (field is OverlayDataField odf)
                {
                    CollectFromFields(odf.CurrentFieldList.Fields, fields);
                }
            }
        }

        internal static T FirstOf<T>(DataTreeNode root) where T : class, IDataField =>
            CollectAllFields(root).OfType<T>().First();

        /// <summary>
        /// Finds the first field of type <typeparamref name="T"/> matching the given description,
        /// providing deterministic pinning instead of blind <c>.First()</c> order sensitivity.
        /// </summary>
        internal static T FindByDescription<T>(DataTreeNode root, string description) where T : class, IDataField =>
            CollectAllFields(root).OfType<T>().First(f => f.SchemaField.Description == description);

        internal static NumericDataField FindNumericField(DataTreeNode root, string description) =>
            FindByDescription<NumericDataField>(root, description);

        internal static BooleanDataField FindBooleanField(DataTreeNode root, string description) =>
            FindByDescription<BooleanDataField>(root, description);

        internal static EnumDataField FindEnumField(DataTreeNode root, string description) =>
            FindByDescription<EnumDataField>(root, description);
    }
}
