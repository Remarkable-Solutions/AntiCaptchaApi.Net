namespace AntiCaptchaApi.Net.Models.Solutions
{
    public class RecaptchaSolution : BaseSolution
    {
        public string GRecaptchaResponse { get; set; }
        public string GRecaptchaResponseMd5 { get; set; }
        public override bool IsValid() =>
            GRecaptchaResponse != null;
    }
}