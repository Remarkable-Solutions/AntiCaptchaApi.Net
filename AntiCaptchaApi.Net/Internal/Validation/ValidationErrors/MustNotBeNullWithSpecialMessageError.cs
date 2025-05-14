namespace AntiCaptchaApi.Net.Internal.Validation.ValidationErrors
{
    internal class MustNotBeNullWithSpecialMessageError : MustNotBeNullError
    {
        public string Message { get; }

        internal MustNotBeNullWithSpecialMessageError(string propertyName, string message) : base(propertyName)
        {
            this.Message = message;
        }

        public override string ToString()
        {
            return $"{base.ToString()} {Message}";
        }
    }
}
