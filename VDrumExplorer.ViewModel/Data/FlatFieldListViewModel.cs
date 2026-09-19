// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Collections.Generic;
using System.Linq;

namespace VDrumExplorer.ViewModel.Data
{
    /// <summary>
    /// One section of the flat field list (Phase 1 task 4, docs/accessibility.md §4): a
    /// single field-container detail of one tree node, announced as
    /// "{node} — {container}" (identifier-first, docs/accessibility.md §2) and rendered
    /// with heading semantics in the view.
    /// <para>
    /// The section exposes the <see cref="Container"/> view model it was built from. For the
    /// section of the currently selected tree node this is the <em>same</em> container view
    /// model instance the details pane displays, and <see cref="Fields"/> therefore binds the
    /// same <see cref="DataFieldViewModel"/> instances as the details pane: one editing path,
    /// zero divergence. For sections of other nodes the containers come from a fresh
    /// <c>CreateDetails()</c> walk (like <see cref="FieldSearchViewModel"/>), which is fine —
    /// those containers are displayed nowhere else.
    /// </para>
    /// </summary>
    public class FlatFieldSectionViewModel : ViewModelBase
    {
        /// <summary>The tree node owning the container ("Kick", "Kit 1: Premium Wood", …).</summary>
        public DataTreeNodeViewModel Node { get; }

        /// <summary>
        /// The field-container detail view model ("Main instrument", "Kit common", …).
        /// For the selected node's section this is the same instance the details pane shows.
        /// </summary>
        public FieldContainerDataNodeDetailViewModel Container { get; }

        /// <summary>The announced/rendered section heading: "{node} — {container}".</summary>
        public string Heading => $"{Node.Title} — {Container.Description}";

        /// <summary>
        /// The fields of the container in schema order (overlay fields flattened). Bound
        /// live from the container, so overlay-driven regeneration is reflected here too.
        /// </summary>
        public IReadOnlyList<DataFieldViewModel> Fields => Container.Fields;

        /// <summary>
        /// Prefix for the schema-driven editor AutomationIds of this section's rows
        /// (docs/accessibility.md §3.1: the flat area uses the <c>flat-fields</c> area).
        /// The shared per-type editor templates in DataExplorer.axaml resolve this from
        /// the nearest ItemsControl ancestor, so the same templates produce
        /// <c>data-explorer.details.*</c> IDs in the details pane
        /// (see <see cref="FieldContainerDataNodeDetailViewModel.IdPrefix"/>) and
        /// <c>data-explorer.flat-fields.*</c> IDs here.
        /// </summary>
        public string IdPrefix => IdPrefixConst;

        internal const string IdPrefixConst = "data-explorer.flat-fields";

        internal FlatFieldSectionViewModel(DataTreeNodeViewModel node, FieldContainerDataNodeDetailViewModel container)
        {
            Node = node;
            Container = container;
        }
    }

    /// <summary>
    /// Flat/linear field-list mode for a data explorer window (Phase 1 task 4): an
    /// alternative traversal of the <em>same</em> ViewModel state as the tree + details
    /// pane — all fields of the current kit as a flat list ordered by schema-tree section,
    /// each row bound to the same <see cref="DataFieldViewModel"/> instances the details
    /// pane uses. Screen-reader users navigate it with standard reading keys and section
    /// headings; sighted users get a spreadsheet-like view. Toggled per session (not
    /// persisted); off by default so the default layout — and the visual baselines — are
    /// untouched.
    /// <para>
    /// Scope: the whole module for the <see cref="KitExplorerViewModel"/> (its tree <em>is</em>
    /// the kit); in the <see cref="ModuleExplorerViewModel"/> the fields of the kit containing
    /// the currently selected node (mirroring <see cref="FieldSearchViewModel"/>; if the
    /// selection has no kit — e.g. the module root — the whole module is listed). Read-only
    /// list details are excluded; the flat list is about editable fields.
    /// </para>
    /// </summary>
    public class FlatFieldListViewModel : ViewModelBase
    {
        private readonly DataExplorerViewModel owner;

        public FlatFieldListViewModel(DataExplorerViewModel owner)
        {
            this.owner = owner;
            ToggleFlatModeCommand = new DelegateCommand(ToggleFlatMode, true);
        }

        /// <summary>Toggles flat field-list mode (session-only; not persisted).</summary>
        public DelegateCommand ToggleFlatModeCommand { get; }

