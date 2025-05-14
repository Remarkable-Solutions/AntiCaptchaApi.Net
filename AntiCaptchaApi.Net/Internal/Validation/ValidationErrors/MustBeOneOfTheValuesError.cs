using System.Collections.Generic;

namespace AntiCaptchaApi.Net.Internal.Validation.ValidationErrors
{
    internal class MustBeOneOfTheValuesError : ValidationError
    {
        public List<string> CorrectValues { get; }

        internal MustBeOneOfTheValuesError(string propertyName, List<string> correctValues) : base(propertyName, "do not have correct value.")
        {
            CorrectValues = correctValues;
        }
        public override string ToString() => $"{base.ToString()}. Correct values: {string.Join(',', CorrectValues)}.";
    }
}