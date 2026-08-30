// Copyright 2023 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using System;
using System.Linq;
using VDrumExplorer.Model.Schema.Physical;

namespace VDrumExplorer.Model.Test.Schema.Physical;

internal class ContainerBaseTest
{
    private ContainerContainer root = null!;

    [SetUp]
    public void SetUp()
    {
        root = TestData.LoadTD27().Schema.PhysicalRoot;
    }

    // --- Basic properties ---

    [Test]
    public void Root_NameIsNonEmpty()
    {
        Assert.IsFalse(string.IsNullOrEmpty(root.Name));
    }

    [Test]
    public void Root_DescriptionIsNonEmpty()
    {
        Assert.IsFalse(string.IsNullOrEmpty(root.Description));
    }

    [Test]
    public void Root_AddressIsNotNull()
    {
        Assert.IsNotNull(root.Address);
    }

    [Test]
    public void Root_PathIsSlash()
    {
        Assert.AreEqual("/", root.Path);
    }

    [Test]
    public void Root_ParentIsNull()
    {
        Assert.IsNull(root.Parent);
    }

    [Test]
    public void Root_SchemaIsNotNull()
    {
        Assert.IsNotNull(root.Schema);
    }

    // --- Path property format ---

    [Test]
    public void ChildContainer_PathIncludesParentPath()
    {
        var child = root.Containers.First();
        Assert.AreEqual("/" + child.Name, child.Path);
    }

    [Test]
    public void GrandchildContainer_PathIncludesFullAncestry()
    {
        var child = root.Containers.First(c => c is ContainerContainer) as ContainerContainer;
        Assume.That(child, Is.Not.Null);
        var grandchild = child!.Containers.First();
        Assert.AreEqual($"/{child.Name}/{grandchild.Name}", grandchild.Path);
    }

    // --- ResolveContainer ---

    [Test]
    public void ResolveContainer_EmptyString_ReturnsSelf()
    {
        Assert.AreSame(root, root.ResolveContainer(""));
    }

    [Test]
    public void ResolveContainer_Dot_ReturnsSelf()
    {
        Assert.AreSame(root, root.ResolveContainer("."));
    }

    [Test]
    public void ResolveContainer_RelativePath_ReturnsCorrectContainer()
    {
        var child = root.Containers.First();
        var resolved = root.ResolveContainer(child.Name);
        Assert.AreSame(child, resolved);
    }

    [Test]
    public void ResolveContainer_DeepRelativePath_ReturnsCorrectContainer()
    {
        var child = root.Containers.First(c => c is ContainerContainer) as ContainerContainer;
        Assume.That(child, Is.Not.Null);
        var grandchild = child!.Containers.First();
        var resolved = root.ResolveContainer($"{child.Name}/{grandchild.Name}");
        Assert.AreSame(grandchild, resolved);
    }

    [Test]
    public void ResolveContainer_AbsolutePath_ReturnsCorrectContainer()
    {
        var child = root.Containers.First();
        var resolved = child.ResolveContainer("/" + child.Name);
        Assert.AreSame(child, resolved);
    }

    [Test]
    public void ResolveContainer_AbsolutePath_FromChild_ReturnsOtherChild()
    {
        var firstChild = root.Containers.First();
        var lastChild = root.Containers.Last();
        var resolved = firstChild.ResolveContainer("/" + lastChild.Name);
        Assert.AreSame(lastChild, resolved);
    }

    [Test]
    public void ResolveContainer_ParentPath_ReturnsSibling()
    {
        var firstChild = root.Containers.First();
        var lastChild = root.Containers.Last();
        var resolved = firstChild.ResolveContainer("../" + lastChild.Name);
        Assert.AreSame(lastChild, resolved);
    }

    [Test]
    public void ResolveContainer_ParentPath_FromRoot_Throws()
    {
        Assert.Throws<ArgumentException>(() => root.ResolveContainer("../Setup"));
    }

