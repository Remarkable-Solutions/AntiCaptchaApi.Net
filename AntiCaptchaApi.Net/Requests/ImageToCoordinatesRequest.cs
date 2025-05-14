using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces;
using Newtonsoft.Json;

namespace AntiCaptchaApi.Net.Requests
{
    /// <summary>
    /// Obtain coordinates of objects in an image
    /// Post an image body and a comment in English and receive up to 6 sets of coordinates of given objects.
    /// You can request point coordinates as well as rectangle coordinates.
    /// Maximum image size on one side is 500 pixels.
    /// Images larger than that will be downscaled on the worker's interface.
    /// </summary>
    public class ImageToCoordinatesRequest : CaptchaRequest<ImageToCoordinatesSolution>, IImageToCoordinatesRequest
    {
        /// <summary>
        /// [Required]
        /// File body encoded in base64. Make sure to send it without line breaks.
        /// Do not include 'data:image/png,' or similar tags, only clean base64!
        /// </summary>
        [JsonProperty("body")]
        public string Body { get; set; }
        /// <summary>
        /// [Optional]
        /// Comments for the task in English characters only. Example: "Select objects in specified order" or "select all cars".
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// [Optional]
        /// Task mode, can be "points" or "rectangles". The default is "points".
        /// </summary>
        public ImageToCoordinatesMode Mode { get; set; } = ImageToCoordinatesMode.Points;
        /// <summary>
        /// [Optional]
        /// Optional parameter to distinguish source of image captchas in spending statistics.
        /// </summary>
        public string WebsiteURL { get; set; }
        
        public ImageToCoordinatesRequest()
        {
            
        }
        
        public ImageToCoordinatesRequest(IImageToCoordinatesRequest request) : base(request)
        {
            Body = request.Body;
            Comment = request.Comment;
            Mode = request.Mode;
            WebsiteURL = request.WebsiteURL;
        }
    }
}