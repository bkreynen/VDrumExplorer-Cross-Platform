// Copyright 2024 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using System.Linq;
using VDrumExplorer.Model.Schema.Logical;

namespace VDrumExplorer.Model.Test.Schema.Logical
{
    public class FieldFormattableStringTest
    {
        private ModuleSchema schema = null!;
        private FieldFormattableString formattable = null!;

        [SetUp]
        public void SetUp()
        {
            schema = TestData.LoadTD27().Schema;
            // Find a FieldFormattableString with non-empty format paths.
            formattable = schema.LogicalRoot.DescendantsAndSelf()
                .Select(n => n.Format)
                .First(f => f.FormatPaths.Count > 0);
        }

        [Test]
        public void FormatString_IsNonEmpty()
        {
            Assert.IsFalse(string.IsNullOrEmpty(formattable.FormatString));
        }

        [Test]
        public void Container_IsNotNull()
        {
            Assert.IsNotNull(formattable.Container);
        }

        [Test]
        public void FormatPaths_IsNonEmpty()
        {
            Assert.Greater(formattable.FormatPaths.Count, 0);
        }

        [Test]
        public void FormatPaths_ContainsNonEmptyStrings()
        {
            foreach (var path in formattable.FormatPaths)
            {
                Assert.IsFalse(string.IsNullOrEmpty(path));
            }
        }

        [Test]
        public void FormatPaths_IsNeverNull()
        {
            // Even for nodes with no format paths, FormatPaths should be an empty list, not null.
            var noPathFormat = schema.LogicalRoot.DescendantsAndSelf()
                .Select(n => n.Format)
                .First(f => f.FormatPaths.Count == 0);
            Assert.IsNotNull(noPathFormat.FormatPaths);
            Assert.AreEqual(0, noPathFormat.FormatPaths.Count);
        }

        [Test]
        public void ToString_ReturnsFormatString()
        {
            Assert.AreEqual(formattable.FormatString, formattable.ToString());
        }
    }
}