    [Test]
    public void ResolveContainer_InvalidName_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => root.ResolveContainer("Nonexistent"));
        Assert.That(ex!.Message, Does.Contain("Nonexistent"));
    }

    [Test]
    public void ResolveContainer_InvalidNestedName_Throws()
    {
        var child = root.Containers.First(c => c is ContainerContainer) as ContainerContainer;
        Assume.That(child, Is.Not.Null);
        var ex = Assert.Throws<ArgumentException>(() => root.ResolveContainer($"{child.Name}/Nonexistent"));
        Assert.That(ex!.Message, Does.Contain("Nonexistent"));
    }

    [Test]
    public void ResolveContainer_PathThroughFieldContainer_Throws()
    {
        // A FieldContainer has no child containers, so resolving through it should fail.
        var fieldContainer = root.DescendantsAndSelf().OfType<FieldContainer>().First();
        var ex = Assert.Throws<ArgumentException>(() => fieldContainer.ResolveContainer("Nonexistent"));
        Assert.That(ex!.Message, Does.Contain("Nonexistent"));
    }

    // --- ResolveField ---

    [Test]
    public void ResolveField_ValidFieldName_ReturnsField()
    {
        var fieldContainer = root.DescendantsAndSelf().OfType<FieldContainer>().First();
        var field = fieldContainer.Fields.First();
        var resolved = fieldContainer.ResolveField(field.Name);
        Assert.AreSame(field, resolved);
    }

    [Test]
    public void ResolveField_ValidPath_ReturnsField()
    {
        var fieldContainer = root.DescendantsAndSelf().OfType<FieldContainer>().First();
        var field = fieldContainer.Fields.First();
        var resolved = root.ResolveField($"{fieldContainer.Path.Substring(1)}/{field.Name}");
        Assert.AreSame(field, resolved);
    }

    [Test]
    public void ResolveField_InvalidFieldName_Throws()
    {
        var fieldContainer = root.DescendantsAndSelf().OfType<FieldContainer>().First();
        var ex = Assert.Throws<ArgumentException>(() => fieldContainer.ResolveField("NonexistentField"));
        Assert.That(ex!.Message, Does.Contain("NonexistentField"));
    }

    [Test]
    public void ResolveField_OnContainerContainer_Throws()
    {
        // Resolving a field on a ContainerContainer (not a FieldContainer) should throw.
        var ex = Assert.Throws<ArgumentException>(() => root.ResolveField("NonexistentField"));
        Assert.That(ex!.Message, Does.Contain("not a field container"));
    }

    // --- DescendantsAndSelf ---

    [Test]
    public void DescendantsAndSelf_IncludesSelf()
    {
        var descendants = root.DescendantsAndSelf().ToList();
        Assert.IsTrue(descendants.Contains(root));
    }

    [Test]
    public void DescendantsAndSelf_ReturnsBfsOrder()
    {
        // BFS: root first, then all direct children, then grandchildren.
        var descendants = root.DescendantsAndSelf().ToList();
        Assert.AreSame(root, descendants[0]);

        // All direct children should come before any grandchild.
        var directChildIndices = root.Containers.Select(c => descendants.IndexOf(c)).ToList();
        var firstGrandchildIndex = descendants
            .Where(d => d.Parent != null && d.Parent != root)
            .Select(d => descendants.IndexOf(d))
            .DefaultIfEmpty(-1)
            .Min();

        if (firstGrandchildIndex > 0)
        {
            foreach (var idx in directChildIndices)
            {
                Assert.Less(idx, firstGrandchildIndex, "Direct child should come before any grandchild in BFS order");
            }
        }
    }

    [Test]
    public void DescendantsAndSelf_ReturnsAllNodes()
    {
        var descendants = root.DescendantsAndSelf().ToList();

        // Count via independent recursive traversal — should equal BFS count.
        // This makes the test non-tautological: previously the ancestor walk was structurally guaranteed.
        int expectedCount = CountRecursive(root);
        Assert.AreEqual(expectedCount, descendants.Count,
            $"DescendantsAndSelf should return {expectedCount} containers for TD-27");

        // No duplicates / no cycles
        Assert.AreEqual(descendants.Count, descendants.Distinct().Count(),
            "DescendantsAndSelf should not contain duplicates (cycle check)");

        // Structural invariant: every node ultimately reaches root via Parent.
        foreach (var node in descendants)
        {
            var current = node;
            while (current.Parent != null)
            {
                current = current.Parent!;
            }
            Assert.AreSame(root, current);
        }

        static int CountRecursive(IContainer container) =>
            1 + (container is ContainerContainer cc ? cc.Containers.Sum(CountRecursive) : 0);
    }

    // --- ToString ---

    [Test]
    public void ToString_ReturnsDescription()
    {
        Assert.AreEqual(root.Description, root.ToString());
    }
}
