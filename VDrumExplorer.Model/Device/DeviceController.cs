// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VDrumExplorer.Model.Midi;
using VDrumExplorer.Model.Data;
using VDrumExplorer.Model.Data.Fields;
using VDrumExplorer.Model.Data.Logical;
using VDrumExplorer.Model.Schema.Fields;
using VDrumExplorer.Model.Schema.Logical;
using VDrumExplorer.Model.Schema.Physical;
using Microsoft.Extensions.Logging;

namespace VDrumExplorer.Model.Device
{
    /// <summary>
    /// "Normal" implementation of <see cref="IDeviceController"/>, using a <see cref="RolandMidiClient"/>.
    /// </summary>
    public class DeviceController : IDeviceController
    {
        /// <summary>
        /// How long we wait after each write operation for the module to catch up.
        /// TODO: Test this further.
        /// </summary>
        private static readonly TimeSpan WriteDelay = TimeSpan.FromMilliseconds(40);

        /// <summary>
        /// The address for the "current kit" field. Currently the same for all schemas,
        /// but we always refer to it via this constant so we can adjust if necessary.
        /// </summary
        private static readonly ModuleAddress CurrentKitAddress = ModuleAddress.FromLogicalValue(0);

        /// <summary>
        /// How long we're prepared to wait for a single data segment to load.
        /// </summary>
        private readonly TimeSpan loadSegmentTimeout;

        /// <summary>
        /// The underlying client.
        /// </summary>
        private readonly RolandMidiClient client;

        /// <summary>
        /// Logger to use when reporting validation errors.
        /// </summary>
        private readonly ILogger logger;

        public ModuleSchema Schema { get; }

        public string InputName => client.InputName;

        public string OutputName => client.OutputName;

        public DeviceController(RolandMidiClient client, ILogger logger) : this(client, logger, TimeSpan.FromSeconds(1))
        {
        }

        private DeviceController(RolandMidiClient client, ILogger logger, TimeSpan loadSegmentTimeout) =>
            (this.client, this.logger, this.loadSegmentTimeout, Schema) =
            (client, logger, loadSegmentTimeout, ModuleSchema.KnownSchemas[client.Identifier].Value);

        public async Task<int> GetCurrentKitAsync(CancellationToken cancellationToken)
        {
            var segment = await LoadSegment(CurrentKitAddress, 1, cancellationToken);
            return segment.ReadInt32(ModuleOffset.Zero, NumericCodec.Range8) + 1;
        }

        public Task SetCurrentKitAsync(int kit, CancellationToken cancellationToken)
        {
            // Send a MIDI Program Change to switch the active kit on the device.
            // The TD-17 responds to Program Change on its configured MIDI channel (typically channel 10).
            // Program numbers are 0-indexed: kit 1 = program 0, kit 100 = program 99.
            // Note: SysEx Data Set to the "Current" register (address 0x00_00_00_00) only updates the
            // stored value but does not trigger the actual kit switch on the TD-17.
            client.SendProgramChange(10, kit - 1);
            return Task.CompletedTask;
        }

        public async Task<Kit> LoadKitAsync(int kit, IProgress<TransferProgress>? progressHandler, CancellationToken cancellationToken)
        {
            var kitRoot = Schema.GetKitRoot(kit);
            var snapshot = await LoadDescendantsAsync(kitRoot, progressHandler, cancellationToken);
            snapshot = snapshot.Relocated(kitRoot, Schema.Kit1Root);
            return Kit.FromSnapshot(Schema, snapshot, kit, logger);
        }

        public async Task<Module> LoadModuleAsync(IProgress<TransferProgress>? progressHandler, CancellationToken cancellationToken)
        {
            var snapshot = await LoadDescendantsAsync(Schema.LogicalRoot, progressHandler, cancellationToken);
            return Module.FromSnapshot(Schema, snapshot, logger);
        }

        public void PlayNote(int channel, int note, int velocity) => client.PlayNote(channel, note, velocity);

        public void Silence(int channel) => client.Silence(channel);

