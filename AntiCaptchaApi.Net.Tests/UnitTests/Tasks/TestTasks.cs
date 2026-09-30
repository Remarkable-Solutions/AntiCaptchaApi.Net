using AntiCaptchaApi.Net.Tasks;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Tasks;

// Test-only task types. They can derive from the closed CaptchaTask hierarchy because the test assembly is a friend
// assembly (InternalsVisibleTo + private protected constructors). They are deliberately not registered in
// AntiCaptchaJsonContext, and the core-assembly discovery tests never see them.

internal sealed record TestSolution(string Value) : Solution;

internal sealed record NoneTestTask(string WebsiteKey) : CaptchaTask<TestSolution>
{
    protected override string TypeBase => "NoneTestTask";

    protected override ProxyMode ProxyMode => ProxyMode.None;

    protected override void Validate(Validator v) => v.NotEmpty(WebsiteKey);
}

internal sealed record OptionalTestTask(string WebsiteKey) : CaptchaTask<TestSolution>
{
    protected override string TypeBase => "OptionalTestTask";

    protected override void Validate(Validator v) => v.NotEmpty(WebsiteKey);
}

internal sealed record RequiredTestTask(string WebsiteKey) : CaptchaTask<TestSolution>
{
    protected override string TypeBase => "RequiredTestTask";

    protected override ProxyMode ProxyMode => ProxyMode.Required;

    protected override void Validate(Validator v) => v.NotEmpty(WebsiteKey);
}

internal sealed record SameNameTestTask(string WebsiteKey) : CaptchaTask<TestSolution>
{
    protected override string TypeBase => "SameNameTestTask";

    protected override ProxyMode ProxyMode => ProxyMode.OptionalSameName;

    protected override void Validate(Validator v) => v.NotEmpty(WebsiteKey);
}
