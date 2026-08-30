// Copyright 2023 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;
using VDrumExplorer.Model.Schema.Logical;
using VDrumExplorer.Model.Schema.Physical;

namespace VDrumExplorer.Model.Test.Schema.Logical;

internal class TreeNodeTest
{
    private ModuleSchema schema = null!;
    private TreeNode root = null!;

    [SetUp]
    public void SetUp()
    {
        schema = TestData.LoadTD27().Schema;
        root = schema.LogicalRoot;
    }

    // --- Basic properties ---

    [Test]
    public void Root_HasExpectedName()
    {
        Assert.AreEqual("Root", root.Name);
    }

    [Test]
    public void Root_HasRootPath()
    {
        Assert.AreEqual("/", root.Path);
    }

    [Test]
    public void Root_ParentIsNull()
    {
        Assert.IsNull(root.Parent);
    }

    [Test]
    public void Root_HasChildren()
    {
        // The TD-27 logical root has children: Setup, Kits, SetLists, TriggerBanks.
        Assert.Greater(root.Children.Count, 0);
        CollectionAssert.AreEquivalent(
            new[] { "Setup", "Kits", "SetLists", "TriggerBanks" },
            root.Children.Select(c => c.Name).ToList());
    }

    [Test]
    public void Root_ChildrenHaveParentSetToRoot()
    {
        foreach (var child in root.Children)
        {
            Assert.AreSame(root, child.Parent);
        }
    }

    // --- Path property format ---

    [Test]
    public void ChildNode_PathIncludesParentPath()
    {
        var kits = root.ResolveNode("Kits");
        Assert.AreEqual("/Kits", kits.Path);
    }

    [Test]
    public void GrandchildNode_PathIncludesFullAncestry()
    {
        var kit1 = root.ResolveNode("Kits/Kit[1]");
        Assert.AreEqual("/Kits/Kit[1]", kit1.Path);
    }

    // --- ResolveNode ---

    [Test]
    public void ResolveNode_Self_WithDot_ReturnsSelf()
    {
        Assert.AreSame(root, root.ResolveNode("."));
    }

    [Test]
    public void ResolveNode_Self_WithEmptyString_ReturnsSelf()
    {
        Assert.AreSame(root, root.ResolveNode(""));
    }

    [Test]
    public void ResolveNode_RelativePath_ReturnsCorrectNode()
    {
        var kits = root.ResolveNode("Kits");
        Assert.AreEqual("Kits", kits.Name);
    }

    [Test]
    public void ResolveNode_DeepRelativePath_ReturnsCorrectNode()
    {
        var kit1 = root.ResolveNode("Kits/Kit[1]");
        Assert.AreEqual("Kit[1]", kit1.Name);
    }

    [Test]
    public void ResolveNode_AbsolutePath_ReturnsCorrectNode()
    {
        // From a child, resolve an absolute path back to another child.
        var kits = root.ResolveNode("Kits");
        var setup = kits.ResolveNode("/Setup");
        Assert.AreEqual("Setup", setup.Name);
    }

    [Test]
    public void ResolveNode_AbsolutePath_ToSelf_ReturnsSelf()
    {
        var kits = root.ResolveNode("Kits");
        Assert.AreSame(kits, kits.ResolveNode("/Kits"));
    }

    [Test]
    public void ResolveNode_ParentPath_ReturnsParent()
    {
        var kits = root.ResolveNode("Kits");
        var resolved = kits.ResolveNode("../Setup");
        Assert.AreEqual("Setup", resolved.Name);
    }

    [Test]
    public void ResolveNode_ParentPath_FromRoot_Throws()
    {
        Assert.Throws<ArgumentException>(() => root.ResolveNode("../Setup"));
    }

    [Test]
    public void ResolveNode_InvalidSegment_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => root.ResolveNode("Nonexistent"));
        Assert.That(ex!.Message, Does.Contain("Nonexistent"));
    }

    [Test]
    public void ResolveNode_InvalidNestedSegment_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => root.ResolveNode("Kits/Nonexistent"));
        Assert.That(ex!.Message, Does.Contain("Nonexistent"));
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
        var childNames = new HashSet<string>(root.Children.Select(c => c.Name));
        var directChildIndices = root.Children.Select(c => descendants.IndexOf(c)).ToList();
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

        // Count via independent recursive traversal (DFS) — should equal BFS count.
        // This ensures no node is missed or duplicated and documents the expected schema size.
        int expectedCount = CountRecursive(root);
        Assert.AreEqual(expectedCount, descendants.Count,
            $"DescendantsAndSelf should return {expectedCount} nodes for TD-27 (no missing or extra nodes)");

        // No duplicates / no cycles
        Assert.AreEqual(descendants.Count, descendants.Distinct().Count(),
            "DescendantsAndSelf should not contain duplicates (cycle check)");

        // Every descendant should have the root as an ancestor (transitively) — kept as structural invariant.
        foreach (var node in descendants)
        {
            var current = node;
            while (current.Parent != null)
            {
                current = current.Parent;
            }
            Assert.AreSame(root, current);
        }

        // BFS invariant is also proven in DescendantsAndSelf_ReturnsBfsOrder; here we just verify count.

        static int CountRecursive(TreeNode node) =>
            1 + node.Children.Sum(CountRecursive);
    }

    // --- DescendantFieldContainers ---

    [Test]
    public void DescendantFieldContainers_ReturnsNonEmptyCollection()
    {
        var fieldContainers = root.DescendantFieldContainers().ToList();
        Assert.Greater(fieldContainers.Count, 0);
        Assert.IsTrue(fieldContainers.All(fc => fc is FieldContainer));
    }

    [Test]
    public void DescendantFieldContainers_ReturnsDistinctContainers()
    {
        var fieldContainers = root.DescendantFieldContainers().ToList();
        var distinctPaths = fieldContainers.Select(fc => fc.Path).Distinct().ToList();
        Assert.AreEqual(fieldContainers.Count, distinctPaths.Count);
    }

    // --- KitNumber ---

    [Test]
    public void KitRootNodes_HaveKitNumberSet()
    {
        for (int i = 1; i <= schema.Kits; i++)
        {
            var kitRoot = schema.GetKitRoot(i);
            Assert.AreEqual(i, kitRoot.KitNumber, $"Kit {i} root should have KitNumber={i}");
        }
    }

    [Test]
    public void RootNode_KitNumberIsNull()
    {
        Assert.IsNull(root.KitNumber);
    }

    [Test]
    public void NonKitNode_KitNumberIsNull()
    {
        var setup = root.ResolveNode("Setup");
        Assert.IsNull(setup.KitNumber);
    }

    // --- ToString ---

    [Test]
    public void ToString_ContainsName()
    {
        Assert.That(root.ToString(), Does.Contain("Root"));
    }

    [Test]
    public void ToString_ContainsPath()
    {
        var kits = root.ResolveNode("Kits");
        Assert.That(kits.ToString(), Does.Contain(kits.Container.Path));
    }

    [Test]
    public void ToString_ContainsChildrenCount()
    {
        Assert.That(root.ToString(), Does.Contain($"Children: {root.Children.Count}"));
    }

    [Test]
    public void ToString_ContainsDetailsCount()
    {
        Assert.That(root.ToString(), Does.Contain($"Details: {root.Details.Count}"));
    }

    // --- Container property ---

    [Test]
    public void Container_IsNotNull()
    {
        Assert.IsNotNull(root.Container);
    }

    [Test]
    public void Container_ForRoot_IsPhysicalRoot()
    {
        Assert.AreSame(schema.PhysicalRoot, root.Container);
    }
}
