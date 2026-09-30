namespace AntiCaptchaApi.Net.Tasks;

/// <summary>Which characters an <see cref="ImageToTextTask"/> answer may contain. Sent as the number <c>0</c>, <c>1</c> or <c>2</c>.</summary>
public enum NumericOption
{
    /// <summary>No requirement (<c>0</c>).</summary>
    NoRequirements = 0,

    /// <summary>Only numbers are allowed (<c>1</c>).</summary>
    NumbersOnly = 1,

    /// <summary>Any letters are allowed, but no numbers (<c>2</c>).</summary>
    AnyLettersExceptNumbers = 2,
}
