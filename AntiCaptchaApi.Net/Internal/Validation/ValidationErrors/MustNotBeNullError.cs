namespace AntiCaptchaApi.Net.Internal.Validation.ValidationErrors
{
    internal class MustNotBeNullError : ValidationError
    {
        internal MustNotBeNullError(string propertyName) : base(propertyName, "must not be null!")
        {
        
        }
    
        public override string ToString()
        {
            return base.ToString();
        }
    }
}