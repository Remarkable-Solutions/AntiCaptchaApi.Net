namespace AntiCaptchaApi.Net.Enums
{
    /// <summary>
    /// Specifies the outcome of a captcha task being reported.
    /// </summary>
    public enum ReportOutcome
    {
        /// <summary>
        /// Report that an image-based captcha (like ImageToText) was solved incorrectly.
        /// Corresponds to the 'reportIncorrectImageCaptcha' API method.
        /// </summary>
        IncorrectImageCaptcha,

        /// <summary>
        /// Report that a ReCaptcha task was solved incorrectly.
        /// Corresponds to the 'reportIncorrectRecaptcha' API method.
        /// </summary>
        IncorrectRecaptcha,

        /// <summary>
        /// Report that a ReCaptcha task was solved correctly.
        /// Corresponds to the 'reportCorrectRecaptcha' API method.
        /// </summary>
        CorrectRecaptcha,

        /// <summary>
        /// Report that an HCaptcha task was solved incorrectly.
        /// Corresponds to the 'reportIncorrectHCaptcha' API method.
        /// </summary>
        IncorrectHCaptcha
    }
}