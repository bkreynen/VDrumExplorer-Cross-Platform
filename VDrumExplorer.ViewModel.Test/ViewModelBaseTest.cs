// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xunit;

namespace VDrumExplorer.ViewModel.Test
{
    public class ViewModelBaseTest
    {
        [Fact]
        public void SetProperty_WithSameValue_DoesNotFirePropertyChanged()
        {
            var vm = new TestViewModel();
            var handler = new PropertyChangedHandler();
            vm.PropertyChanged += handler.OnPropertyChanged;
            vm.TestProperty = vm.TestProperty; // Same value
            Assert.Empty(handler.ChangedProperties);
        }

        [Fact]
        public void SetProperty_WithDifferentValue_FiresPropertyChangedAndUpdatesField()
        {
            var vm = new TestViewModel { TestProperty = "old" };
            var handler = new PropertyChangedHandler();
            vm.PropertyChanged += handler.OnPropertyChanged;
            vm.TestProperty = "new";
            Assert.Equal("new", vm.TestProperty);
            Assert.Contains(nameof(TestViewModel.TestProperty), handler.ChangedProperties);
        }

        [Fact]
        public void SetProperty_WithInvalidValue_ThrowsArgumentException()
        {
            var vm = new TestViewModel();
            Assert.Throws<System.ArgumentException>(() => vm.TestPropertyWithValidation = "invalid");
        }

        [Fact]
        public void SetProperty_WithValidValue_FiresPropertyChanged()
        {
            var vm = new TestViewModel();
            var handler = new PropertyChangedHandler();
            vm.PropertyChanged += handler.OnPropertyChanged;
            vm.TestPropertyWithValidation = "valid";
            Assert.Contains(nameof(TestViewModel.TestPropertyWithValidation), handler.ChangedProperties);
        }

        [Fact]
        public void PropertyChanged_Add_TriggersOnPropertyChangedHasSubscribers()
        {
            var vm = new TestViewModel();
            var handler = new PropertyChangedHandler();
            vm.PropertyChanged += handler.OnPropertyChanged;
            Assert.True(vm.HasSubscribers);
        }

        [Fact]
        public void PropertyChanged_Remove_TriggersOnPropertyChangedHasNoSubscribers()
        {
            var vm = new TestViewModel();
            var handler = new PropertyChangedHandler();
            vm.PropertyChanged += handler.OnPropertyChanged;
            Assert.True(vm.HasSubscribers);
            vm.PropertyChanged -= handler.OnPropertyChanged;
            Assert.False(vm.HasSubscribers);
        }

        [Fact]
        public void RaisePropertyChanged_FiresEventWithCorrectPropertyName()
        {
            var vm = new TestViewModel();
            var handler = new PropertyChangedHandler();
            vm.PropertyChanged += handler.OnPropertyChanged;
            vm.RaiseTestPropertyChange();
            Assert.Contains("TestProperty", handler.ChangedProperties);
        }

        [Fact]
        public void ViewModelBase_TModel_Model_ReturnsConstructorValue()
        {
            var model = new TestModel();
            var vm = new TestModelViewModel(model);
            Assert.Same(model, vm.GetModel());
        }

        [Fact]
        public void ViewModelBase_TModel_PropertyChanged_SubscribesToModelNotifications()
        {
            var model = new TestModel();
            var vm = new TestModelViewModel(model);
            var handler = new PropertyChangedHandler();
            vm.PropertyChanged += handler.OnPropertyChanged;
            model.RaiseModelPropertyChange();
            Assert.Contains("ModelProperty", handler.ChangedProperties);
        }

        [Fact]
        public void ViewModelBase_TModel_PropertyChanged_UnsubscribesFromModelNotifications()
        {
            var model = new TestModel();
            var vm = new TestModelViewModel(model);
            var handler = new PropertyChangedHandler();
            vm.PropertyChanged += handler.OnPropertyChanged;
            model.RaiseModelPropertyChange();
            handler.ChangedProperties.Clear();
            vm.PropertyChanged -= handler.OnPropertyChanged;
            model.RaiseModelPropertyChange();
            Assert.Empty(handler.ChangedProperties);
        }

        private sealed class TestViewModel : ViewModelBase
        {
            private string? testProperty;
            public string? TestProperty
            {
                get => testProperty;
                set => SetProperty(ref testProperty, value);
            }

            private string? testPropertyWithValidation;
            public string? TestPropertyWithValidation
            {
                get => testPropertyWithValidation;
                set => SetProperty(ref testPropertyWithValidation, value, value != "invalid");
            }

            internal bool HasSubscribers { get; private set; }

            protected override void OnPropertyChangedHasSubscribers() => HasSubscribers = true;
            protected override void OnPropertyChangedHasNoSubscribers() => HasSubscribers = false;

            internal void RaiseTestPropertyChange() => RaisePropertyChanged("TestProperty");
        }

        private sealed class TestModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;

            internal void RaiseModelPropertyChange() =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ModelProperty"));
        }

        private sealed class TestModelViewModel : ViewModelBase<TestModel>
        {
            public TestModelViewModel(TestModel model) : base(model) { }

            internal TestModel GetModel() => Model;

            protected override void OnPropertyModelChanged(object sender, PropertyChangedEventArgs e) =>
                RaisePropertyChanged(e.PropertyName!);
        }

        private sealed class PropertyChangedHandler
        {
            internal List<string> ChangedProperties { get; } = new List<string>();

            public void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) =>
                ChangedProperties.Add(e.PropertyName!);
        }
    }
}
