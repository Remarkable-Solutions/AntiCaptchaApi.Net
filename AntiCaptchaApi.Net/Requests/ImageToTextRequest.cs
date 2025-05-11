using System.IO;
using AntiCaptchaApi.Net.Enums;
using AntiCaptchaApi.Net.Internal.Helpers;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces;
using Newtonsoft.Json;

namespace AntiCaptchaApi.Net.Requests
{
    /// <summary>
    /// Represents a request to solve an image captcha (ImageToText task).
    /// The task involves sending an image file (as base64) and receiving the text depicted in it.
    /// </summary>
    /// <remarks>
    /// Supported formats include JPG, PNG, GIF. Max file size: 500KB.
    /// Text can contain digits, letters, special characters, and spaces.
    /// Custom captchas like "find a cat in this series of images" are not supported.
    /// See https://anti-captcha.com/apidoc/task-types/ImageToTextTask for more details.
    /// </remarks>
    public class ImageToTextRequest : CaptchaRequest<ImageToTextSolution>, IImageToTextRequest
    {
        /// <summary>
        /// [Required] Gets or sets the image file body encoded in base64.
        /// Ensure the string is clean base64 without line breaks or data URI prefixes (e.g., 'data:image/png;base64,').
        /// This property can be set directly or indirectly via the <see cref="FilePath"/> property.
        /// </summary>
        [JsonProperty("body")]
        public string BodyBase64 { get; set; }
        
        /// <summary>
        /// [Optional] Gets or sets a value indicating whether the answer must contain at least one space.
        /// <c>false</c> (default): No requirement.
        /// <c>true</c>: Requires workers to enter an answer with at least one space. Use with caution, as tasks without spaces might be skipped.
        /// </summary>
        public bool? Phrase { get; set; }
        
        
        /// <summary>
        /// [Optional] Gets or sets a value indicating whether the answer must be entered with case sensitivity.
        /// <c>false</c> (default): No requirement.
        /// <c>true</c>: Workers see a special mark indicating case sensitivity is required.
        /// </summary>
        public bool? Case { get; set; }
        
        
        /// <summary>
        /// [Optional] Gets or sets the numeric requirement for the answer.
        /// <see cref="NumericOption.NoRequirements"/> (default): No specific numeric requirement.
        /// <see cref="NumericOption.NumbersOnly"/>: Only numbers are allowed.
        /// <see cref="NumericOption.LettersOnly"/>: Any letters are allowed, but no numbers.
        /// </summary>
        public NumericOption? Numeric { get; set; }
        
        
        /// <summary>
        /// [Optional] Gets or sets a value indicating whether the answer requires mathematical calculation.
        /// <c>false</c> (default): No requirement.
        /// <c>true</c>: Workers see a special mark indicating the answer must be calculated (e.g., solve "2 + 3").
        /// </summary>
        public bool? Math { get; set; }
        
        
        /// <summary>
        /// [Optional] Gets or sets the minimum required length for the answer.
        /// <c>0</c> (default): No requirement.
        /// Any value greater than 0 defines the minimum length.
        /// </summary>
        public int? MinLength { get; set; }
        
        
        /// <summary>
        /// [Optional] Gets or sets the maximum required length for the answer.
        /// <c>0</c> (default): No requirement.
        /// Any value greater than 0 defines the maximum length.
        /// </summary>
        public int? MaxLength { get; set; }
        
        
        /// <summary>
        /// [Optional] Gets or sets additional comments for workers (e.g., "enter red text only").
        /// Providing comments does not guarantee the worker will follow them precisely.
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// [Write-Only Optional] Sets a file path from which to load the image.
        /// When this property is set, the file specified by the path is read, converted to a base64 string, and assigned to the <see cref="BodyBase64"/> property.
        /// If the file does not exist, <see cref="BodyBase64"/> remains unchanged.
        /// </summary>
        [JsonIgnore] // Prevent this property from being serialized in the API request payload
        public string FilePath
        {
            // This property only has a setter to load the file content into BodyBase64.
            // It doesn't store the path itself after setting.
            set
            {
                if (File.Exists(value))
                {
                    BodyBase64 = StringHelper.ImageFileToBase64String(value);
                }
            }
        }

        public ImageToTextRequest()
        {
            
        }
        
        public ImageToTextRequest(IImageToTextRequest request) : base(request)
        {
            BodyBase64 = request.BodyBase64;
            Phrase = request.Phrase;
            Case = request.Case;
            Numeric = request.Numeric;
            Math = request.Math;
            MinLength = request.MinLength;
            MaxLength = request.MaxLength;
            Comment = request.Comment;
        }
    }
}