        private bool isFlatModeEnabled;
        /// <summary>
        /// Whether the flat field list is shown instead of the tree + details pane.
        /// False by default: the default layout — and the visual baselines — are unchanged.
        /// </summary>
        public bool IsFlatModeEnabled
        {
            get => isFlatModeEnabled;
            private set
            {
                if (SetProperty(ref isFlatModeEnabled, value))
                {
                    RaisePropertyChanged(nameof(IsTreeModeVisible));
                    Sections = value ? BuildSections() : Array.Empty<FlatFieldSectionViewModel>();
                }
            }
        }

        /// <summary>Whether the tree + details pane is shown (i.e. flat mode is off).</summary>
        public bool IsTreeModeVisible => !isFlatModeEnabled;

        private IReadOnlyList<FlatFieldSectionViewModel> sections = Array.Empty<FlatFieldSectionViewModel>();
        /// <summary>
        /// The flat list rows: one section per field container of the current kit scope, in
        /// schema-tree order, each carrying its fields in schema order. Empty while flat
        /// mode is off (nothing is built, so the hidden flat list adds nothing to the
        /// default accessibility scan or render).
        /// </summary>
        public IReadOnlyList<FlatFieldSectionViewModel> Sections
        {
            get => sections;
            private set => SetProperty(ref sections, value);
        }

        /// <summary>Total number of field rows across all sections (for announcements/tests).</summary>
        public int FieldCount => Sections.Sum(s => s.Fields.Count);

        private void ToggleFlatMode()
        {
            IsFlatModeEnabled = !IsFlatModeEnabled;
            // Announce the mode change on the status line (docs/accessibility.md §5:
            // silence is a bug; the mode switch is an operation with an outcome).
            owner.Status.SetMessage(IsFlatModeEnabled
                ? $"Flat field list on: {Sections.Count} sections, {FieldCount} fields"
                : "Flat field list off");
        }

        /// <summary>
        /// Builds the sections by walking the module tree depth-first (same traversal as
        /// <see cref="FieldSearchViewModel.CollectCandidates"/>), restricted to the current
        /// kit scope, yielding one section per field container in schema order with overlay
        /// fields flattened (like <c>FieldFinder</c>, via the container's own
        /// <c>Fields</c> regeneration).
        /// <para>
        /// For the currently selected node, the containers of the details pane
        /// (<see cref="DataExplorerViewModel.SelectedNodeDetails"/>) are reused so the flat
        /// list binds the <em>same</em> container and field view model instances as the
        /// details pane; other nodes get fresh detail view models (they are displayed
        /// nowhere else, so there is no divergence).
        /// </para>
        /// </summary>
        private IReadOnlyList<FlatFieldSectionViewModel> BuildSections()
        {
            // Scope: the whole module for a Kit Explorer; in the Module Explorer the kit
            // containing the selected node (or the whole module when the selection is not
            // inside a kit), mirroring the search scope of FieldSearchViewModel.
            int? scopeKit = owner.SelectedNode?.KitNumber;
            var sections = new List<FlatFieldSectionViewModel>();
            var stack = new Stack<DataTreeNodeViewModel>();
            stack.Push(owner.Root[0]);
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                bool inScope = !(scopeKit is int kit && node.KitNumber != kit);
                if (inScope)
                {
                    foreach (var detail in GetDetails(node))
                    {
                        if (detail is FieldContainerDataNodeDetailViewModel container)
                        {
                            sections.Add(new FlatFieldSectionViewModel(node, container));
                        }
                    }
                }
                // Always push children (in reverse so traversal order is depth-first, top
                // to bottom) even when this node is out of scope: pruning here would cut
                // off whole subtrees (e.g. the kit-less module root hides every kit).
                for (int i = node.Children.Count - 1; i >= 0; i--)
                {
                    stack.Push(node.Children[i]);
                }
            }
            return sections;
        }

        /// <summary>
        /// The detail view models for <paramref name="node"/>: the details-pane instances
        /// when the node is the current selection (zero divergence with the details pane),
        /// otherwise a fresh set (as <see cref="FieldSearchViewModel"/> does).
        /// </summary>
        private IReadOnlyList<IDataNodeDetailViewModel> GetDetails(DataTreeNodeViewModel node) =>
            ReferenceEquals(node, owner.SelectedNode) && owner.SelectedNodeDetails is object
                ? owner.SelectedNodeDetails
                : node.CreateDetails();
    }
}
