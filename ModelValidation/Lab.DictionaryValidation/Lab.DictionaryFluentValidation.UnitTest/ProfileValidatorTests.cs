using System.Collections.Generic;
using System.Linq;
using Lab.DictionaryFluentValidation.Validators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.DictionaryFluentValidation.UnitTest;

[TestClass]
public class ProfileValidatorTests
{
    [TestMethod]
    public void aaaa()
    {
        var profileValidator = new ProfileTypeValidator();
        var data = new Dictionary<string, object>
        {
            { "name", new { firstName = "yao", lastName = "yu", fullName = "yao-chang.yu" } },
            { "birthday", new { year = 2000, month = 2, day = 28 } },
            { "contactEmail", "yao@aa.bb" },
        };
        var validationResult = profileValidator.Validate(data);
        
        data = new Dictionary<string, object>
        {
            { "gender", "公的" },
        };
        validationResult = profileValidator.Validate(data);

        data = new Dictionary<string, object>
        {
            { "Name", null },
        };

        validationResult = profileValidator.Validate(data);
        data = new Dictionary<string, object>
        {
            { "name", new { firstName = "yao", lastName = "", fullName = "" } },
        };
        validationResult = profileValidator.Validate(data);
        data = new Dictionary<string, object>
        {
            { "Hi", null },
        };
        validationResult = profileValidator.Validate(data);
    }

    [TestMethod]
    public void Key區分大小寫()
    {
        var data = new Dictionary<string, object>
        {
            { "Name", null },
        };

        var profileValidator = new ProfileTypeValidator();
        var validationResult = profileValidator.Validate(data);
        Assert.AreEqual(false, validationResult.IsValid);
        var actualError = validationResult.Errors.First();
        Assert.AreEqual("Name", actualError.PropertyName);
        Assert.AreEqual("NotSupportValidator", actualError.ErrorCode);
        Assert.AreEqual("'Name' column not support", actualError.ErrorMessage);
    }

    [TestMethod]
    public void 二月三十是非法日期()
    {
        var data = new Dictionary<string, object>
        {
            { "birthday", new { year = 2000, month = 2, day = 30 } },
        };

        var profileValidator = new ProfileTypeValidator();
        var validationResult = profileValidator.Validate(data);
        Assert.AreEqual(false, validationResult.IsValid);
        var actualError = validationResult.Errors.First();
        Assert.AreEqual("birthday", actualError.PropertyName);
        Assert.AreEqual(nameof(BirthdayTypeValidator), actualError.ErrorCode);
        Assert.AreEqual("year:2000,month:2,day:30 is invalid date format", actualError.ErrorMessage);
    }

    [TestMethod]
    public void 日期內容為非法值()
    {
        var data = new Dictionary<string, object>
        {
            { "birthday", null },
        };

        var profileValidator = new ProfileTypeValidator();
        var validationResult = profileValidator.Validate(data);
        Assert.AreEqual(true, validationResult.IsValid);
    }

    [TestMethod]
    public void 必填欄位為空()
    {
        var data = new Dictionary<string, object>
        {
            { "name", new { firstName = "yao", lastName = "", fullName = "" } },
        };
        var profileValidator = new ProfileTypeValidator();
        var validationResult = profileValidator.Validate(data);

        // Assert.AreEqual(false, validationResult.IsValid);
        var actualError = validationResult.Errors.First();
        Assert.AreEqual("name.fullName", actualError.PropertyName);
        Assert.AreEqual("NotEmptyValidator", actualError.ErrorCode);
        Assert.AreEqual("'name.fullName' must not be empty.", actualError.ErrorMessage);
    }

    [TestMethod]
    public void 沒有年是非法日期()
    {
        var data = new Dictionary<string, object>
        {
            { "birthday", new { month = 2, day = 30 } },
        };

        var profileValidator = new ProfileTypeValidator();
        var validationResult = profileValidator.Validate(data);
        Assert.AreEqual(false, validationResult.IsValid);
        var actualError = validationResult.Errors.First();
        Assert.AreEqual("birthday.year", actualError.PropertyName);
        Assert.AreEqual(nameof(BirthdayTypeValidator), actualError.ErrorCode);
        Assert.AreEqual("'birthday.year' must not be empty.", actualError.ErrorMessage);
    }

    [TestMethod]
    public void 使用不支援的Key()
    {
        var data = new Dictionary<string, object>
        {
            { "Hi", null },
        };

        var profileValidator = new ProfileTypeValidator();
        var validationResult = profileValidator.Validate(data);
        Assert.AreEqual(false, validationResult.IsValid);
        var actualError = validationResult.Errors.First();
        Assert.AreEqual("Hi", actualError.PropertyName);
        Assert.AreEqual("NotSupportValidator", actualError.ErrorCode);
        Assert.AreEqual("'Hi' column not support", actualError.ErrorMessage);
    }

    [TestMethod]
    public void 使用支援的Key()
    {
        var data = new Dictionary<string, object>
        {
            { "name", null },
        };

        var profileValidator = new ProfileTypeValidator();
        var validationResult = profileValidator.Validate(data);
        Assert.AreEqual(true, validationResult.IsValid);
    }

    [TestMethod]
    public void 性別格式錯誤()
    {
        var data = new Dictionary<string, object>
        {
            { "gender", "公的" },
        };
        var profileValidator = new ProfileTypeValidator();
        var validationResult = profileValidator.Validate(data);
        Assert.AreEqual(false, validationResult.IsValid);
        var actualError = validationResult.Errors.First();
        Assert.AreEqual("gender", actualError.PropertyName);
        Assert.AreEqual(nameof(GenderTypeValidator), actualError.ErrorCode);
        Assert.AreEqual("'公的' is invalid value.", actualError.ErrorMessage);
    }

    [TestMethod]
    public void 通過驗證()
    {
        var data = new Dictionary<string, object>
        {
            { "name", new { firstName = "yao", lastName = "yu", fullName = "yao-chang.yu" } },
            { "birthday", new { year = 2000, month = 2, day = 28 } },
            { "contactEmail", "yao@aa.bb" },
        };

        var profileValidator = new ProfileTypeValidator();
        var validationResult = profileValidator.Validate(data);
        Assert.AreEqual(true, validationResult.IsValid);
    }

    [TestMethod]
    public void 郵件格式錯誤()
    {
        var data = new Dictionary<string, object>
        {
            { "contactEmail", "yao" },
        };
        var profileValidator = new ProfileTypeValidator();
        var validationResult = profileValidator.Validate(data);
        Assert.AreEqual(false, validationResult.IsValid);
        var actualError = validationResult.Errors.First();
        Assert.AreEqual("contactEmail", actualError.PropertyName);
        Assert.AreEqual(nameof(EmailTypeValidator), actualError.ErrorCode);
        Assert.AreEqual("'contactEmail' is not a valid email address.", actualError.ErrorMessage);
    }
}