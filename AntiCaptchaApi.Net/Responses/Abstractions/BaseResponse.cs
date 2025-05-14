namespace AntiCaptchaApi.Net.Responses.Abstractions
{
    public abstract class BaseResponse
    {
        public int? ErrorId { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorDescription {  get; set; }
        
        public string RawResponse { get; set; }
        
        public string RawPayload { get; set; }

        protected BaseResponse()
        {
            
        }

        protected BaseResponse(string errorCode, string errorDescription)
        {
            ErrorCode = errorCode;
            ErrorDescription = errorDescription;
        }
        
        public bool IsErrorResponse => 
            !string.IsNullOrEmpty(ErrorDescription) || !string.IsNullOrEmpty(ErrorCode) || ErrorId != 0;
    }
}