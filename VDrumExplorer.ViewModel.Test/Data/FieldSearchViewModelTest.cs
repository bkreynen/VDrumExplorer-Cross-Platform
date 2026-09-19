// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using VDrumExplorer.Model;
using VDrumExplorer.ViewModel.Data;
using VDrumExplorer.ViewModel.Test.Fakes;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Data
{
    /// <summary>
    /// Tests for the jump-to-field search (Phase 1 task 4): the pure token matcher
    /// (exact word &gt; word prefix &gt; substring &gt; subsequence, all tokens must match),
    /// result ranking and the selection/jump/announcement flow. The view-level behavior
    /// (focus mechanics) is covered by the headless interaction tests in the GUI test project.
    /// </summary>
    public class FieldSearchViewModelTest
    {
        private readonly Module module = TestData.LoadTD27Module();

        private KitExplorerViewModel CreateKitExplorer() =>
            new KitExplorerViewModel(new FakeViewServices(), NullLogger.Instance, new DeviceViewModel(), module.ExportKit(1));

        private ModuleExplorerViewModel CreateModuleExplorer() =>
            new ModuleExplorerViewModel(new FakeViewServices(), NullLogger.Instance, new DeviceViewModel(), module);

        // === Matcher: exact-word / word-prefix / substring / subsequence / no-match ===

        private const string KitVolumeText = "Kit 1: Premium Wood — Kit common — Kit volume";

        [Theory]
        [InlineData("kit volume", 8)]        // "kit" exact word (4) + "volume" exact word (4)
        [InlineData("Kit Volume", 8)]        // case-insensitive
        [InlineData("kit vol", 7)]           // "kit" exact (4) + "vol" word-prefix (3)
        [InlineData("volume", 4)]            // exact word
        [InlineData("volu", 3)]              // word-prefix
        [InlineData("olume", 2)]             // substring (not a word prefix)
        [InlineData("vlu", 1)]               // subsequence fallback
        [InlineData("kv", 1)]                // subsequence across words
        [InlineData("premium wood", 8)]      // multi-token, container/node words
        public void ScoreDisplayText_MatchKinds(string query, int expectedScore)
        {
            Assert.Equal(expectedScore, FieldSearchViewModel.ScoreDisplayText(KitVolumeText, query));
        }

        [Theory]
        [InlineData("zzz qx")]               // no token matches at all
        [InlineData("kit zzqx")]             // one token matches, one does not
        [InlineData("xkiv")]                 // subsequence would work per-char, but not in order
        public void ScoreDisplayText_NoMatch_ReturnsZero(string query)
        {
            Assert.Equal(0, FieldSearchViewModel.ScoreDisplayText(KitVolumeText, query));
        }

        [Fact]
        public void ScoreDisplayText_EmptyQuery_MatchesNothing()
        {
            Assert.Equal(0, FieldSearchViewModel.ScoreDisplayText(KitVolumeText, ""));
            Assert.Equal(0, FieldSearchViewModel.ScoreDisplayText(KitVolumeText, "   "));
        }

        [Fact]
        public void ScoreDisplayText_AllTokensMustMatch_TokenOrderIrrelevant()
        {
            // Reordering tokens doesn't change the score: it's a sum over per-token matches.
            Assert.Equal(
                FieldSearchViewModel.ScoreDisplayText(KitVolumeText, "volume kit"),
                FieldSearchViewModel.ScoreDisplayText(KitVolumeText, "kit volume"));
        }

        // === Open / close lifecycle ===

        [Fact]
        public void Search_ClosedByDefault_NoResults()
        {
            var vm = CreateKitExplorer();
            Assert.False(vm.FieldSearch.IsOpen);
            Assert.Empty(vm.FieldSearch.Results);
            Assert.Null(vm.FieldSearch.SelectedResult);
        }

        [Fact]
        public void OpenSearch_RaisesFocusSearchRequested()
        {
            var vm = CreateKitExplorer();
            vm.FieldSearch.FocusSearchRequested += (s, e) => raisedCount++;
            vm.FieldSearch.OpenSearchCommand.Execute(null!);

            Assert.True(vm.FieldSearch.IsOpen);
            Assert.Equal(1, raisedCount);
        }

        [Fact]
        public void CloseSearch_ClearsQueryResultsAndSelection()
        {
            var vm = CreateKitExplorer();
            vm.FieldSearch.OpenSearchCommand.Execute(null!);
            vm.FieldSearch.Query = "kit volume";
            Assert.NotEmpty(vm.FieldSearch.Results);

            vm.FieldSearch.CloseSearchCommand.Execute(null!);

            Assert.False(vm.FieldSearch.IsOpen);
            Assert.Equal("", vm.FieldSearch.Query);
            Assert.Empty(vm.FieldSearch.Results);
            Assert.Null(vm.FieldSearch.SelectedResult);
        }

        // === Matching, ranking and selection ===

        [Fact]
        public void Query_RanksResults_ScoresNonIncreasing_DisplayTextTieBreak()
        {
            var vm = CreateKitExplorer();
            vm.FieldSearch.OpenSearchCommand.Execute(null!);
            vm.FieldSearch.Query = "kit volume";

            var results = vm.FieldSearch.Results;
            Assert.NotEmpty(results);
            Assert.True(results.Count <= 10, "Result list should be capped at 10 rows.");
            for (int i = 1; i < results.Count; i++)
            {
                Assert.True(results[i - 1].MatchScore >= results[i].MatchScore,
                    $"Results must be ordered by descending match score (row {i - 1} vs {i}).");
                if (results[i - 1].MatchScore == results[i].MatchScore)
                {
                    Assert.True(string.CompareOrdinal(results[i - 1].DisplayText, results[i].DisplayText) <= 0,
                        $"Equal-score results must tie-break by DisplayText ordinal (row {i - 1} vs {i}).");
                }
            }
        }

        [Fact]
        public void Query_MultiTokenMatch_FindsKitVolume_AndOnlyScoredResults()
        {
            var vm = CreateKitExplorer();
            vm.FieldSearch.OpenSearchCommand.Execute(null!);
            vm.FieldSearch.Query = "kit volume";

            var results = vm.FieldSearch.Results;
            Assert.NotEmpty(results);
            // The kit volume field is matched (exact-word score for both tokens).
            Assert.Contains(results, r => r.Container.Description == "Kit common" && r.Field.Description == "Kit volume");
            // Every shown result genuinely matched the query (score recomputed > 0).
            Assert.All(results, result => Assert.True(FieldSearchViewModel.ScoreDisplayText(result.DisplayText, "kit volume") > 0));
        }

        [Fact]
        public void Query_AllTokensMustMatch_OneFailingTokenFiltersOut()
        {
            var vm = CreateKitExplorer();
            vm.FieldSearch.OpenSearchCommand.Execute(null!);
            vm.FieldSearch.Query = "kit volume";
            Assert.NotEmpty(vm.FieldSearch.Results);

            // "zzqx" cannot match any field text as word/prefix/substring/subsequence, so no
            // result can match both tokens even though "kit" matches plenty.
            vm.FieldSearch.Query = "kit zzqx";

            Assert.Empty(vm.FieldSearch.Results);
            Assert.Null(vm.FieldSearch.SelectedResult);
        }

        [Fact]
        public void Query_NoMatch_ShowsEmptyState_WhenOpen()
        {
            var vm = CreateKitExplorer();
            vm.FieldSearch.OpenSearchCommand.Execute(null!);
            Assert.False(vm.FieldSearch.HasNoResults);

            vm.FieldSearch.Query = "zzz qx nofield";

            Assert.Empty(vm.FieldSearch.Results);
            Assert.Null(vm.FieldSearch.SelectedResult);
            Assert.True(vm.FieldSearch.HasNoResults);
        }

        [Fact]
        public void SelectedResult_IsFirstResult_AndFlaggedSelected()
        {
            var vm = CreateKitExplorer();
            vm.FieldSearch.OpenSearchCommand.Execute(null!);
            vm.FieldSearch.Query = "kit volume";

            var first = vm.FieldSearch.Results[0];
            Assert.Same(first, vm.FieldSearch.SelectedResult);
            Assert.True(first.IsSelected);
            Assert.All(vm.FieldSearch.Results.Where(r => !ReferenceEquals(r, first)), r => Assert.False(r.IsSelected));
        }

        // === Result structure: node, container, field, display text ===

        [Fact]
        public void Results_DisplayTextIsNodeContainerField()
        {
            var vm = CreateKitExplorer();
            vm.FieldSearch.OpenSearchCommand.Execute(null!);
            vm.FieldSearch.Query = "kit volume";

            var result = vm.FieldSearch.Results[0];
            Assert.Equal($"{result.Node.Title} — {result.Container.Description} — {result.Field.Description}", result.DisplayText);
            Assert.NotEmpty(result.Container.Fields);
        }

        // === Keyboard navigation: FocusNext / FocusPrevious ===

        [Fact]
        public void FocusNextResultCommand_MovesSelection_AndAnnounces()
        {
            var vm = CreateKitExplorer();
            vm.FieldSearch.OpenSearchCommand.Execute(null!);
            vm.FieldSearch.Query = "kit volume";
            Assert.True(vm.FieldSearch.Results.Count >= 2, "Expected multiple 'kit volume' matches to navigate.");

            var second = vm.FieldSearch.Results[1];
            vm.FieldSearch.FocusNextResultCommand.Execute(null!);

            Assert.Same(second, vm.FieldSearch.SelectedResult);
            Assert.True(second.IsSelected);
            Assert.False(vm.FieldSearch.Results[0].IsSelected);
            Assert.Equal(second.DisplayText, vm.Status.Message);
        }

        [Fact]
        public void FocusPreviousResultCommand_MovesSelectionBack_AndAnnounces()
        {
            var vm = CreateKitExplorer();
            vm.FieldSearch.OpenSearchCommand.Execute(null!);
            vm.FieldSearch.Query = "kit volume";
            vm.FieldSearch.FocusNextResultCommand.Execute(null!);

            vm.FieldSearch.FocusPreviousResultCommand.Execute(null!);

            var first = vm.FieldSearch.Results[0];
            Assert.Same(first, vm.FieldSearch.SelectedResult);
            Assert.Equal(first.DisplayText, vm.Status.Message);
        }

        [Fact]
        public void FocusNextResultCommand_AtEnd_StaysOnLastResult()
        {
            var vm = CreateKitExplorer();
            vm.FieldSearch.OpenSearchCommand.Execute(null!);
            vm.FieldSearch.Query = "volume";
            int count = vm.FieldSearch.Results.Count;
            Assert.True(count >= 1);
            for (int i = 0; i < count + 5; i++)
            {
                vm.FieldSearch.FocusNextResultCommand.Execute(null!);
            }

            Assert.Same(vm.FieldSearch.Results[count - 1], vm.FieldSearch.SelectedResult);
        }

        [Fact]
        public void FocusNextResultCommand_NoResults_IsNoOp()
        {
            var vm = CreateKitExplorer();
            vm.FieldSearch.OpenSearchCommand.Execute(null!);
            vm.FieldSearch.Query = "zzz qx nofield";

            vm.FieldSearch.FocusNextResultCommand.Execute(null!);

            Assert.Null(vm.FieldSearch.SelectedResult);
        }

        // === Jumping ===

        [Fact]
        public void JumpToSelectedResultCommand_SelectsNode_FlagsFocusedField_Announces_AndRaisesEvent()
        {
            var vm = CreateKitExplorer();
            vm.FieldSearch.OpenSearchCommand.Execute(null!);
            vm.FieldSearch.Query = "kit volume";
            var result = vm.FieldSearch.SelectedResult!;
            int jumped = 0;
            vm.FieldSearch.SearchJumped += (s, e) => jumped++;

            vm.FieldSearch.JumpToSelectedResultCommand.Execute(null!);

            Assert.Same(result.Node, vm.SelectedNode);
            Assert.Equal("Jumped to " + result.DisplayText, vm.Status.Message);
            Assert.Equal(1, jumped);

            // The details pane was rebuilt for the selected node; the fresh container detail
            // (resolved by model identity) must carry the focused field, whose underlying
            // IDataField is the matched one.
            var freshContainer = vm.SelectedNodeDetails!
                .OfType<FieldContainerDataNodeDetailViewModel>()
                .Single(c => ReferenceEquals(c.ModelForTest, result.Container.ModelForTest));
            Assert.Same(freshContainer.Fields.Single(f => ReferenceEquals(f.ModelForTest, result.Field.ModelForTest)),
                freshContainer.FocusedField);
        }

        [Fact]
        public void SelectSearchResultCommand_JumpsToClickedResult_EvenOnAnotherNode()
        {
            var vm = CreateKitExplorer();
            vm.FieldSearch.OpenSearchCommand.Execute(null!);
            vm.FieldSearch.Query = "volume";
            // Jump to a field that lives on a different tree node than the current selection.
            var target = vm.FieldSearch.Results
                .First(r => !ReferenceEquals(r.Node, vm.SelectedNode));

            vm.FieldSearch.SelectSearchResultCommand.Execute(target);

            Assert.Same(target.Node, vm.SelectedNode);
            Assert.Equal("Jumped to " + target.DisplayText, vm.Status.Message);
            var freshContainer = vm.SelectedNodeDetails!
                .OfType<FieldContainerDataNodeDetailViewModel>()
                .Single(c => ReferenceEquals(c.ModelForTest, target.Container.ModelForTest));
            Assert.Same(freshContainer.Fields.Single(f => ReferenceEquals(f.ModelForTest, target.Field.ModelForTest)),
                freshContainer.FocusedField);
        }

        [Fact]
        public void JumpToSelectedResultCommand_NoSelection_IsNoOp()
        {
            var vm = CreateKitExplorer();
            vm.FieldSearch.OpenSearchCommand.Execute(null!);
            vm.FieldSearch.Query = "zzz qx nofield";
            var nodeBefore = vm.SelectedNode;

            vm.FieldSearch.JumpToSelectedResultCommand.Execute(null!);

            Assert.Same(nodeBefore, vm.SelectedNode);
            Assert.Equal("", vm.Status.Message);
        }

        // === Search scope: current kit in the Module Explorer ===

        [Fact]
        public void Query_ModuleExplorer_ResultsOnlyFromSelectedKit()
        {
            var vm = CreateModuleExplorer();
            var kitRoot = FindAllKitRoots(vm.Root[0]).First();
            vm.SelectedNode = kitRoot;

            vm.FieldSearch.OpenSearchCommand.Execute(null!);
            vm.FieldSearch.Query = "kit volume";

            Assert.NotEmpty(vm.FieldSearch.Results);
            Assert.All(vm.FieldSearch.Results, result => Assert.Equal(kitRoot.KitNumber, result.Node.KitNumber));
        }

        private static System.Collections.Generic.List<DataTreeNodeViewModel> FindAllKitRoots(DataTreeNodeViewModel node)
        {
            var result = new System.Collections.Generic.List<DataTreeNodeViewModel>();
            FindRecursive(node, result);
            return result;
            static void FindRecursive(DataTreeNodeViewModel current, System.Collections.Generic.List<DataTreeNodeViewModel> acc)
            {
                if (current.IsKitRoot) acc.Add(current);
                foreach (var child in current.Children) FindRecursive(child, acc);
            }
        }

        private int raisedCount;
    }
}
