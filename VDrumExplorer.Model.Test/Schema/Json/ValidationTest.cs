// Copyright 2023 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using VDrumExplorer.Model.Schema.Json;

namespace VDrumExplorer.Model.Test.Schema.Json;

internal class ValidationTest
{
    // Validation is internal, but accessible via InternalsVisibleTo.

    // --- ValidateNull (throws when value is NOT null) ---

    [Test]
    public void ValidateNull_NonNullValue_Throws()
    {
        var ex = Assert.Throws<ModuleSchemaException>(() =>
            Validation.ValidateNull("not null", "field", "otherField"));
        Assert.AreEqual("field must not be specified because of otherField", ex!.Message);
    }

    [Test]
    public void ValidateNull_NullValue_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => Validation.ValidateNull<string>(null, "field", "otherField"));
    }

    // --- ValidateNotNull (class overload) ---

    [Test]
    public void ValidateNotNull_Class_Null_Throws()
    {
        var ex = Assert.Throws<ModuleSchemaException>(() =>
            Validation.ValidateNotNull<string>(null, "field"));
        Assert.AreEqual("field must be specified", ex!.Message);
    }

    [Test]
    public void ValidateNotNull_Class_NonNull_ReturnsValue()
    {
        var result = Validation.ValidateNotNull("value", "field");
        Assert.AreEqual("value", result);
    }

    // --- ValidateNotNull (struct overload) ---

    [Test]
    public void ValidateNotNull_Struct_Null_Throws()
    {
        var ex = Assert.Throws<ModuleSchemaException>(() =>
            Validation.ValidateNotNull<int>(null, "field"));
        Assert.AreEqual("field must be specified", ex!.Message);
    }

    [Test]
    public void ValidateNotNull_Struct_NonNull_ReturnsValue()
    {
        var result = Validation.ValidateNotNull<int>(42, "field");
        Assert.AreEqual(42, result);
    }

    // --- Validate(bool, string) ---

    [Test]
    public void Validate_BoolString_False_Throws()
    {
        var ex = Assert.Throws<ModuleSchemaException>(() =>
            Validation.Validate(false, "Something is wrong"));
        Assert.AreEqual("Something is wrong", ex!.Message);
    }

    [Test]
    public void Validate_BoolString_True_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => Validation.Validate(true, "Something is wrong"));
    }

    // --- Validate(bool, string, object) ---

    [Test]
    public void Validate_BoolStringOneArg_False_ThrowsWithFormattedMessage()
    {
        var ex = Assert.Throws<ModuleSchemaException>(() =>
            Validation.Validate(false, "Value {0} is invalid", 42));
        Assert.AreEqual("Value 42 is invalid", ex!.Message);
    }

    [Test]
    public void Validate_BoolStringOneArg_True_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => Validation.Validate(true, "Value {0} is invalid", 42));
    }

    // --- Validate(bool, string, object, object) ---

    [Test]
    public void Validate_BoolStringTwoArgs_False_ThrowsWithFormattedMessage()
    {
        var ex = Assert.Throws<ModuleSchemaException>(() =>
            Validation.Validate(false, "Value {0} in field {1} is invalid", 42, "KitName"));
        Assert.AreEqual("Value 42 in field KitName is invalid", ex!.Message);
    }

    [Test]
    public void Validate_BoolStringTwoArgs_True_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => Validation.Validate(true, "Value {0} in field {1} is invalid", 42, "KitName"));
    }
}