        public Task SaveDescendants(DataTreeNode node, ModuleAddress? targetAddress, IProgress<TransferProgress>? progressHandler, CancellationToken cancellationToken)
        {
            var containers = node.SchemaNode.DescendantFieldContainers().OrderBy(fc => fc.Address).ToList();
            var snapshot = node.Data.CreatePartialSnapshot(node.SchemaNode);
            int offset = 0;
            if (targetAddress is ModuleAddress target)
            {
                var source = node.SchemaNode.Container.Address;
                snapshot = snapshot.Relocated(source, target);
                offset = target.LogicalValue - source.LogicalValue;
            }
            var addressPaths = containers.ToDictionary(c => c.Address.PlusLogicalOffset(offset), c => c.Path);
            return SaveSnapshot(snapshot, addressPaths, progressHandler, cancellationToken);
        }

        public async Task LoadDescendants(DataTreeNode node, ModuleAddress? targetAddress, IProgress<TransferProgress>? progressHandler, CancellationToken cancellationToken)
        {
            var snapshot = await LoadDescendantsAsync(node.SchemaNode, progressHandler, cancellationToken);
            if (targetAddress is ModuleAddress target)
            {
                snapshot = snapshot.Relocated(node.SchemaNode.Container.Address, target);
            }
            node.Data.LoadPartialSnapshot(snapshot, logger);
        }

        private async Task<ModuleDataSnapshot> LoadDescendantsAsync(TreeNode root, IProgress<TransferProgress>? progressHandler, CancellationToken cancellationToken)
        {
            // Sort containers by address so we can find adjacent ones.
            var containers = root.DescendantFieldContainers().OrderBy(c => c.Address).ToList();
            var snapshot = new ModuleDataSnapshot();
            int completed = 0;

            // Batch adjacent containers into single requests to reduce round-trips.
            // This is conservative: we only batch containers that are contiguous in address space
            // and whose combined size fits within the 383-byte (0x17f) maximum request size.
            var batches = CreateBatches(containers);
            foreach (var batch in batches)
            {
                progressHandler?.Report(new TransferProgress(completed, containers.Count, batch.Path));

                if (batch.Containers.Count == 1)
                {
                    // Single container — load directly (same as before).
                    var container = batch.Containers[0];
                    var segment = await LoadSegment(container.Address, container.Size, cancellationToken);
                    snapshot.Add(segment);
                }
                else
                {
                    // Multiple containers — load as one request and split the result.
                    var firstContainer = batch.Containers[0];
                    var segment = await LoadSegment(firstContainer.Address, batch.TotalSize, cancellationToken);
                    var data = segment.CopyData();
                    int offset = 0;
                    foreach (var container in batch.Containers)
                    {
                        var containerData = new byte[container.Size];
                        Array.Copy(data, offset, containerData, 0, container.Size);
                        snapshot.Add(new DataSegment(container.Address, containerData));
                        offset += container.Size;
                    }
                }
                completed += batch.Containers.Count;
            }
            progressHandler?.Report(new TransferProgress(containers.Count, containers.Count, "complete"));
            return snapshot;
        }

        /// <summary>
        /// Groups adjacent field containers into batches for more efficient loading.
        /// Only containers that are contiguous in address space (no gaps) and whose
        /// combined size fits within the 383-byte maximum request size are batched.
        /// </summary>
        private static List<ContainerBatch> CreateBatches(List<FieldContainer> containers)
        {
            const int maxBatchSize = 0x17f; // 383 bytes — the maximum single request size.
            var batches = new List<ContainerBatch>();
            var currentBatch = new List<FieldContainer>();
            int currentBatchSize = 0;

            foreach (var container in containers)
            {
                int containerSize = container.Size;

                if (currentBatch.Count == 0)
                {
                    // Start a new batch.
                    currentBatch.Add(container);
                    currentBatchSize = containerSize;
                }
                else
                {
                    // Check if this container is adjacent to the previous one.
                    var previous = currentBatch[currentBatch.Count - 1];
                    int previousEnd = previous.Address.LogicalValue + previous.Size;
                    int containerStart = container.Address.LogicalValue;

                    bool isAdjacent = containerStart == previousEnd;
                    bool fitsInBatch = currentBatchSize + containerSize <= maxBatchSize;

                    if (isAdjacent && fitsInBatch)
                    {
                        // Add to current batch.
                        currentBatch.Add(container);
                        currentBatchSize += containerSize;
                    }
                    else
                    {
                        // Finalize current batch and start a new one.
                        batches.Add(new ContainerBatch(currentBatch, currentBatchSize));
                        currentBatch = new List<FieldContainer> { container };
                        currentBatchSize = containerSize;
                    }
                }
            }

            if (currentBatch.Count > 0)
            {
                batches.Add(new ContainerBatch(currentBatch, currentBatchSize));
            }

            return batches;
        }

