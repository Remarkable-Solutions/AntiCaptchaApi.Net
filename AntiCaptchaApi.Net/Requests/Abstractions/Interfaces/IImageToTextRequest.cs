using AntiCaptchaApi.Net.Enums;
using AntiCaptchaApi.Net.Models.Solutions;

namespace AntiCaptchaApi.Net.Requests.Abstractions.Interfaces
{
    public interface IImageToTextRequest : ICaptchaRequest<ImageToTextSolution>
    {
        public string BodyBase64 { get; set; }
        public bool? Phrase { get; set; }
        public bool? Case { get; set; }
        public NumericOption? Numeric { get; set; }
        public bool? Math { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public string Comment { get; set; }
        public string FilePath
        {
            set;
        }
    }
}