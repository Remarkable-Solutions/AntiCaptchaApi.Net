using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Tasks;

public class FriendlyCaptchaTaskTests
{
    [Fact]
    public void Validate_ProxyWithoutUserAgent_Fails()
    {
        var task = new FriendlyCaptchaTask(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey) { Proxy = TaskSamples.Proxy };

        var exception = Assert.Throws<AntiCaptchaValidationException>(task.RunValidation);

        Assert.Equal(new ValidationError("UserAgent", "required when Proxy is set"), Assert.Single(exception.Errors));
    }

    // 2.x reused ProsopoSolution for FriendlyCaptcha.
    [Fact]
    public void Solution_IsFriendlyCaptchaSolution()
    {
        var task = new FriendlyCaptchaTask(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey);

        Assert.IsAssignableFrom<CaptchaTask<FriendlyCaptchaSolution>>(task);
    }

    [Fact]
    public void TypeName_ProxylessAndProxy()
    {
        var task = new FriendlyCaptchaTask(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey);

        Assert.Equal("FriendlyCaptchaTaskProxyless", task.ResolveTypeName());
        Assert.Equal("FriendlyCaptchaTask", (task with { Proxy = TaskSamples.Proxy }).ResolveTypeName());
    }
}
