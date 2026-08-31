// Copyright 2024 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using System.Linq;
using VDrumExplorer.Model.Schema.Logical;
using VDrumExplorer.Model.Schema.Physical;

namespace VDrumExplorer.Model.Test.Schema.Logical
{
    public class FieldContainerNodeDetailTest
    {
        private ModuleSchema schema = null!;
        private FieldContainerNodeDetail detail = null!;

        [SetUp]
        public void SetUp()
        {
            schema = TestData.LoadTD27().Schema;
            // Find a FieldContainerNodeDetail in the schema's logical tree.
            detail = schema.LogicalRoot.DescendantsAndSelf()
                .SelectMany(n => n.Details)
                .OfType<FieldContainerNodeDetail>()
                .First();
        }

        [Test]
        public void Description_IsNonEmpty()
        {
            Assert.IsFalse(string.IsNullOrEmpty(detail.Description));
        }

        [Test]
        public void Container_IsNotNull()
        {
            Assert.IsNotNull(detail.Container);
            Assert.IsInstanceOf<FieldContainer>(detail.Container);
        }

        [Test]
        public void Container_HasValidPath()
        {
            Assert.IsFalse(string.IsNullOrEmpty(detail.Container.Path));
        }

        [Test]
        public void ToString_ContainsDescription()
        {
            Assert.That(detail.ToString(), Does.Contain(detail.Description));
        }

        [Test]
        public void ToString_ContainsContainerPath()
        {
            Assert.That(detail.ToString(), Does.Contain(detail.Container.Path));
        }

        [Test]
        public void ToString_HasExpectedFormat()
        {
            // ToString format: "{Description}: {Container.Path}"
            Assert.AreEqual($"{detail.Description}: {detail.Container.Path}", detail.ToString());
        }

        [Test]
        public void Constructor_SetsProperties()
        {
            var container = schema.PhysicalRoot.DescendantsAndSelf()
                .OfType<FieldContainer>()
                .First();
            var newDetail = new FieldContainerNodeDetail("Test description", container);
            Assert.AreEqual("Test description", newDetail.Description);
            Assert.AreSame(container, newDetail.Container);
        }
    }
}
