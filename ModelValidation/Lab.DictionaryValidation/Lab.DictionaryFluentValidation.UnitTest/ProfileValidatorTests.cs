using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using Lab.DictionaryFluentValidation.Fields;
using Lab.DictionaryFluentValidation.Validators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.DictionaryFluentValidation.UnitTest;

[TestClass]
public class ProfileValidatorTests
{
    [TestMethod]
    public void TestMethod1()
    {
        var data = new Dictionary<string, object>()
        {
            { "contactEmail", "yao" },
            { "Name", new { firstName = "yao", lastName = "yu", fullName = "yao-chang.yu" } },
        };
        var options = new JsonSerializerOptions()
        {
            Converters = { new DictionaryStringObjectJsonConverter() }
        };

        
        var profileValidator = new ProfileValidator();
        var validationResult = profileValidator.Validate(data);
    }
}