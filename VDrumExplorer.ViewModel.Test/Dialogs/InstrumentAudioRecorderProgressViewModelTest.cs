// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Collections.Generic;
using System.ComponentModel;
using VDrumExplorer.ViewModel.Dialogs;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Dialogs
{
    public class InstrumentAudioRecorderProgressViewModelTest
    {
        [Fact]
        public void CurrentInstrumentRecording_DefaultIsProgress()
        {
            var vm = new InstrumentAudioRecorderProgressViewModel();
            Assert.Equal("Progress", vm.CurrentInstrumentRecording);
        }

        [Fact]
        public void CurrentInstrumentRecording_SetValue_UpdatesProperty()
        {
            var vm = new InstrumentAudioRecorderProgressViewModel();
            vm.CurrentInstrumentRecording = "Snare";
            Assert.Equal("Snare", vm.CurrentInstrumentRecording);
        }

        [Fact]
        public void CurrentInstrumentRecording_SetValue_FiresPropertyChanged()
        {
            var vm = new InstrumentAudioRecorderProgressViewModel();
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);
            vm.CurrentInstrumentRecording = "Kick";
            Assert.Contains(nameof(vm.CurrentInstrumentRecording), changedProperties);
        }

        [Fact]
        public void CurrentInstrumentRecording_SetToSameValue_DoesNotFirePropertyChanged()
        {
            var vm = new InstrumentAudioRecorderProgressViewModel();
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);
            vm.CurrentInstrumentRecording = vm.CurrentInstrumentRecording;
            Assert.Empty(changedProperties);
        }

        [Fact]
        public void CurrentInstrumentRecording_SetToNull_UpdatesProperty()
        {
            var vm = new InstrumentAudioRecorderProgressViewModel();
            vm.CurrentInstrumentRecording = null;
            Assert.Null(vm.CurrentInstrumentRecording);
        }

        [Fact]
        public void TotalInstruments_DefaultIsZero()
        {
            var vm = new InstrumentAudioRecorderProgressViewModel();
            Assert.Equal(0, vm.TotalInstruments);
        }

        [Fact]
        public void TotalInstruments_SetValue_UpdatesProperty()
        {
            var vm = new InstrumentAudioRecorderProgressViewModel();
            vm.TotalInstruments = 42;
            Assert.Equal(42, vm.TotalInstruments);
        }

        [Fact]
        public void TotalInstruments_SetValue_FiresPropertyChanged()
        {
            var vm = new InstrumentAudioRecorderProgressViewModel();
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);
            vm.TotalInstruments = 10;
            Assert.Contains(nameof(vm.TotalInstruments), changedProperties);
        }

        [Fact]
        public void TotalInstruments_SetToSameValue_DoesNotFirePropertyChanged()
        {
            var vm = new InstrumentAudioRecorderProgressViewModel();
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);
            vm.TotalInstruments = 0; // Same as default
            Assert.Empty(changedProperties);
        }

        [Fact]
        public void CompletedInstruments_DefaultIsZero()
        {
            var vm = new InstrumentAudioRecorderProgressViewModel();
            Assert.Equal(0, vm.CompletedInstruments);
        }

        [Fact]
        public void CompletedInstruments_SetValue_UpdatesProperty()
        {
            var vm = new InstrumentAudioRecorderProgressViewModel();
            vm.CompletedInstruments = 7;
            Assert.Equal(7, vm.CompletedInstruments);
        }

        [Fact]
        public void CompletedInstruments_SetValue_FiresPropertyChanged()
        {
            var vm = new InstrumentAudioRecorderProgressViewModel();
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);
            vm.CompletedInstruments = 5;
            Assert.Contains(nameof(vm.CompletedInstruments), changedProperties);
        }

        [Fact]
        public void CompletedInstruments_SetToSameValue_DoesNotFirePropertyChanged()
        {
            var vm = new InstrumentAudioRecorderProgressViewModel();
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);
            vm.CompletedInstruments = 0; // Same as default
            Assert.Empty(changedProperties);
        }
    }
}
