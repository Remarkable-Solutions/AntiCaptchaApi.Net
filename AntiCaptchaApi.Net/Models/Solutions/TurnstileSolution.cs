namespace AntiCaptchaApi.Net.Models.Solutions
{
    public class TurnstileSolution : BaseSolution
    {
        public string Token { get; set; }
        public string UserAgent { get; set; }
    
        public override bool IsValid() => !string.IsNullOrEmpty(Token) && !string.IsNullOrEmpty(UserAgent); 
    }
}