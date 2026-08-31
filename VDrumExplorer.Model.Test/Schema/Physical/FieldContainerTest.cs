// Copyright 2023 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Linq;
using VDrumExplorer.Model.Schema.Physical;
using VDrumExplorer.Model.Test.Helpers;

namespace VDrumExplorer.Model.Test.Schema.Physical;

internal class FieldContainerTest
{
    private FieldContainer fieldContainer = null!;
    private ContainerContainer root = null!;

    [SetUp]
    public void SetUp()
    {
        var module = TestData.LoadTD27();
        root = module.Schema.PhysicalRoot;
        // Pinned via shared helper instead of First() – proves fixture stability
        fieldContainer = ModelTestHelpers.FindCurrentFieldContainer(module);
    }

    // --- Fields collection ---

    [Test]
    public void Fields_IsNonEmpty()
    {
        CollectionAssert.IsNotEmpty(fieldContainer.Fields);
    }

    [Test]
    public void Fields_AllHaveParentSetToContainer()
    {
        foreach (var field in fieldContainer.Fields)
        {
            Assert.AreSame(fieldContainer, field.Parent);
        }
    }

    [Test]
    public void Fields_HaveUniqueNames()
    {
        var names = fieldContainer.Fields.Select(f => f.Name).ToList();
        var distinctNames = names.Distinct().ToList();
        CollectionAssert.AreEqual(distinctNames, names);
    }

    // --- Size ---

    [Test]
    public void Size_IsPositive()
    {
        Assert.Greater(fieldContainer.Size, 0);
    }

    // --- GetFieldOrNull ---

    [Test]
    public void GetFieldOrNull_ExistingField_ReturnsField()
    {
        var field = fieldContainer.Fields.First();
        var resolved = fieldContainer.GetFieldOrNull(field.Name);
        Assert.AreSame(field, resolved);
    }

    [Test]
    public void GetFieldOrNull_NonExistingField_ReturnsNull()
    {
        var resolved = fieldContainer.GetFieldOrNull("NonexistentField");
        Assert.IsNull(resolved);
    }

    // --- AddressComparer ---

    [Test]
    public void AddressComparer_LowerAddressFirst()
    {
        var containers = root.DescendantsAndSelf().OfType<FieldContainer>().ToList();
        var low = containers.OrderBy(fc => fc.Address.LogicalValue).First();
        var high = containers.OrderByDescending(fc => fc.Address.LogicalValue).First();

        Assume.That(low.Address, Is.Not.EqualTo(high.Address));

        // low should come before high
        Assert.Less(FieldContainer.AddressComparer.Compare(low, high), 0);
    }

    [Test]
    public void AddressComparer_HigherAddressSecond()
    {
        var containers = root.DescendantsAndSelf().OfType<FieldContainer>().ToList();
        var low = containers.OrderBy(fc => fc.Address.LogicalValue).First();
        var high = containers.OrderByDescending(fc => fc.Address.LogicalValue).First();

        Assume.That(low.Address, Is.Not.EqualTo(high.Address));

        // high should come after low
        Assert.Greater(FieldContainer.AddressComparer.Compare(high, low), 0);
    }

    [Test]
    public void AddressComparer_EqualAddresses_ReturnsZero()
    {
        var container = fieldContainer;
        Assert.AreEqual(0, FieldContainer.AddressComparer.Compare(container, container));
    }

    [Test]
    public void AddressComparer_SortsContainersByAddress()
    {
        var containers = root.DescendantsAndSelf().OfType<FieldContainer>().ToList();
        var sorted = containers.OrderBy(fc => fc, FieldContainer.AddressComparer).ToList();
        var expectedSorted = containers.OrderBy(fc => fc.Address.LogicalValue).ToList();
        CollectionAssert.AreEqual(expectedSorted, sorted);
    }

    // --- Inherited properties ---

    [Test]
    public void Name_IsNonEmpty()
    {
        Assert.IsFalse(string.IsNullOrEmpty(fieldContainer.Name));
    }

    [Test]
    public void Description_IsNonEmpty()
    {
        Assert.IsFalse(string.IsNullOrEmpty(fieldContainer.Description));
    }

    [Test]
    public void Path_StartsWithSlash()
    {
        Assert.IsTrue(fieldContainer.Path.StartsWith("/"));
    }

    [Test]
    public void Parent_IsNotNull()
    {
        Assert.IsNotNull(fieldContainer.Parent);
    }

    [Test]
    public void Schema_IsNotNull()
    {
        Assert.IsNotNull(fieldContainer.Schema);
    }
}
