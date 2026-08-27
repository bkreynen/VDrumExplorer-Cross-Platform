// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using VDrumExplorer.Model.Device;
using VDrumExplorer.ViewModel.Dialogs;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Dialogs
{
    public class DataTransferViewModelTest
    {
        [Fact]
        public async Task TransferAsync_SuccessfulTransfer_ReturnsResultAndSetsDialogResult()
        {
            var vm = new DataTransferViewModel<string>(
                NullLogger.Instance, "Test", "Progress: {0}",
                (progress, token) => Task.FromResult("done"));
            var result = await vm.TransferAsync();
            Assert.Equal("done", result);
            Assert.True(vm.DialogResult);
        }

        [Fact]
        public async Task TransferAsync_ReportsProgress()
        {
            // Use a single progress report to avoid race conditions with Progress<T>'s
            // async callback dispatching. The individual property-update tests below
            // (Completed_, Total_, CurrentItem_, ProgressDescription_UpdatedViaProgress)
            // already cover per-field progress reporting thoroughly.
            var vm = new DataTransferViewModel<string>(
                NullLogger.Instance, "Test", "Item: {0}",
                (progress, token) =>
                {
                    progress.Report(new TransferProgress(3, 3, "item3"));
                    return Task.FromResult("done");
                });
            await vm.TransferAsync();
            // Progress<T> posts callbacks asynchronously; allow them to complete.
            await Task.Delay(50);
            await WaitForAsync(() => vm.Total == 3);
            Assert.Equal(3, vm.Total);
            Assert.Equal(3, vm.Completed);
            Assert.Equal("item3", vm.CurrentItem);
            Assert.Equal("Item: item3", vm.ProgressDescription);
        }

        /// <summary>
        /// Polls until the given condition is true, or a timeout is reached.
        /// <see cref="Progress{T}"/> posts its callbacks asynchronously, so the view model
        /// state may not be updated immediately after
        /// <see cref="DataTransferViewModel{T}.TransferAsync"/> completes.
        /// </summary>
        private static async Task WaitForAsync(Func<bool> condition)
        {
            var deadline = DateTime.UtcNow.AddSeconds(2);
            while (!condition())
            {
                if (DateTime.UtcNow > deadline)
                {
                    return; // Let the assertions fail with a clear message.
                }
                await Task.Delay(20);
            }
        }

        [Fact]
        public async Task TransferAsync_CancelledToken_ThrowsOperationCanceledException()
        {
            var vm = new DataTransferViewModel<string>(
                NullLogger.Instance, "Test", "Progress: {0}",
                (progress, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    return Task.FromResult("done");
                });
            vm.CancelCommand.Execute(null);
            await Assert.ThrowsAsync<OperationCanceledException>(() => vm.TransferAsync());
            Assert.False(vm.DialogResult);
        }

        [Fact]
        public async Task TransferAsync_TransferThrows_SetsDialogResultFalseAndRethrows()
        {
            var vm = new DataTransferViewModel<string>(
                NullLogger.Instance, "Test", "Progress: {0}",
                (progress, token) => throw new InvalidOperationException("fail"));
            await Assert.ThrowsAsync<InvalidOperationException>(() => vm.TransferAsync());
            Assert.False(vm.DialogResult);
        }

        [Fact]
        public void CancelCommand_IsNotNull()
        {
            var vm = new DataTransferViewModel<string>(
                NullLogger.Instance, "Test", "Progress: {0}",
                (progress, token) => Task.FromResult("done"));
            Assert.NotNull(vm.CancelCommand);
        }

        [Fact]
        public async Task CancelCommand_WhenExecuted_CancelsTransfer()
        {
            var vm = new DataTransferViewModel<string>(
                NullLogger.Instance, "Test", "Progress: {0}",
                (progress, token) =>
                {
                    // Cancel before the transfer starts; the token should already be cancelled.
                    Assert.True(token.IsCancellationRequested);
                    token.ThrowIfCancellationRequested();
                    return Task.FromResult("done");
                });
            vm.CancelCommand.Execute(null!);
            await Assert.ThrowsAsync<System.OperationCanceledException>(() => vm.TransferAsync());
        }

        [Fact]
        public void Title_SetByConstructor()
        {
            var vm = new DataTransferViewModel<string>(
                NullLogger.Instance, "My Title", "Progress: {0}",
                (progress, token) => Task.FromResult("done"));
            Assert.Equal("My Title", vm.Title);
        }

        [Fact]
        public void DialogResult_SetFiresPropertyChanged()
        {
            var vm = new DataTransferViewModel("Test");
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);
            vm.DialogResult = true;
            Assert.Contains(nameof(vm.DialogResult), changedProperties);
        }

        [Fact]
        public async Task Completed_UpdatedViaProgress_FiresPropertyChanged()
        {
            var vm = new DataTransferViewModel<string>(
                NullLogger.Instance, "Test", "Progress: {0}",
                (progress, token) =>
                {
                    progress.Report(new TransferProgress(5, 10, "item"));
                    return Task.FromResult("done");
                });
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);
            await vm.TransferAsync();
            // Progress<T> posts callbacks asynchronously; allow them to complete.
            await Task.Delay(50);
            await WaitForAsync(() => vm.Completed == 5);
            Assert.Equal(5, vm.Completed);
            Assert.Contains(nameof(vm.Completed), changedProperties);
        }

        [Fact]
        public async Task Total_UpdatedViaProgress_FiresPropertyChanged()
        {
            var vm = new DataTransferViewModel<string>(
                NullLogger.Instance, "Test", "Progress: {0}",
                (progress, token) =>
                {
                    progress.Report(new TransferProgress(1, 10, "item"));
                    return Task.FromResult("done");
                });
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);
            await vm.TransferAsync();
            // Progress<T> posts callbacks asynchronously; allow them to complete.
            await Task.Delay(50);
            await WaitForAsync(() => vm.Total == 10);
            Assert.Equal(10, vm.Total);
            Assert.Contains(nameof(vm.Total), changedProperties);
        }

        [Fact]
        public async Task CurrentItem_UpdatedViaProgress_FiresPropertyChanged()
        {
            var vm = new DataTransferViewModel<string>(
                NullLogger.Instance, "Test", "Progress: {0}",
                (progress, token) =>
                {
                    progress.Report(new TransferProgress(1, 10, "test item"));
                    return Task.FromResult("done");
                });
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);
            await vm.TransferAsync();
            // Progress<T> posts callbacks asynchronously; allow them to complete.
            await Task.Delay(50);
            await WaitForAsync(() => vm.CurrentItem == "test item");
            Assert.Equal("test item", vm.CurrentItem);
            Assert.Contains(nameof(vm.CurrentItem), changedProperties);
        }

        [Fact]
        public async Task ProgressDescription_UpdatedViaProgress_FiresPropertyChanged()
        {
            var vm = new DataTransferViewModel<string>(
                NullLogger.Instance, "Test", "Item: {0}",
                (progress, token) =>
                {
                    progress.Report(new TransferProgress(1, 10, "thing"));
                    return Task.FromResult("done");
                });
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);
            await vm.TransferAsync();
            // Progress<T> posts callbacks asynchronously; allow them to complete.
            await Task.Delay(50);
            await WaitForAsync(() => vm.ProgressDescription == "Item: thing");
            Assert.Equal("Item: thing", vm.ProgressDescription);
            Assert.Contains(nameof(vm.ProgressDescription), changedProperties);
        }

        [Fact]
        public void DefaultValues_AreCorrect()
        {
            var vm = new DataTransferViewModel("Test");
            Assert.Equal(0, vm.Completed);
            Assert.Equal(0, vm.Total);
            Assert.Equal("", vm.CurrentItem);
            Assert.Equal("Progress", vm.ProgressDescription);
            Assert.Null(vm.DialogResult);
        }
    }
}
