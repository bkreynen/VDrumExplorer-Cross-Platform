// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Collections.Generic;
using System.Linq;
using VDrumExplorer.Model.Data.Logical;
using VDrumExplorer.ViewModel.Status;

namespace VDrumExplorer.ViewModel.Data
{
    /// <summary>
    /// One row of the jump-to-field search: a single editable field of the module data,
    /// with enough context to jump to it (owning tree node, container detail, field view model).
    /// Displayed as "{node} — {container} — {field}" so search results are announced
    /// identifier-first (docs/accessibility.md §2).
    /// </summary>
    public class FieldSearchResultViewModel : ViewModelBase
    {
        /// <summary>
        /// The tree node that contains the field; jumping selects this node.
        /// </summary>
        public DataTreeNodeViewModel Node { get; }

        /// <summary>
        /// The field-container detail the field belongs to ("Kit common", "Main instrument", …),
        /// as generated for the node's current details.
        /// </summary>
        public FieldContainerDataNodeDetailViewModel Container { get; }

        /// <summary>
        /// The field view model the search matched; the jump target that the view focuses.
        /// Overlay fields are flattened, so this is always a concrete editable field view model.
        /// </summary>
        public DataFieldViewModel Field { get; }

        /// <summary>
        /// The announced/displayed text: "{node} — {container} — {field}".
        /// </summary>
        public string DisplayText { get; }

        private bool isSelected;
        /// <summary>
        /// Whether this is the currently highlighted result (keyboard Down/Up selection,
        /// announced on the status line when it changes).
        /// </summary>
        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }

        /// <summary>
        /// Score of the best match of the query tokens against <see cref="DisplayText"/>;
        /// higher sorts earlier. Used for ranking only, not bound in the view.
        /// </summary>
        internal int MatchScore { get; set; }

