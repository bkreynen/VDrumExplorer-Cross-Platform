// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using VDrumExplorer.Model;
using VDrumExplorer.ViewModel.Data;
using VDrumExplorer.ViewModel.Test.Fakes;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Data
{
    /// <summary>
    /// Tests for the flat field-list mode (Phase 1 task 4, docs/accessibility.md §4):
    /// the off-by-default toggle, section construction in schema-tree order, the
    /// zero-divergence identity between the flat list's field view models and the
    /// details pane's (for the selected node), the kit scope in the Module Explorer,
    /// and the status announcement on toggling. The view-level behavior (visibility
    /// toggling, editing through the flat editors) is covered by the headless
    /// interaction tests in the GUI test project.
    /// </summary>
    public class FlatFieldListViewModelTest
    {
        private readonly Module module = TestData.LoadTD27Module();

        private KitExplorerViewModel CreateKitExplorer() =>
            new KitExplorerViewModel(new FakeViewServices(), NullLogger.Instance, new DeviceViewModel(), module.ExportKit(1));

        private ModuleExplorerViewModel CreateModuleExplorer() =>
            new ModuleExplorerViewModel(new FakeViewServices(), NullLogger.Instance, new DeviceViewModel(), module);

        // === Toggle: default off / on / off ===

        [Fact]
        public void FlatMode_DefaultOff_SectionsEmpty_TreeVisible()
        {
            var vm = CreateKitExplorer();

            Assert.False(vm.FlatFields.IsFlatModeEnabled);
            Assert.True(vm.FlatFields.IsTreeModeVisible);
            Assert.Empty(vm.FlatFields.Sections);
            Assert.Equal(0, vm.FlatFields.FieldCount);
        }

        [Fact]
        public void ToggleFlatMode_On_SectionsBuilt_TreeHidden()
        {
            var vm = CreateKitExplorer();

            vm.FlatFields.ToggleFlatModeCommand.Execute(null!);

            Assert.True(vm.FlatFields.IsFlatModeEnabled);
            Assert.False(vm.FlatFields.IsTreeModeVisible);
            Assert.NotEmpty(vm.FlatFields.Sections);
            Assert.True(vm.FlatFields.FieldCount > 0, "Flat list should contain editable fields.");
        }

        [Fact]
        public void ToggleFlatMode_OffAgain_SectionsCleared_TreeVisibleAgain()
        {
            var vm = CreateKitExplorer();
            vm.FlatFields.ToggleFlatModeCommand.Execute(null!);
            Assert.True(vm.FlatFields.IsFlatModeEnabled);

            vm.FlatFields.ToggleFlatModeCommand.Execute(null!);

            Assert.False(vm.FlatFields.IsFlatModeEnabled);
            Assert.True(vm.FlatFields.IsTreeModeVisible);
            Assert.Empty(vm.FlatFields.Sections);
        }

        // === Status announcement on toggle (docs/accessibility.md §5) ===

        [Fact]
        public void ToggleFlatMode_On_AnnouncesSectionAndFieldCounts()
        {
            var vm = CreateKitExplorer();

            vm.FlatFields.ToggleFlatModeCommand.Execute(null!);

            string expected =
                $"Flat field list on: {vm.FlatFields.Sections.Count} sections, {vm.FlatFields.FieldCount} fields";
            Assert.Equal(expected, vm.Status.Message);
            Assert.Equal("", vm.Status.ErrorMessage);
        }

        [Fact]
        public void ToggleFlatMode_Off_AnnouncesOff()
        {
            var vm = CreateKitExplorer();
            vm.FlatFields.ToggleFlatModeCommand.Execute(null!);
            Assert.NotEqual("", vm.Status.Message);

            vm.FlatFields.ToggleFlatModeCommand.Execute(null!);

            Assert.Equal("Flat field list off", vm.Status.Message);
            Assert.Equal("", vm.Status.ErrorMessage);
        }

        // === Section order: schema-tree traversal order ===

        [Fact]
        public void Sections_OrderMatchesSchemaTreeTraversal()
        {
            var vm = CreateKitExplorer();
            vm.FlatFields.ToggleFlatModeCommand.Execute(null!);

            // Expected order: a depth-first walk of the tree, one section per
            // field-container detail per node, containers in the node's detail order.
            var expectedHeadings = new List<string>();
            CollectExpectedHeadings(vm.Root[0], expectedHeadings);

            var actualHeadings = vm.FlatFields.Sections.Select(s => s.Heading).ToList();
            Assert.Equal(expectedHeadings, actualHeadings);
        }

        [Fact]
        public void Sections_FieldOrderMatchesContainerFieldOrder()
        {
            var vm = CreateKitExplorer();
            vm.FlatFields.ToggleFlatModeCommand.Execute(null!);

            Assert.All(vm.FlatFields.Sections, section =>
                Assert.Equal(
                    section.Container.Fields.Select(f => f.Description),
                    section.Fields.Select(f => f.Description)));
        }

        private static void CollectExpectedHeadings(DataTreeNodeViewModel node, List<string> headings)
        {
            foreach (var detail in node.CreateDetails())
            {
                if (detail is FieldContainerDataNodeDetailViewModel container)
                {
                    headings.Add($"{node.Title} — {container.Description}");
                }
            }
            foreach (var child in node.Children)
            {
                CollectExpectedHeadings(child, headings);
            }
        }

        // === Zero divergence: the selected node's section reuses the details-pane instances ===

        [Fact]
        public void Sections_SelectedNode_ContainersAndFieldsAreDetailsPaneInstances()
        {
            var vm = CreateKitExplorer();
            // Force the details pane to be built for the selected root node.
            Assert.NotNull(vm.SelectedNodeDetails);

            vm.FlatFields.ToggleFlatModeCommand.Execute(null!);

            var detailContainers = vm.SelectedNodeDetails!
                .OfType<FieldContainerDataNodeDetailViewModel>()
                .ToList();
            Assert.NotEmpty(detailContainers);

            var selectedSections = vm.FlatFields.Sections
                .Where(s => ReferenceEquals(s.Node, vm.SelectedNode))
                .ToList();
            Assert.NotEmpty(selectedSections);

            foreach (var section in selectedSections)
            {
                // The container itself is the exact instance the details pane shows.
                var detailContainer = Assert.Single(detailContainers,
                    c => ReferenceEquals(c, section.Container));

                // Every field row is the exact field view model the details pane binds —
                // one editing path, zero divergence.
                Assert.Equal(detailContainer.Fields.Count, section.Fields.Count);
                for (int i = 0; i < section.Fields.Count; i++)
                {
                    Assert.Same(detailContainer.Fields[i], section.Fields[i]);
                }
            }
        }

        [Fact]
        public void Sections_OtherNodes_UseFreshContainers()
        {
            var vm = CreateKitExplorer();
            vm.FlatFields.ToggleFlatModeCommand.Execute(null!);

            // Sections of nodes other than the selection get fresh containers (they are
            // displayed nowhere else), so none of them may be a details-pane instance.
            var detailsPaneContainers = vm.SelectedNodeDetails!
                .OfType<FieldContainerDataNodeDetailViewModel>()
                .ToList();
            var otherNodeSections = vm.FlatFields.Sections
                .Where(s => !ReferenceEquals(s.Node, vm.SelectedNode))
                .ToList();

            Assert.All(otherNodeSections, section =>
                Assert.All(detailsPaneContainers, detailsContainer =>
                    Assert.NotSame(detailsContainer, section.Container)));
        }

        // === Section metadata ===

        [Fact]
        public void Section_HeadingIsNodeAndContainer()
        {
            var vm = CreateKitExplorer();
            vm.FlatFields.ToggleFlatModeCommand.Execute(null!);

            var section = vm.FlatFields.Sections[0];
            Assert.Equal($"{section.Node.Title} — {section.Container.Description}", section.Heading);
            Assert.Equal(FlatFieldSectionViewModel.IdPrefixConst, section.IdPrefix);
        }

        // === Scope in the Module Explorer: the kit of the selected node ===

        [Fact]
        public void ModuleExplorer_SectionsOnlyFromSelectedKit()
        {
            var vm = CreateModuleExplorer();
            var kitRoot = FindAllKitRoots(vm.Root[0]).First(kit => kit.KitNumber == 3);
            vm.SelectedNode = kitRoot;

            vm.FlatFields.ToggleFlatModeCommand.Execute(null!);

            Assert.NotEmpty(vm.FlatFields.Sections);
            Assert.All(vm.FlatFields.Sections, section => Assert.Equal(kitRoot.KitNumber, section.Node.KitNumber));
        }

        private static List<DataTreeNodeViewModel> FindAllKitRoots(DataTreeNodeViewModel node)
        {
            var result = new List<DataTreeNodeViewModel>();
            FindRecursive(node, result);
            return result;
            static void FindRecursive(DataTreeNodeViewModel current, List<DataTreeNodeViewModel> acc)
            {
                if (current.IsKitRoot)
                {
                    acc.Add(current);
                }
                foreach (var child in current.Children)
                {
                    FindRecursive(child, acc);
                }
            }
        }
    }
}