        /// <summary>
        /// A batch of adjacent field containers loaded in a single request.
        /// </summary>
        private sealed class ContainerBatch
        {
            public IReadOnlyList<FieldContainer> Containers { get; }
            public int TotalSize { get; }
            public string Path { get; }

            public ContainerBatch(IReadOnlyList<FieldContainer> containers, int totalSize)
            {
                Containers = containers;
                TotalSize = totalSize;
                Path = containers.Count == 1
                    ? containers[0].Path
                    : $"{containers[0].Path} (+{containers.Count - 1} more)";
            }
        }

        public async Task SetInstrumentAsync(int kit, int trigger, Instrument instrument, CancellationToken cancellationToken)
        {
            var field = Schema.GetMainInstrumentField(kit, trigger);
            var segment = await LoadSegment(field.Parent!.Address, field.Parent!.Size, cancellationToken);
            var dataField = new InstrumentDataField(field, Schema);
            dataField.Instrument = instrument;
            dataField.Save(segment);
            await SaveSegment(segment, cancellationToken);
        }

        // Internal to allow simple loading of segments in DumpDeviceSegmentCommand.
        internal async Task<DataSegment> LoadSegment(ModuleAddress address, int size, CancellationToken cancellationToken)
        {
            var timerToken = new CancellationTokenSource(loadSegmentTimeout).Token;
            var effectiveToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timerToken).Token;
            try
            {
                var data = await client.RequestDataAsync(address.DisplayValue, size, effectiveToken);
                return new DataSegment(address, data);
            }
            catch (OperationCanceledException) when (timerToken.IsCancellationRequested)
            {
                throw new TimeoutException("Timed out waiting for data from device");
            }
        }

        public async Task<string> LoadKitNameAsync(int kit, CancellationToken cancellationToken)
        {
            var kitRoot = Schema.GetKitRoot(kit);
            var rootContainer = kitRoot.Container;
            var nameField = rootContainer.ResolveField(Schema.KitNamePath);
            var subNameField = Schema.KitSubNamePath is string subNamePath ? rootContainer.ResolveField(subNamePath) : null;

            var containersToLoad = new[] { nameField.Parent, subNameField?.Parent }.Where(c => c is object).Select(c => c!).Distinct();
            var snapshot = new ModuleDataSnapshot();
            foreach (var container in containersToLoad)
            {
                var segment = await LoadSegment(container.Address, container.Size, cancellationToken);
                snapshot.Add(segment);
            }
            var data = ModuleData.FromLogicalRootNode(kitRoot);
            data.LoadPartialSnapshot(snapshot, logger);

            return Kit.GetKitName(data, kitRoot);
        }

        // Assumption: the list of containers is exactly the same as the segments in the snapshot.
        // We just use this so that we can report the field path instead of the address.
        // (An alternative would be a map from address to path...)
        private async Task SaveSnapshot(ModuleDataSnapshot snapshot, Dictionary<ModuleAddress, string> addressPaths, IProgress<TransferProgress>? progressHandler, CancellationToken cancellationToken)
        {
            int completed = 0;
            foreach (var segment in snapshot.Segments)
            {
                progressHandler?.Report(new TransferProgress(completed, snapshot.SegmentCount, addressPaths[segment.Address]));
                await SaveSegment(segment, cancellationToken);
                completed++;
            }
            progressHandler?.Report(new TransferProgress(snapshot.SegmentCount, snapshot.SegmentCount, "complete"));
        }

        // Internal to allow simple changing of MFX in CheckMfxDefaultsCommand.
        internal async Task SaveSegment(DataSegment segment, CancellationToken cancellationToken)
        {
            client.SendData(segment.Address.DisplayValue, segment.CopyData());
            await Task.Delay(WriteDelay, cancellationToken);
        }

        public void Dispose()
        {
            try
            {
                client.Dispose();
            }
            catch
            {
                // It's rare to get errors when disposing of the client, but it can happen - and it's not worth reporting.
            }
        }
    }
}
