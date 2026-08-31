// Copyright 2023 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Linq;
using VDrumExplorer.Model.Data;
using VDrumExplorer.Model.Data.Fields;
using VDrumExplorer.Model.Schema.Fields;
using VDrumExplorer.Model.Schema.Physical;

namespace VDrumExplorer.Model.Test.Helpers
{
    /// <summary>
    /// Shared helpers for locating schema/data fields in tests. Consolidates the per-file
    /// inline <c>Root.DescendantsAndSelf().OfType&lt;T&gt;().First()</c> lookups that were
    /// duplicated across BooleanDataFieldTest, InstrumentDataFieldTest, Engine and related
    /// files, plus the KitMfx[1] overlay lookup used by OverlayDataFieldTest.
    /// </summary>
    internal static class ModelTestHelpers
    {
        /// <summary>
        /// Finds the first <see cref="BooleanField"/> in the module's physical schema.
        /// </summary>
        internal static BooleanField FindBooleanField(Module module) =>
            module.Schema.PhysicalRoot.DescendantsAndSelf()
                .OfType<FieldContainer>()
                .SelectMany(fc => fc.Fields)
                .OfType<BooleanField>()
                .First();

        /// <summary>
        /// Finds the first <see cref="EnumField"/> in the module's physical schema.
        /// </summary>
        internal static EnumField FindEnumField(Module module) =>
            module.Schema.PhysicalRoot.DescendantsAndSelf()
                .OfType<FieldContainer>()
                .SelectMany(fc => fc.Fields)
                .OfType<EnumField>()
                .First();

        /// <summary>
        /// Finds the first <see cref="EnumField"/> satisfying a predicate.
        /// </summary>
        internal static EnumField FindEnumField(Module module, System.Func<EnumField, bool> predicate) =>
            module.Schema.PhysicalRoot.DescendantsAndSelf()
                .OfType<FieldContainer>()
                .SelectMany(fc => fc.Fields)
                .OfType<EnumField>()
                .First(predicate);

        /// <summary>
        /// Finds the first <see cref="InstrumentField"/> in the module's physical schema.
        /// </summary>
        internal static InstrumentField FindInstrumentField(Module module) =>
            module.Schema.PhysicalRoot.DescendantsAndSelf()
                .OfType<FieldContainer>()
                .SelectMany(fc => fc.Fields)
                .OfType<InstrumentField>()
                .First();

        /// <summary>
        /// Finds the first <see cref="NumericField"/> in the module's physical schema.
        /// </summary>
        internal static NumericField FindNumericField(Module module) =>
            module.Schema.PhysicalRoot.DescendantsAndSelf()
                .OfType<FieldContainer>()
                .SelectMany(fc => fc.Fields)
                .OfType<NumericField>()
                .First();

        /// <summary>
        /// Returns the <see cref="OverlayField"/> for KitMfx[1]/Parameters and its switch
        /// <see cref="EnumField"/> KitMfx[1]/Type – the canonical overlay fixture used by
        /// OverlayDataFieldTest and TempoDataFieldTest.
        /// </summary>
        internal static (OverlayField Overlay, EnumField Switch) FindOverlayKitMfx1(Module module)
        {
            var container = module.Schema.Kit1Root.Container.ResolveContainer("KitMfx[1]");
            return ((OverlayField)container.ResolveField("Parameters"), (EnumField)container.ResolveField("Type"));
        }

        /// <summary>
        /// Resolves a named overlay pair from any container path (e.g. "KitMfx[1]", "Type", "Parameters").
        /// </summary>
        internal static (OverlayField Overlay, EnumField Switch) FindOverlay(Module module, string containerPath, string switchFieldName, string overlayFieldName)
        {
            var container = module.Schema.Kit1Root.Container.ResolveContainer(containerPath);
            return ((OverlayField)container.ResolveField(overlayFieldName), (EnumField)container.ResolveField(switchFieldName));
        }

        /// <summary>
        /// Loads a fresh TD-27 module – thin wrapper over <see cref="TestData.LoadTD27"/> for
        /// callers that prefer the helpers namespace. Keeps CreateModule/LoadTD27 deduplicated.
        /// </summary>
        internal static Module LoadTD27() => TestData.LoadTD27();

        /// <summary>
        /// Gets the data field for a schema field from the module's data.
        /// </summary>
        internal static TData GetDataField<TData>(Module module, IField field)
            where TData : IDataField => (TData)module.Data.GetDataField(field);

        /// <summary>
        /// Convenience: find the BooleanDataField in the module.
        /// </summary>
        internal static BooleanDataField FindBooleanDataField(Module module)
        {
            var schemaField = FindBooleanField(module);
            return (BooleanDataField)module.Data.GetDataField(schemaField);
        }

        internal static InstrumentDataField FindInstrumentDataField(Module module)
        {
            var schemaField = FindInstrumentField(module);
            return (InstrumentDataField)module.Data.GetDataField(schemaField);
        }

        internal static EnumDataField FindEnumDataField(Module module)
        {
            var schemaField = FindEnumField(module);
            return (EnumDataField)module.Data.GetDataField(schemaField);
        }

        internal static NumericDataField FindNumericDataField(Module module)
        {
            var schemaField = FindNumericField(module);
            return (NumericDataField)module.Data.GetDataField(schemaField);
        }

        /// <summary>
        /// Returns the <c>/Current</c> <see cref="FieldContainer"/> – the first field container
        /// in TD-27's physical schema (holds CurrentKit). Pinned by absolute path instead of
        /// <c>DescendantsAndSelf().OfType&lt;FieldContainer&gt;().First()</c> so refactors that
        /// change traversal order do not silently pick a different fixture.
        /// </summary>
        internal static FieldContainer FindCurrentFieldContainer(Module module) =>
            (FieldContainer)module.Schema.PhysicalRoot.ResolveContainer("/Current");

        /// <summary>
        /// Resolves a <see cref="FieldContainer"/> by absolute path, e.g. <c>"/Current"</c> or
        /// <c>"/Setup/Output"</c>. Useful for pinning <c>ContainerBaseTest</c> / <c>FieldContainerTest</c>
        /// fixtures to a known container instead of <c>First()</c>.
        /// </summary>
        internal static FieldContainer ResolveFieldContainer(Module module, string absolutePath) =>
            (FieldContainer)module.Schema.PhysicalRoot.ResolveContainer(absolutePath);

        /// <summary>
        /// Returns the <see cref="IContainer"/> at <paramref name="absolutePath"/> – thin wrapper
        /// over <see cref="ContainerBase.ResolveContainer(string)"/> for tests that want a named
        /// container rather than an ordinal lookup.
        /// </summary>
        internal static IContainer ResolveContainer(Module module, string absolutePath) =>
            module.Schema.PhysicalRoot.ResolveContainer(absolutePath);
    }
}
