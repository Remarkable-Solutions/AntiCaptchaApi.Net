using AntiCaptchaApi.Net.Models.Solutions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace AntiCaptchaApi.Net.Requests.Abstractions.Interfaces
{
    public interface IImageToCoordinatesRequest : ICaptchaRequest<ImageToCoordinatesSolution>
    {
        public string Body { get; set; }
        public string Comment { get; set; }
        [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
        public ImageToCoordinatesMode Mode { get; set; }
        public string WebsiteURL { get; set; }
    }

    public enum ImageToCoordinatesMode {
        Points,
        Rectangles
    }
}