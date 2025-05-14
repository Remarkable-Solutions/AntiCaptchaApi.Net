namespace AntiCaptchaApi.Net.Internal.Validation.ValidationErrors
{
    internal class MustNotBeNullOrEmptyError : ValidationError
    {
        internal MustNotBeNullOrEmptyError(string propertyName) : base(propertyName, "must not be null or empty!")
        {
        
        }
    
        public override string ToString()
        {
            return base.ToString();
        }
    }
}