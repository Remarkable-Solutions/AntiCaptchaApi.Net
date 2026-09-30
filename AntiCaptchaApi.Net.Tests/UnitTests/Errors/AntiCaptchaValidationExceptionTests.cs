using System;
using System.Collections.Generic;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Errors;

public class AntiCaptchaValidationExceptionTests
{
    [Fact]
    public void Message_ListsEveryFieldError()
    {
        var exception = new AntiCaptchaValidationException(new[]
        {
            new ValidationError("WebsiteUrl", "must be an absolute http(s) URL"),
            new ValidationError("WebsiteKey", "must not be empty"),
        });

        Assert.Contains("WebsiteUrl: must be an absolute http(s) URL", exception.Message);
        Assert.Contains("WebsiteKey: must not be empty", exception.Message);
    }

    [Fact]
    public void Errors_AreReadOnly()
    {
        var source = new List<ValidationError> { new("WebsiteKey", "must not be empty") };
        var exception = new AntiCaptchaValidationException(source);

        source.Add(new ValidationError("Other", "added later"));

        Assert.Single(exception.Errors);
        Assert.Throws<NotSupportedException>(() => ((IList<ValidationError>)exception.Errors).Add(new ValidationError("X", "y")));
    }
}
