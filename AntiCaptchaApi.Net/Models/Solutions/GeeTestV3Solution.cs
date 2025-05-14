namespace AntiCaptchaApi.Net.Models.Solutions
{
    public class GeeTestV3Solution : BaseSolution
    {
        public string Challenge { get; set; }
        public string Seccode { get; set; }
        public string Validate { get; set; }
        public override bool IsValid() => Challenge != null && Seccode != null && Validate != null;
    }
}