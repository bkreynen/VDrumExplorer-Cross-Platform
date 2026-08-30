// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using VDrumExplorer.Model;
using VDrumExplorer.Model.Data.Logical;
using VDrumExplorer.ViewModel.Data;
using VDrumExplorer.ViewModel.Test.Fakes;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Data
{
    public class DataTreeNodeViewModelTest
    {
        private readonly Module module = TestData.LoadTD27Module();
        private readonly ModuleExplorerViewModel root;

        public DataTreeNodeViewModelTest()
        {
            root = new ModuleExplorerViewModel(new FakeViewServices(), NullLogger.Instance, new DeviceViewModel(), module);
        }

        private DataTreeNodeViewModel KitRootNode => FindKitRoot(root.Root[0]);

        private DataTreeNodeViewModel NonKitRootNode => root.Root[0];

        private DataTreeNodeViewModel ChildOfKitRoot => KitRootNode.Children[0];

        private static DataTreeNodeViewModel FindKitRoot(DataTreeNodeViewModel node)
        {
            if (node.IsKitRoot)
            {
                return node;
            }
            foreach (var child in node.Children)
            {
                var result = FindKitRoot(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null!;
        }

        [Fact]
        public void Title_KitRootNode_ContainsKitName()
        {
            var node = KitRootNode;
            var kitName = Kit.GetKitName(node.Model);
            Assert.Contains(kitName, node.Title);
        }

        [Fact]
        public void Title_KitRootNode_IncludesKitNumberForModuleExplorer()
        {
            var node = KitRootNode;
            // ModuleExplorer shows "Kit N: name" format
            Assert.Contains("Kit", node.Title);
            Assert.Contains(":", node.Title);
        }

        [Fact]
        public void Title_NonKitNode_ContainsFormattedString()
        {
            var node = NonKitRootNode;
            // The root node's title is the formattable string text
            Assert.NotNull(node.Title);
            Assert.NotEmpty(node.Title);
        }

        [Fact]
        public void Children_NodeWithChildren_NonEmpty()
        {
            var node = KitRootNode;
            Assert.NotEmpty(node.Children);
        }

        [Fact]
        public void Children_LeafNode_Empty()
        {
            // Find a leaf node by walking down the tree
            var node = KitRootNode;
            while (node.Children.Count > 0)
            {
                node = node.Children[0];
            }
            Assert.Empty(node.Children);
        }

        [Fact]
        public void KitNumber_KitRootNode_ReturnsKitNumber()
        {
            var node = KitRootNode;
            Assert.NotNull(node.KitNumber);
            Assert.True(node.KitNumber > 0);
        }

        [Fact]
        public void KitNumber_ChildOfKitRoot_WalksUpTree()
        {
            var child = ChildOfKitRoot;
            // The child should have the same kit number as the kit root
            Assert.Equal(KitRootNode.KitNumber, child.KitNumber);
        }

        [Fact]
        public void KitNumber_ModuleRootNode_ReturnsNull()
        {
            var node = NonKitRootNode;
            // The module root has no kit number
            Assert.Null(node.KitNumber);
        }

        [Fact]
        public void IsKitRoot_KitRootNode_ReturnsTrue()
        {
            var node = KitRootNode;
            Assert.True(node.IsKitRoot);
        }

        [Fact]
        public void IsKitRoot_NonKitNode_ReturnsFalse()
        {
            var node = NonKitRootNode;
            Assert.False(node.IsKitRoot);
        }

        [Fact]
        public void IsKitRoot_ChildOfKitRoot_ReturnsFalse()
        {
            var child = ChildOfKitRoot;
            Assert.False(child.IsKitRoot);
        }

        [Fact]
        public void KitContextCommandsEnabled_ModuleExplorerKitRoot_ReturnsTrue()
        {
            var node = KitRootNode;
            // Root is a ModuleExplorerViewModel, so IsModuleExplorer is true
            Assert.True(node.KitContextCommandsEnabled);
        }

        [Fact]
        public void KitContextCommandsEnabled_ModuleExplorerNonKitRoot_ReturnsFalse()
        {
            var node = NonKitRootNode;
            Assert.False(node.KitContextCommandsEnabled);
        }

        [Fact]
        public void KitContextCommandsEnabled_ModuleExplorerChildOfKitRoot_ReturnsFalse()
        {
            var child = ChildOfKitRoot;
            Assert.False(child.KitContextCommandsEnabled);
        }

        [Fact]
        public void GetMidiNote_NodeWithoutMidiNotePath_ReturnsNull()
        {
            // The module root node has no MIDI note path — enforce premise so test fails if schema changes
            var node = NonKitRootNode;
            Assert.Null(node.MidiNotePath);
            Assert.Null(node.GetMidiNote());
        }

        [Fact]
        public void GetMidiNote_NodeWithMidiNotePath_ReturnsNote()
        {
            // Find a node with a MIDI note path
            var schemaRoot = module.Schema.LogicalRoot;
            var midiNode = schemaRoot.DescendantsAndSelf().First(n => n.MidiNotePath != null);
            // Find the corresponding DataTreeNodeViewModel by walking the tree
            var vm = FindNodeByPath(root.Root[0], midiNode.Path);
            Assert.NotNull(vm);
            var note = vm!.GetMidiNote();
            // The note should be a valid MIDI note (0-127) or null if "off"
            Assert.True(note is null || (note >= 0 && note < 128));
        }

        [Fact]
        public void MidiNotePath_NodeWithoutMidiNotePath_ReturnsNull()
        {
            var node = NonKitRootNode;
            Assert.Null(node.Model.SchemaNode.MidiNotePath);
            Assert.Null(node.MidiNotePath);
        }

        [Fact]
        public void MidiNotePath_NodeWithMidiNotePath_ReturnsPath()
        {
            var schemaRoot = module.Schema.LogicalRoot;
            var midiNode = schemaRoot.DescendantsAndSelf().First(n => n.MidiNotePath != null);
            var vm = FindNodeByPath(root.Root[0], midiNode.Path);
            Assert.NotNull(vm);
            Assert.Equal(midiNode.MidiNotePath, vm!.MidiNotePath);
        }

        [Fact]
        public void CreateDetails_ReturnsCorrectDetailViewModels()
        {
            var node = KitRootNode;
            var details = node.CreateDetails();
            Assert.NotEmpty(details);
            // Each detail should have a non-empty description
            foreach (var detail in details)
            {
                Assert.NotNull(detail.Description);
                Assert.NotEmpty(detail.Description);
            }
        }

        [Fact]
        public void CreateDetails_ModuleRoot_ReturnsDetails()
        {
            var node = NonKitRootNode;
            var details = node.CreateDetails();
            // The module root should have details
            Assert.NotEmpty(details);
        }

        [Fact]
        public void Root_ReturnsParentExplorerViewModel()
        {
            var node = KitRootNode;
            Assert.Same(root, node.Root);
        }

        [Fact]
        public void Model_ReturnsDataTreeNode()
        {
            var node = KitRootNode;
            Assert.NotNull(node.Model);
            Assert.Same(node.Model.SchemaNode, module.Schema.GetKitRoot(node.KitNumber!.Value));
        }

        private static DataTreeNodeViewModel? FindNodeByPath(DataTreeNodeViewModel node, string path)
        {
            if (node.Model.SchemaNode.Path == path)
            {
                return node;
            }
            foreach (var child in node.Children)
            {
                var found = FindNodeByPath(child, path);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }
    }
}
