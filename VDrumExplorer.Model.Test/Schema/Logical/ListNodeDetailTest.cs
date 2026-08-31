// Copyright 2024 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using System.Linq;
using VDrumExplorer.Model.Schema.Logical;

namespace VDrumExplorer.Model.Test.Schema.Logical
{
    public class ListNodeDetailTest
    {
        private ModuleSchema schema = null!;
        private ListNodeDetail detail = null!;

        [SetUp]
        public void SetUp()
        {
            schema = TestData.LoadTD27().Schema;
            // Find a ListNodeDetail in the schema's logical tree.
            detail = schema.LogicalRoot.DescendantsAndSelf()
                .SelectMany(n => n.Details)
                .OfType<ListNodeDetail>()
                .First();
        }

        [Test]
        public void Description_IsNonEmpty()
        {
            Assert.IsFalse(string.IsNullOrEmpty(detail.Description));
        }

        [Test]
        public void Items_IsNonEmpty()
        {
            Assert.Greater(detail.Items.Count, 0);
        }

        [Test]
        public void Items_ContainsFieldFormattableStrings()
        {
            foreach (var item in detail.Items)
            {
                Assert.IsInstanceOf<FieldFormattableString>(item);
            }
        }

        [Test]
        public void Items_HaveNonEmptyFormatStrings()
        {
            foreach (var item in detail.Items)
            {
                Assert.IsFalse(string.IsNullOrEmpty(item.FormatString));
            }
        }

        [Test]
        public void ToString_ContainsDescription()
        {
            Assert.That(detail.ToString(), Does.Contain(detail.Description));
        }

        [Test]
        public void ToString_ContainsItemCount()
        {
            Assert.That(detail.ToString(), Does.Contain(detail.Items.Count.ToString()));
        }

        [Test]
        public void ToString_HasExpectedFormat()
        {
            // ToString format: "{Description}: {Items.Count} fields"
            Assert.AreEqual($"{detail.Description}: {detail.Items.Count} fields", detail.ToString());
        }

        [Test]
        public void Constructor_SetsProperties()
        {
            var items = new[]
            {
                schema.LogicalRoot.Format,
                schema.LogicalRoot.Children.First().Format
            };
            var newDetail = new ListNodeDetail("Test list", items);
            Assert.AreEqual("Test list", newDetail.Description);
            Assert.AreSame(items, newDetail.Items);
            Assert.AreEqual(2, newDetail.Items.Count);
        }
    }
}
