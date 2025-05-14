namespace AntiCaptchaApi.Net.Models.Solutions
{
    public class ImageToTextSolution : BaseSolution
    {
        public string Text { get; set; }
        
        public string Url { get; set; }
    
        public override bool IsValid() =>
            Text != null && Url != null;
    }
}