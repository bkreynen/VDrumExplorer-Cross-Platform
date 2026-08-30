// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using NUnit.Framework;
using VDrumExplorer.Utility;

namespace VDrumExplorer.Utility.Test
{
    public class LazyTest
    {
        [Test]
        public void Create_ReturnsLazyUsingFactory()
        {
            var lazy = Lazy.Create(() => 42);
            Assert.IsFalse(lazy.IsValueCreated);
            Assert.AreEqual(42, lazy.Value);
            Assert.IsTrue(lazy.IsValueCreated);
        }

        [Test]
        public void Create_WithReferenceType_ReturnsLazyUsingFactory()
        {
            var lazy = Lazy.Create(() => "hello");
            Assert.IsFalse(lazy.IsValueCreated);
            Assert.AreEqual("hello", lazy.Value);
            Assert.IsTrue(lazy.IsValueCreated);
        }

        [Test]
        public void Create_ValueNotCreatedUntilAccessed()
        {
            int factoryCallCount = 0;
            var lazy = Lazy.Create(() => { factoryCallCount++; return 42; });
            Assert.AreEqual(0, factoryCallCount);
            Assert.IsFalse(lazy.IsValueCreated);
            var value = lazy.Value;
            Assert.AreEqual(1, factoryCallCount);
            Assert.AreEqual(42, value);
        }

        [Test]
        public void Create_FactoryCalledOnlyOnce()
        {
            int factoryCallCount = 0;
            var lazy = Lazy.Create(() => { factoryCallCount++; return new object(); });
            var first = lazy.Value;
            var second = lazy.Value;
            Assert.AreEqual(1, factoryCallCount);
            Assert.AreSame(first, second);
        }

        [Test]
        public void Initialize_SetsOutParameter()
        {
            Lazy.Initialize(out Lazy<int> field, () => 42);
            Assert.IsNotNull(field);
            Assert.IsFalse(field.IsValueCreated);
            Assert.AreEqual(42, field.Value);
            Assert.IsTrue(field.IsValueCreated);
        }

        [Test]
        public void Initialize_WithReferenceType_SetsOutParameter()
        {
            Lazy.Initialize(out Lazy<string> field, () => "hello");
            Assert.IsNotNull(field);
            Assert.IsFalse(field.IsValueCreated);
            Assert.AreEqual("hello", field.Value);
        }

        [Test]
        public void Initialize_ValueNotCreatedUntilAccessed()
        {
            int factoryCallCount = 0;
            Lazy.Initialize(out Lazy<int> field, () => { factoryCallCount++; return 42; });
            Assert.AreEqual(0, factoryCallCount);
            Assert.IsFalse(field.IsValueCreated);
            var value = field.Value;
            Assert.AreEqual(1, factoryCallCount);
            Assert.AreEqual(42, value);
        }

        [Test]
        public void Initialize_FactoryCalledOnlyOnce()
        {
            int factoryCallCount = 0;
            Lazy.Initialize(out Lazy<object> field, () => { factoryCallCount++; return new object(); });
            var first = field.Value;
            var second = field.Value;
            Assert.AreEqual(1, factoryCallCount);
            Assert.AreSame(first, second);
        }

        [Test]
        public void Create_FactoryThrows_CachesException()
        {
            int callCount = 0;
            var lazy = Lazy.Create<int>(() => { callCount++; throw new InvalidOperationException("boom"); });
            Assert.Throws<InvalidOperationException>(() => { var _ = lazy.Value; });
            Assert.Throws<InvalidOperationException>(() => { var _ = lazy.Value; });
            // System.Lazy<T> with ExecutionAndPublication caches the exception and does not re-invoke the factory.
            Assert.AreEqual(1, callCount, "Factory should be invoked only once; exception is cached");
        }

        [Test]
        public void Create_NullFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Lazy.Create<int>(null!));
        }

        [Test]
        public void Initialize_NullFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Lazy.Initialize(out Lazy<int> field, null!));
        }
    }
}
