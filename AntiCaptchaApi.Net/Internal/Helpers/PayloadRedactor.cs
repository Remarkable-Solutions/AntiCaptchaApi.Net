using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AntiCaptchaApi.Net.Internal.Helpers
{
    /// <summary>
    /// Produces a copy of a request payload that is safe to expose through
    /// <see cref="Responses.Abstractions.BaseResponse.RawPayload"/>: the top-level <c>clientKey</c> and every
    /// <c>proxyPassword</c> (at any depth) are replaced with <see cref="Mask"/>.
    /// </summary>
    internal static class PayloadRedactor
    {
        internal const string Mask = "***";
        internal const string UnparseablePlaceholder = "[payload redacted: unparseable]";

        private const string ClientKeyProperty = "clientKey";
        private const string ProxyPasswordProperty = "proxyPassword";

        /// <summary>
        /// Returns the redacted JSON. Never returns the raw text of a payload it cannot parse.
        /// </summary>
        internal static string Redact(string json)
        {
            if (json == null)
                return null;

            JObject root;
            try
            {
                root = JObject.Parse(json);
            }
            catch (JsonException)
            {
                return UnparseablePlaceholder;
            }

            if (root.ContainsKey(ClientKeyProperty))
                root[ClientKeyProperty] = Mask;

            foreach (var property in root.Descendants().OfType<JProperty>()
                         .Where(p => string.Equals(p.Name, ProxyPasswordProperty, StringComparison.Ordinal))
                         .ToList())
            {
                property.Value = Mask;
            }

            return root.ToString();
        }
    }
}
