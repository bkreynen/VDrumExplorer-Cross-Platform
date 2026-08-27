// Copyright 2023 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using VDrumExplorer.Model.Schema.Json;

namespace VDrumExplorer.Model.Test.Schema.Json;

internal class PathUtilitiesTest
{
    // PathUtilities is internal, but accessible via InternalsVisibleTo.

    [Test]
    public void AppendPath_NullParent_ReturnsRootPath()
    {
        // The root node has a null parent path, and AppendPath returns "/" for it.
        Assert.AreEqual("/", PathUtilities.AppendPath(null, "Root"));
    }

    [Test]
    public void AppendPath_RootParent_ReturnsChildPath()
    {
        // Direct child of root: parent path is "/", result is "/child".
        Assert.AreEqual("/child", PathUtilities.AppendPath("/", "child"));
    }

    [Test]
    public void AppendPath_NonRootParent_ReturnsNestedPath()
    {
        Assert.AreEqual("/parent/child", PathUtilities.AppendPath("/parent", "child"));
    }

    [Test]
    public void AppendPath_DeeplyNestedParent_ReturnsNestedPath()
    {
        Assert.AreEqual("/parent/sub/child", PathUtilities.AppendPath("/parent/sub", "child"));
    }

    [Test]
    public void AppendPath_DeeplyNestedParent_MultipleLevels()
    {
        Assert.AreEqual("/a/b/c/d", PathUtilities.AppendPath("/a/b/c", "d"));
    }
}
