namespace AntiCaptchaApi.Net.Internal.Common
{
    /// <summary>
    /// Error codes that <see cref="Helpers.HttpHelper"/> sets for local transport failures (no API answer).
    /// Shared with the polling logic so the strings cannot drift.
    /// </summary>
    internal static class TransportErrorCodes
    {
        internal const string HttpError = "HttpError";
        internal const string JsonError = "JsonError";
        internal const string UnexpectedError = "UnexpectedError";
        internal const string Cancelled = "Cancelled";

        /// <summary>
        /// True when <paramref name="errorCode"/> is a transport failure worth retrying. <see cref="Cancelled"/>
        /// only counts when the caller did not cancel (i.e. it was an <c>HttpClient</c> timeout).
        /// </summary>
        internal static bool IsTransient(string errorCode, bool callerCancelled)
        {
            switch (errorCode)
            {
                case HttpError:
                case JsonError:
                case UnexpectedError:
                    return true;
                case Cancelled:
                    return !callerCancelled;
                default:
                    return false;
            }
        }
    }
}