        internal FieldSearchResultViewModel(DataTreeNodeViewModel node, FieldContainerDataNodeDetailViewModel container, DataFieldViewModel field, string displayText)
        {
            Node = node;
            Container = container;
            Field = field;
            DisplayText = displayText;
        }
    }

    /// <summary>
    /// Jump-to-field search for a data explorer window (Phase 1 task 4, docs/accessibility.md §4).
    /// Opened with Ctrl+K; the query is fuzzy-matched case-insensitively against the fields of
    /// the current kit (all query tokens must match); activating a result selects the owning
    /// tree node (which rebuilds the details pane) and flags the matched field as focused so
    /// the view can move keyboard focus to that field's editor.
    /// <para>
    /// This is a pure ViewModel: matching, ranking and jumping are all here and covered by
    /// <see cref="ViewModel.Test.Data.FieldSearchViewModelTest"/>; the view only performs the
    /// focus mechanics (see DataExplorer.axaml.cs).
    /// </para>
    /// </summary>
    public class FieldSearchViewModel : ViewModelBase
    {
        private readonly DataExplorerViewModel owner;

        /// <summary>Matches an exact word in the field text (best match).</summary>
        private const int ExactWordScore = 4;
        /// <summary>Matches the start of a word in the field text.</summary>
        private const int WordPrefixScore = 3;
        /// <summary>Matches anywhere in the field text (substring).</summary>
        private const int SubstringScore = 2;
        /// <summary>Fallback: all token characters occur in order (subsequence).</summary>
        private const int SubsequenceScore = 1;

        /// <summary>Maximum number of results shown, so the result list stays keyboard-navigable.</summary>
        private const int MaxResults = 10;

        public FieldSearchViewModel(DataExplorerViewModel owner)
        {
            this.owner = owner;
            OpenSearchCommand = new DelegateCommand(OpenSearch, true);
            CloseSearchCommand = new DelegateCommand(CloseSearch, true);
            FocusNextResultCommand = new DelegateCommand(() => MoveSelection(1), true);
            FocusPreviousResultCommand = new DelegateCommand(() => MoveSelection(-1), true);
            JumpToSelectedResultCommand = new DelegateCommand(() => Jump(SelectedResult), true);
            SelectSearchResultCommand = new DelegateCommand<FieldSearchResultViewModel>(result => Jump(result), true);
        }

        private bool isOpen;
        /// <summary>
        /// Whether the search overlay row is visible (collapsed by default, so the
        /// visual layout — and the visual baselines — are unchanged until Ctrl+K).
        /// </summary>
        public bool IsOpen
        {
            get => isOpen;
            private set
            {
                if (SetProperty(ref isOpen, value))
                {
                    RaisePropertyChanged(nameof(HasResults));
                    RaisePropertyChanged(nameof(HasNoResults));
                }
            }
        }

        private string query = "";
        /// <summary>
        /// The current search text; tokens are matched case-insensitively and every token
        /// must match. Changing this recomputes <see cref="Results"/>.
        /// </summary>
        public string Query
        {
            get => query;
            set
            {
                if (SetProperty(ref query, value ?? ""))
                {
                    RaisePropertyChanged(nameof(HasQuery));
                    RaisePropertyChanged(nameof(HasNoResults));
                    UpdateResults();
                }
            }
        }

        private IReadOnlyList<FieldSearchResultViewModel> results = Array.Empty<FieldSearchResultViewModel>();
        /// <summary>The ranked result rows for the current query (at most <see cref="MaxResults"/>).</summary>
        public IReadOnlyList<FieldSearchResultViewModel> Results
        {
            get => results;
            private set => SetProperty(ref results, value);
        }

        private FieldSearchResultViewModel? selectedResult;
        /// <summary>The currently highlighted result (Down/Up keys, Enter jumps to it).</summary>
        public FieldSearchResultViewModel? SelectedResult
        {
            get => selectedResult;
            private set
            {
                if (selectedResult is FieldSearchResultViewModel previous)
                {
                    previous.IsSelected = false;
                }
                selectedResult = value;
                if (selectedResult is FieldSearchResultViewModel current)
                {
                    current.IsSelected = true;
                }
                RaisePropertyChanged(nameof(SelectedResult));
            }
        }

        /// <summary>Whether the query contains any text to search for.</summary>
        public bool HasQuery => query.Trim().Length > 0;

        /// <summary>Whether any result rows are shown.</summary>
        public bool HasResults => Results.Count > 0;

        /// <summary>Whether the "no matching fields" empty state is shown.</summary>
        public bool HasNoResults => IsOpen && HasQuery && Results.Count == 0;

        /// <summary>Focus moves to the search text box (view wiring; raised when the search opens).</summary>
        public event EventHandler? FocusSearchRequested;

        /// <summary>Focus moves to the selected result row (raised after Down/Up selection changes).</summary>
        public event EventHandler? FocusResultsRequested;

        /// <summary>A jump completed; the view focuses (and scrolls to) the target field's editor.</summary>
        public event EventHandler? SearchJumped;

        public DelegateCommand OpenSearchCommand { get; }
        public DelegateCommand CloseSearchCommand { get; }
        public DelegateCommand FocusNextResultCommand { get; }
        public DelegateCommand FocusPreviousResultCommand { get; }
        public DelegateCommand JumpToSelectedResultCommand { get; }
        public DelegateCommand<FieldSearchResultViewModel> SelectSearchResultCommand { get; }

        private void OpenSearch()
        {
            if (!IsOpen)
            {
                IsOpen = true;
                UpdateResults();
            }
            FocusSearchRequested?.Invoke(this, EventArgs.Empty);
        }

        private void CloseSearch()
        {
            IsOpen = false;
            Query = "";
            SelectedResult = null;
        }

        /// <summary>Moves the highlighted result by <paramref name="offset"/> entries, clamping
        /// at the ends of the list; announces the newly selected result and asks the view to
        /// focus it (docs/accessibility.md §4: Down/Up within the search results).</summary>
        private void MoveSelection(int offset)
        {
            if (Results.Count == 0)
            {
                return;
            }
            int index = -1;
            for (int i = 0; i < Results.Count; i++)
            {
                if (ReferenceEquals(Results[i], SelectedResult))
                {
                    index = i;
                    break;
                }
            }
            index = Math.Max(0, Math.Min(Results.Count - 1, index + offset));
            var result = Results[index];
            if (result != SelectedResult)
            {
                SelectedResult = result;
                owner.Status.SetMessage(result.DisplayText);
                FocusResultsRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Jumps to <paramref name="result"/>: selects its node in the tree (rebuilding
        /// the details pane), re-resolves the fresh container detail and field view models by
        /// model identity, flags the field as focused and announces the jump.</summary>
        private void Jump(FieldSearchResultViewModel? result)
        {
            if (result is null)
            {
                return;
            }
            SelectedResult = result;
            owner.SelectedNode = result.Node;
            var freshContainer = owner.SelectedNodeDetails
                ?.OfType<FieldContainerDataNodeDetailViewModel>()
                .FirstOrDefault(c => ReferenceEquals(c.ModelForTest, result.Container.ModelForTest));
            if (freshContainer is object)
            {
                var freshField = freshContainer.Fields
                    .FirstOrDefault(f => ReferenceEquals(f.ModelForTest, result.Field.ModelForTest));
                if (freshField is object)
                {
                    freshContainer.FocusedField = freshField;
                }
            }
            owner.Status.SetMessage($"Jumped to {result.DisplayText}");
            SearchJumped?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Recomputes the result list from the current query over the module tree:
        /// every field of every container detail (overlay fields flattened, like
        /// <c>FieldFinder</c>) of the currently selected kit, ranked and trimmed to
        /// <see cref="MaxResults"/>.</summary>
        private void UpdateResults()
        {
            var matches = new List<FieldSearchResultViewModel>();
            if (HasQuery)
            {
                int kitNumber = owner.SelectedNode?.KitNumber ?? -1;
                foreach (var candidate in CollectCandidates())
                {
                    if (kitNumber != -1 && candidate.Node.KitNumber != kitNumber)
                    {
                        // Search scope is the current kit (the tree walk covers the whole
                        // module in the Module Explorer; skip fields of other kits).
                        continue;
                    }
                    int score = ScoreDisplayText(candidate.DisplayText, query);
                    if (score > 0)
                    {
                        candidate.MatchScore = score;
                        matches.Add(candidate);
                    }
                }
            }
            Results = matches
                .OrderByDescending(m => m.MatchScore)
                .ThenBy(m => m.DisplayText, StringComparer.Ordinal)
                .Take(MaxResults)
                .ToList();
            SelectedResult = Results.FirstOrDefault();
        }

        /// <summary>
        /// Walks the whole module tree depth-first, yielding one result per editable field of
        /// every container detail, with overlay fields flattened (see also
        /// <c>FieldFinder.CollectAllFields</c> in the test project, which does the same walk
        /// over the model layer).
        /// </summary>
        private IEnumerable<FieldSearchResultViewModel> CollectCandidates()
        {
            var stack = new Stack<DataTreeNodeViewModel>();
            stack.Push(owner.Root[0]);
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                foreach (var detail in node.CreateDetails())
                {
                    if (detail is FieldContainerDataNodeDetailViewModel container)
                    {
                        foreach (var field in container.Fields)
                        {
                            yield return new FieldSearchResultViewModel(
                                node, container, field, $"{node.Title} — {container.Description} — {field.Description}");
                        }
                    }
                }
                // Push children in reverse so traversal order is depth-first, top to bottom.
                for (int i = node.Children.Count - 1; i >= 0; i--)
                {
                    stack.Push(node.Children[i]);
                }
            }
        }

        /// <summary>
        /// Scores <paramref name="query"/> against <paramref name="displayText"/>: every
        /// whitespace-separated token must match (exact word &gt; word prefix &gt; substring
        /// &gt; subsequence); the result is the sum of the per-token scores, or 0 when any
        /// token fails to match. All comparisons are case-insensitive and ordinal.
        /// </summary>
        internal static int ScoreDisplayText(string displayText, string query)
        {
            string target = displayText.ToLowerInvariant();
            int total = 0;
            foreach (var token in query.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int tokenScore = ScoreToken(token, target);
                if (tokenScore == 0)
                {
                    return 0;
                }
                total += tokenScore;
            }
            return total;
        }

        private static int ScoreToken(string token, string target)
        {
            var words = SplitWords(target);
            if (words.Contains(token))
            {
                return ExactWordScore;
            }
            if (words.Any(word => word.StartsWith(token, StringComparison.Ordinal)))
            {
                return WordPrefixScore;
            }
            if (target.Contains(token))
            {
                return SubstringScore;
            }
            return IsSubsequence(token, target) ? SubsequenceScore : 0;
        }

        /// <summary>Splits the target text into words (alphanumeric runs); the displayed text
        /// uses "—" separators, so punctuation is a word boundary.</summary>
        private static List<string> SplitWords(string text) =>
            text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(part => SplitPunctuation(part))
                .ToList();

        private static IEnumerable<string> SplitPunctuation(string part)
        {
            var current = new System.Text.StringBuilder();
            foreach (char c in part)
            {
                if (char.IsLetterOrDigit(c))
                {
                    current.Append(c);
                }
                else if (current.Length > 0)
                {
                    yield return current.ToString();
                    current.Clear();
                }
            }
            if (current.Length > 0)
            {
                yield return current.ToString();
            }
        }

        private static bool IsSubsequence(string token, string target)
        {
            int targetIndex = 0;
            foreach (char c in token)
            {
                targetIndex = target.IndexOf(c.ToString(), targetIndex, StringComparison.Ordinal);
                if (targetIndex < 0)
                {
                    return false;
                }
                targetIndex++;
            }
            return true;
        }
    }
}
