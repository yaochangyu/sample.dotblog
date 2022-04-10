using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Lab.DictionaryFluentValidation.Fields;
using Lab.DictionaryFluentValidation.Validators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.DictionaryFluentValidation.UnitTest;

[TestClass]
public class ProfileValidatorTests
{
    [TestMethod]
    public void 郵件格式錯誤()
    {
        var data = new Dictionary<string, object>()
        {
            { "contactEmail", "yao" },
        };
        var profileValidator = new ProfileValidator();
        var validationResult = profileValidator.Validate(data);
        Assert.AreEqual(false, validationResult.IsValid);
        var actualError = validationResult.Errors.First();
        Assert.AreEqual("contactEmail", actualError.PropertyName);
        Assert.AreEqual("EmailValidator", actualError.ErrorCode);
        Assert.AreEqual("'contactEmail' is not a valid email address.", actualError.ErrorMessage);
    }

    [TestMethod]
    public void 必填欄位為空()
    {
        var data = new Dictionary<string, object>()
        {
            { "name", new { firstName = "yao", lastName = "", fullName = "" } },
        };
        var profileValidator = new ProfileValidator();
        var validationResult = profileValidator.Validate(data);
        // Assert.AreEqual(false, validationResult.IsValid);
        var actualError = validationResult.Errors.First();
        Assert.AreEqual("name.fullName", actualError.PropertyName);
        Assert.AreEqual("NotEmptyValidator", actualError.ErrorCode);
        Assert.AreEqual("'name.fullName' must not be empty.", actualError.ErrorMessage);
    }

    [TestMethod]
    public void 使用不支援的Key()
    {
        var data = new Dictionary<string, object>()
        {
            { "Hi", null },
        };

        var profileValidator = new ProfileValidator();
        var validationResult = profileValidator.Validate(data);
        Assert.AreEqual(false, validationResult.IsValid);
        var actualError = validationResult.Errors.First();
        Assert.AreEqual("Hi", actualError.PropertyName);
        Assert.AreEqual("NotSupportValidator", actualError.ErrorCode);
        Assert.AreEqual("not support column 'Hi'", actualError.ErrorMessage);
    }

    [TestMethod]
    public void Key區分大小寫()
    {
        var data = new Dictionary<string, object>()
        {
            { "Name", null },
        };

        var profileValidator = new ProfileValidator();
        var validationResult = profileValidator.Validate(data);
        Assert.AreEqual(false, validationResult.IsValid);
        var actualError = validationResult.Errors.First();
        Assert.AreEqual("Name", actualError.PropertyName);
        Assert.AreEqual("NotSupportValidator", actualError.ErrorCode);
        Assert.AreEqual("not support column 'Name'", actualError.ErrorMessage);
    }

    [TestMethod]
    public void 使用支援的Key()
    {
        var data = new Dictionary<string, object>()
        {
            { "name", null },
        };

        var profileValidator = new ProfileValidator();
        var validationResult = profileValidator.Validate(data);
        Assert.AreEqual(true, validationResult.IsValid);
    }
}