// Copyright 2024 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using VDrumExplorer.Model.Device;

namespace VDrumExplorer.Model.Test.Device
{
    public class TransferProgressTest
    {
        [Test]
        public void Constructor_SetsProperties()
        {
            var progress = new TransferProgress(5, 10, "current");
            Assert.AreEqual(5, progress.Completed);
            Assert.AreEqual(10, progress.Total);
            Assert.AreEqual("current", progress.Current);
        }

        [Test]
        public void Constructor_ZeroCompletedZeroTotal()
        {
            var progress = new TransferProgress(0, 0, "start");
            Assert.AreEqual(0, progress.Completed);
            Assert.AreEqual(0, progress.Total);
            Assert.AreEqual("start", progress.Current);
        }

        [Test]
        public void Constructor_HalfComplete()
        {
            var progress = new TransferProgress(5, 10, "loading");
            Assert.AreEqual(5, progress.Completed);
            Assert.AreEqual(10, progress.Total);
            Assert.AreEqual("loading", progress.Current);
        }

        [Test]
        public void Constructor_FullyComplete()
        {
            var progress = new TransferProgress(10, 10, "complete");
            Assert.AreEqual(10, progress.Completed);
            Assert.AreEqual(10, progress.Total);
            Assert.AreEqual("complete", progress.Current);
        }

        [Test]
        public void Constructor_AcceptsNullCurrent()
        {
            var progress = new TransferProgress(0, 10, null!);
            Assert.AreEqual(0, progress.Completed);
            Assert.AreEqual(10, progress.Total);
            Assert.IsNull(progress.Current);
        }
    }
}
