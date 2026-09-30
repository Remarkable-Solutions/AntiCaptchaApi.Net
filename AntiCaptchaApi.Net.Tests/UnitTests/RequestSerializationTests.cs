using System.Collections.Generic;
using System.Linq;
using AntiCaptchaApi.Net.Enums;
using AntiCaptchaApi.Net.Internal;
using AntiCaptchaApi.Net.Models;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests
{
    public class RequestSerializationTests
    {
      
        [Fact]
        public void FunCaptchaTaskProxylessRequestSerializationTest()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "FunCaptchaTaskProxyless",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["websitePublicKey"] = "69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["funcaptchaApiJSSubdomain"] = "funcaptchaApiJSSubdomainTest",
                ["data"] = "TestData"
            };

        
            var request =
                new FunCaptchaProxylessRequest
                {
                    WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                    WebsitePublicKey = expectedPayload["websitePublicKey"]!.ToString(),
                    FunCaptchaApiJsSubdomain = expectedPayload["funcaptchaApiJSSubdomain"]!.ToString(),
                    Data = expectedPayload["data"]!.ToString(),
                };

            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }

        [Fact]
        public void FunCaptchaTaskProxylessRequestSerializationTest_OnlyRequiredArguments()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "FunCaptchaTaskProxyless",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["websitePublicKey"] = "69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC"
            };
    
        
            var request =
                new FunCaptchaProxylessRequest
                {
                    WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                    WebsitePublicKey = expectedPayload["websitePublicKey"]!.ToString(),
                };
    
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }

        [Fact]
        public void FunCaptchaTaskProxyRequestSerializationTest()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "FunCaptchaTask",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["websitePublicKey"] = "69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["funcaptchaApiJSSubdomain"] = "funcaptchaApiJSSubdomainTest",
                ["data"] = "TestData", 
                ["proxyType"] = "http",
                ["proxyAddress"] = "proxyAddressTest",
                ["proxyPort"] = 1080,
                ["proxyLogin"] = "proxyLoginTest",
                ["proxyPassword"] = "proxyPasswordTest",
                ["userAgent"] = "testUserAgent",
            };
        
            var request = new FunCaptchaRequest
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                WebsitePublicKey = expectedPayload["websitePublicKey"]!.ToString(),
                FunCaptchaApiJsSubdomain = expectedPayload["funcaptchaApiJSSubdomain"]!.ToString(),
                Data = expectedPayload["data"]!.ToString(),
                UserAgent = expectedPayload["userAgent"]!.ToString(),
                ProxyConfig = new ProxyConfig
                {
                    ProxyType = ProxyTypeOption.Http,
                    ProxyAddress = expectedPayload["proxyAddress"]!.ToString(),
                    ProxyLogin = expectedPayload["proxyLogin"]!.ToString(),
                    ProxyPassword = expectedPayload["proxyPassword"]!.ToString(),
                    ProxyPort = int.Parse(expectedPayload["proxyPort"]!.ToString()),
                }
            };

    
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }

        [Fact]
        public void FunCaptchaTaskProxyRequestSerializationTest_OnlyRequiredArguments()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "FunCaptchaTask",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["websitePublicKey"] = "69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["proxyType"] = "http",
                ["proxyAddress"] = "proxyAddressTest",
                ["proxyPort"] = 1080,
                ["userAgent"] = "testUserAgent",
            };
        
            var request = new FunCaptchaRequest
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                WebsitePublicKey = expectedPayload["websitePublicKey"]!.ToString(),
                UserAgent = expectedPayload["userAgent"]!.ToString(),
                ProxyConfig = new ProxyConfig
                {
                    ProxyType = ProxyTypeOption.Http,
                    ProxyAddress = expectedPayload["proxyAddress"]!.ToString(),
                    ProxyPort = int.Parse(expectedPayload["proxyPort"]!.ToString()),
                }
            };
        
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }
        
        private void AssertSame(JObject expected, JObject actual)
        {
            if (!JToken.DeepEquals(expected, actual))
            {
                var actualProperties = actual.Properties().Select(x => x.Name).ToList();
                var expectedProperties = expected.Properties().Select(x => x.Name).ToList();

                var missingProperties = expectedProperties.Where(x => !actualProperties.Contains(x)).ToList();

                if (missingProperties.Any())
                {
                    Fail($"Payloads do not match. Missing properties: {string.Join(", ", missingProperties)}");
                    return;
                }

                var extraProperties = actualProperties.Where(x => !expectedProperties.Contains(x)).ToList();

                if (extraProperties.Any())
                {
                    Fail($"Payloads do not match. Extra properties properties: {string.Join(", ", extraProperties)}");
                    return;
                }


                var expectedPropertyValues = expected.Properties().Select(x => (x.Name, x.Value)).ToList();
                var actualPropertyValues = actual.Properties().Select(x => (x.Name, x.Value)).ToList();


                var wrongPropertyValues =
                    actualPropertyValues.Where(x => expectedPropertyValues.Single(y => y.Item1 == x.Item1).Item2.ToString() != x.Item2.ToString())
                        .Select(x => (x.Item1, x.Item2, expectedPropertyValues.Single(y => y.Item1 == x.Item1).Item2))
                        .Select(x => $"Property Name {x.Item1}, actual value: {x.Item2}, expected value: {x.Item3}").ToList();
                if (wrongPropertyValues.Any())
                {
                    Fail($"Payloads do not match. Wrong property values: \n{string.Join("\n", wrongPropertyValues)}");
                    return;
                }
            
                Assert.False(true, $"Payloads do not match.");
            }
        }

        private static void Fail(string message)
        {
            Assert.False(true, message);
        }


        [Fact]
        public void GeeTestV3CaptchaRequestSerializationTest()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "GeeTestTask",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["gt"] = "gtTest",
                ["geetestGetLib"] = "geetestGetLibTest",
                ["challenge"] = "challengeTest",
                ["version"] = 3,
                ["geetestApiServerSubdomain"] = "geetestApiServerSubdomainTest",
                ["proxyType"] = "socks4",
                ["proxyAddress"] = "proxyAddressTest",
                ["proxyPort"] = 1080,
                ["proxyLogin"] = "proxyLoginTest",
                ["proxyPassword"] = "proxyPasswordTest",
                ["userAgent"] = "testUserAgent",
            };
        
            var request = new GeeTestV3Request()
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                Gt = expectedPayload["gt"]!.ToString(),
                Challenge = expectedPayload["challenge"]!.ToString(),
                UserAgent = expectedPayload["userAgent"]!.ToString(),
                GeetestGetLib = expectedPayload["geetestGetLib"]!.ToString(),
                GeetestApiServerSubdomain = expectedPayload["geetestApiServerSubdomain"]!.ToString(),
                ProxyConfig = new ProxyConfig
                {
                    ProxyType = ProxyTypeOption.Socks4,
                    ProxyAddress = expectedPayload["proxyAddress"]!.ToString(),
                    ProxyLogin = expectedPayload["proxyLogin"]!.ToString(),
                    ProxyPassword = expectedPayload["proxyPassword"]!.ToString(),
                    ProxyPort = int.Parse(expectedPayload["proxyPort"]!.ToString()),
                }
            };
        
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }

        [Fact]
        public void GeeTestV3CaptchaRequestSerializationTest_OnlyRequiredArguments()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "GeeTestTask",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["gt"] = "gtTest",
                ["challenge"] = "challengeTest",
                ["version"] = 3,
                ["proxyType"] = "socks4",
                ["proxyAddress"] = "proxyAddressTest",
                ["proxyPort"] = 1080,
                ["userAgent"] = "testUserAgent",
            };
        
            var request = new GeeTestV3Request()
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                Gt = expectedPayload["gt"]!.ToString(),
                Challenge = expectedPayload["challenge"]!.ToString(),
                UserAgent = expectedPayload["userAgent"]!.ToString(),
                ProxyConfig = new ProxyConfig
                {
                    ProxyType = ProxyTypeOption.Socks4,
                    ProxyAddress = expectedPayload["proxyAddress"]!.ToString(),
                    ProxyPort = int.Parse(expectedPayload["proxyPort"]!.ToString()),
                }
            };
        
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }

        [Fact]
        public void GeeTestV3ProxylessCaptchaRequestSerializationTest()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "GeeTestTaskProxyless",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["gt"] = "gtTest",
                ["geetestGetLib"] = "geetestGetLibTest",
                ["challenge"] = "challengeTest",
                ["version"] = 3,
                ["geetestApiServerSubdomain"] = "geetestApiServerSubdomainTest",
            };
        
            var request = new GeeTestV3ProxylessRequest()
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                Gt = expectedPayload["gt"]!.ToString(),
                Challenge = expectedPayload["challenge"]!.ToString(),
                GeetestGetLib = expectedPayload["geetestGetLib"]!.ToString(),
                GeetestApiServerSubdomain = expectedPayload["geetestApiServerSubdomain"]!.ToString(),
            };
        
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }

        [Fact]
        public void GeeTestV3ProxylessCaptchaRequestSerializationTest_OnlyRequiredArguments()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "GeeTestTaskProxyless",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["gt"] = "gtTest",
                ["challenge"] = "challengeTest",
                ["version"] = 3,
            };
        
            var request = new GeeTestV3ProxylessRequest()
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                Gt = expectedPayload["gt"]!.ToString(),
                Challenge = expectedPayload["challenge"]!.ToString(),
            };
        
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }

        [Fact]
        public void GeeTestV4ProxylessCaptchaRequestSerializationTest()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "GeeTestTaskProxyless",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["gt"] = "gtTest",
                ["challenge"] = "challengeTest",
                ["geetestGetLib"] = "geetestGetLibTest",
                ["version"] = 4,
                ["geetestApiServerSubdomain"] = "geetestApiServerSubdomainTest",          
                ["initParameters"] = new JObject()
                {
                    ["initParam1"] = "initParam1Value",
                    ["initParam2"] = "initParam2Value"
                },
            };
        
            var request = new GeeTestV4ProxylessRequest()
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                Gt = expectedPayload["gt"]!.ToString(),
                Challenge = expectedPayload["challenge"]!.ToString(),
                GeetestGetLib = expectedPayload["geetestGetLib"]!.ToString(),
                GeetestApiServerSubdomain = expectedPayload["geetestApiServerSubdomain"]!.ToString(),
                InitParameters = new Dictionary<string, string>()
                {
                    ["initParam1"] = "initParam1Value",
                    ["initParam2"] = "initParam2Value"
                }
            };
        
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }

        [Fact]
        public void GeeTestV4ProxylessCaptchaRequestSerializationTest_OnlyRequiredArguments()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "GeeTestTaskProxyless",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["gt"] = "gtTest",
                ["challenge"] = "challengeTest",
                ["version"] = 4,     
            };
        
            var request = new GeeTestV4ProxylessRequest()
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                Gt = expectedPayload["gt"]!.ToString(),
                Challenge = expectedPayload["challenge"]!.ToString(),
            };
        
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }
    
        [Fact]
        public void GeeTestV4CaptchaRequestSerializationTest()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "GeeTestTask",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["gt"] = "gtTest",
                ["challenge"] = "challengeTest",
                ["geetestGetLib"] = "geetestGetLibTest",
                ["version"] = 4,
                ["geetestApiServerSubdomain"] = "geetestApiServerSubdomainTest",
                ["proxyType"] = "socks4",
                ["proxyAddress"] = "proxyAddressTest",
                ["proxyPort"] = 1080,
                ["proxyLogin"] = "proxyLoginTest",
                ["proxyPassword"] = "proxyPasswordTest",
                ["userAgent"] = "testUserAgent",
            };
        
            var request = new GeeTestV4Request()
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                Gt = expectedPayload["gt"]!.ToString(),
                Challenge = expectedPayload["challenge"]!.ToString(),
                UserAgent = expectedPayload["userAgent"]!.ToString(),
                GeetestGetLib = expectedPayload["geetestGetLib"]!.ToString(),
                GeetestApiServerSubdomain = expectedPayload["geetestApiServerSubdomain"]!.ToString(),
                ProxyConfig = new ProxyConfig
                {
                    ProxyType = ProxyTypeOption.Socks4,
                    ProxyAddress = expectedPayload["proxyAddress"]!.ToString(),
                    ProxyLogin = expectedPayload["proxyLogin"]!.ToString(),
                    ProxyPassword = expectedPayload["proxyPassword"]!.ToString(),
                    ProxyPort = int.Parse(expectedPayload["proxyPort"]!.ToString()),
                }
            };
        
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }
    
        [Fact]
        public void GeeTestV4CaptchaRequestSerializationTest_OnlyRequiredArguments()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "GeeTestTask",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["gt"] = "gtTest",
                ["challenge"] = "challengeTest",
                ["version"] = 4,
                ["proxyType"] = "socks4",
                ["proxyAddress"] = "proxyAddressTest",
                ["proxyPort"] = 1080,
                ["userAgent"] = "testUserAgent",
            };
        
            var request = new GeeTestV4Request()
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                Gt = expectedPayload["gt"]!.ToString(),
                Challenge = expectedPayload["challenge"]!.ToString(),
                UserAgent = expectedPayload["userAgent"]!.ToString(),
                ProxyConfig = new ProxyConfig
                {
                    ProxyType = ProxyTypeOption.Socks4,
                    ProxyAddress = expectedPayload["proxyAddress"]!.ToString(),
                    ProxyPort = int.Parse(expectedPayload["proxyPort"]!.ToString()),
                }
            };
        
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }

        [Fact]
        public void ImageToTextRequestSerializationTest()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "ImageToTextTask",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["comment"] = "commentTest",
                ["body"] = "bodyBase64Test",
                ["phrase"] = true,
                ["case"] = true,
                ["numeric"] = 2,
                ["math"] = true,
                ["minLength"] = 4,
                ["maxLength"] = 5,
            };
        
            var request = new ImageToTextRequest()
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                Comment = expectedPayload["comment"]!.ToString(),
                BodyBase64 = expectedPayload["body"]!.ToString(),
                Phrase = bool.Parse(expectedPayload["phrase"]!.ToString()),
                Case = bool.Parse(expectedPayload["case"]!.ToString()),
                Numeric = (NumericOption)int.Parse(expectedPayload["numeric"]!.ToString()),
                Math = bool.Parse(expectedPayload["math"]!.ToString()),
                MinLength = int.Parse(expectedPayload["minLength"]!.ToString()),
                MaxLength = int.Parse(expectedPayload["maxLength"]!.ToString()),
            };
        
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }



        [Fact]
        public void ImageToTextRequestSerializationTest_OnlyRequiredArguments()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "ImageToTextTask",
                ["body"] = "bodyBase64Test",
            };
        
            var request = new ImageToTextRequest()
            {
                BodyBase64 = expectedPayload["body"]!.ToString(),
            };
        
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }


        [Fact]
        public void RecaptchaV2EnterpriseProxylessRequestSerializationTest()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "RecaptchaV2EnterpriseTaskProxyless",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["websiteKey"] = "69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["apiDomain"] = "apiDomainTest",
                ["enterprisePayload"] = new JObject()
                {
                    ["enterprisePayloadParam1"] = "enterprisePayloadParam1Value",
                    ["enterprisePayloadParam2"] = "enterprisePayloadParam2Value"
                }
            };
        
            var request = new RecaptchaV2EnterpriseProxylessRequest()
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                WebsiteKey = expectedPayload["websiteKey"]!.ToString(),
                ApiDomain = expectedPayload["apiDomain"]!.ToString(),
                EnterprisePayload = new Dictionary<string, string>()
                {
                    ["enterprisePayloadParam1"] = "enterprisePayloadParam1Value",
                    ["enterprisePayloadParam2"] = "enterprisePayloadParam2Value"
                }
            };
        
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }


        [Fact]
        public void RecaptchaV2EnterpriseProxylessRequestSerializationTest_OnlyRequiredArguments()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "RecaptchaV2EnterpriseTaskProxyless",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["websiteKey"] = "69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC"
            };

            var request = new RecaptchaV2EnterpriseProxylessRequest()
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                WebsiteKey = expectedPayload["websiteKey"]!.ToString(),
            };
        
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }


        [Fact]
        public void RecaptchaV2EnterpriseRequestSerializationTest()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "RecaptchaV2EnterpriseTask",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["websiteKey"] = "69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["apiDomain"] = "apiDomainTest",
                ["enterprisePayload"] = new JObject()
                {
                    ["enterprisePayloadParam1"] = "enterprisePayloadParam1Value",
                    ["enterprisePayloadParam2"] = "enterprisePayloadParam2Value"
                },
                ["proxyType"] = "socks4",
                ["proxyAddress"] = "proxyAddressTest",
                ["proxyPort"] = 1080,
                ["proxyLogin"] = "proxyLoginTest",
                ["proxyPassword"] = "proxyPasswordTest",
                ["userAgent"] = "userAgentTest",
                ["cookies"] = "cookiesTest",
            };
        
            var request = new RecaptchaV2EnterpriseRequest()
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                WebsiteKey = expectedPayload["websiteKey"]!.ToString(),
                ApiDomain = expectedPayload["apiDomain"]!.ToString(),
                EnterprisePayload = new Dictionary<string, string>()
                {
                    ["enterprisePayloadParam1"] = "enterprisePayloadParam1Value",
                    ["enterprisePayloadParam2"] = "enterprisePayloadParam2Value"
                },
                ProxyConfig = new ProxyConfig
                {
                    ProxyType = ProxyTypeOption.Socks4,
                    ProxyAddress = expectedPayload["proxyAddress"]!.ToString(),
                    ProxyLogin = expectedPayload["proxyLogin"]!.ToString(),
                    ProxyPassword = expectedPayload["proxyPassword"]!.ToString(),
                    ProxyPort = int.Parse(expectedPayload["proxyPort"]!.ToString()),
                },
                UserAgent = expectedPayload["userAgent"]!.ToString(),
                Cookies = expectedPayload["cookies"]!.ToString(),
            };
        
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }


        [Fact]
        public void RecaptchaV2EnterpriseRequestSerializationTest_OnlyRequiredArguments()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "RecaptchaV2EnterpriseTask",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["websiteKey"] = "69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["proxyType"] = "socks4",
                ["proxyAddress"] = "proxyAddressTest",
                ["proxyPort"] = 1080,
                ["userAgent"] = "userAgentTest",
            };
        
            var request = new RecaptchaV2EnterpriseRequest()
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                WebsiteKey = expectedPayload["websiteKey"]!.ToString(),
                ProxyConfig = new ProxyConfig
                {
                    ProxyType = ProxyTypeOption.Socks4,
                    ProxyAddress = expectedPayload["proxyAddress"]!.ToString(),
                    ProxyPort = int.Parse(expectedPayload["proxyPort"]!.ToString()),
                },
                UserAgent = expectedPayload["userAgent"]!.ToString(),
            };
        
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }

        [Fact]
        public void RecaptchaV2ProxylessRequestSerializationTest()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "RecaptchaV2TaskProxyless",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["websiteKey"] = "69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["recaptchaDataSValue"] = "recaptchaDataSValueTest",
                ["isInvisible"] = true,
            };
        
            var request = new RecaptchaV2ProxylessRequest()
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                WebsiteKey = expectedPayload["websiteKey"]!.ToString(),
                RecaptchaDataSValue = expectedPayload["recaptchaDataSValue"]!.ToString(),
                IsInvisible = bool.Parse(expectedPayload["isInvisible"]!.ToString()),
            };
        
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }

        [Fact]
        public void RecaptchaV2ProxylessRequestSerializationTest_OnlyRequiredArguments()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "RecaptchaV2TaskProxyless",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["websiteKey"] = "69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
            };
        
            var request = new RecaptchaV2ProxylessRequest()
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                WebsiteKey = expectedPayload["websiteKey"]!.ToString(),
            };
        
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }
    
        [Fact]
        public void TurnstileProxylessRequestSerializationTest()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "TurnstileTaskProxyless",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["websiteKey"] = "69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC"
            };
        
            var request = new TurnstileCaptchaProxylessRequest()
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                WebsiteKey = expectedPayload["websiteKey"]!.ToString()
            };
        
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }
    
        [Fact]
        public void TurnstileRequestSerializationTest()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "TurnstileTask",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["websiteKey"] = "69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["proxyType"] = "socks4",
                ["proxyAddress"] = "proxyAddressTest",
                ["proxyPort"] = 1080,
                ["proxyLogin"] = "proxyLoginTest",
                ["proxyPassword"] = "proxyPasswordTest",
                ["userAgent"] = "userAgentTest",
            };
        
            var request = new TurnstileCaptchaRequest()
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                WebsiteKey = expectedPayload["websiteKey"]!.ToString(),
                ProxyConfig = new ProxyConfig
                {
                    ProxyType = ProxyTypeOption.Socks4,
                    ProxyAddress = expectedPayload["proxyAddress"]!.ToString(),
                    ProxyLogin = expectedPayload["proxyLogin"]!.ToString(),
                    ProxyPassword = expectedPayload["proxyPassword"]!.ToString(),
                    ProxyPort = int.Parse(expectedPayload["proxyPort"]!.ToString()),
                },
                UserAgent = expectedPayload["userAgent"]!.ToString(),
            };
        
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }


        [Fact]
        public void TurnstileRequestSerializationTest_OnlyRequiredArguments()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "TurnstileTask",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["websiteKey"] = "69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["proxyType"] = "socks4",
                ["proxyAddress"] = "proxyAddressTest",
                ["proxyPort"] = 1080,
                ["userAgent"] = "userAgentTest",
            };
        
            var request = new TurnstileCaptchaRequest()
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                WebsiteKey = expectedPayload["websiteKey"]!.ToString(),
                ProxyConfig = new ProxyConfig
                {
                    ProxyType = ProxyTypeOption.Socks4,
                    ProxyAddress = expectedPayload["proxyAddress"]!.ToString(),
                    ProxyPort = int.Parse(expectedPayload["proxyPort"]!.ToString()),
                },
                UserAgent = expectedPayload["userAgent"]!.ToString(),
            };
        
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }

        [Fact]
        public void RecaptchaV3RequestSerializationTest_OnlyRequiredArguments()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "RecaptchaV3TaskProxyless",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["websiteKey"] = "69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["minScore"] = 0.3,
            };
        
            var request = new RecaptchaV3Request()
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                WebsiteKey = expectedPayload["websiteKey"]!.ToString(),
                MinScore = decimal.Parse(expectedPayload["minScore"]!.ToString()),
            };
        
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }

        [Fact]
        public void RecaptchaV3RequestSerializationTest()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "RecaptchaV3TaskProxyless",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["websiteKey"] = "69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["apiDomain"] = "apiDomainTest",
                ["pageAction"] = "pageActionTest",
                ["minScore"] = 0.3,
                ["isEnterprise"] = false,
            };
        
            var request = new RecaptchaV3Request()
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                WebsiteKey = expectedPayload["websiteKey"]!.ToString(),
                ApiDomain = expectedPayload["apiDomain"]!.ToString(),
                PageAction = expectedPayload["pageAction"]!.ToString(),
                MinScore = decimal.Parse(expectedPayload["minScore"]!.ToString()),
                IsEnterprise = bool.Parse(expectedPayload["isEnterprise"]!.ToString()),
            };
        
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }

        [Fact]
        public void RecaptchaV3EnterpriseRequestSerializationTest()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "RecaptchaV3TaskProxyless",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["websiteKey"] = "69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["apiDomain"] = "apiDomainTest",
                ["pageAction"] = "pageActionTest",
                ["minScore"] = 0.3,
                ["isEnterprise"] = true,
            };
        
            var request = new RecaptchaV3EnterpriseRequest()
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                WebsiteKey = expectedPayload["websiteKey"]!.ToString(),
                ApiDomain = expectedPayload["apiDomain"]!.ToString(),
                PageAction = expectedPayload["pageAction"]!.ToString(),
                MinScore = decimal.Parse(expectedPayload["minScore"]!.ToString()),
            };
        
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }

        [Fact]
        public void RecaptchaV3EnterpriseRequestSerializationTest_OnlyRequiredArguments()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "RecaptchaV3TaskProxyless",
                ["websiteURL"] = "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["websiteKey"] = "69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
                ["minScore"] = 0.3,
                ["isEnterprise"] = true,
            };
        
            var request = new RecaptchaV3EnterpriseRequest()
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                WebsiteKey = expectedPayload["websiteKey"]!.ToString(),
                MinScore = decimal.Parse(expectedPayload["minScore"]!.ToString()),
            };
        
            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }

        [Fact]
        public void AmazonWafProxylessRequestSerializationTest()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "AmazonTaskProxyless",
                ["websiteURL"] = "https://example.com/waf",
                ["websiteKey"] = "AQIDAHjcYu/GjX+QlghicBgQ/7bFaQZ+m5FKCMDnO+vTbNg96AF5H1K/siwSLK7RfstKtN5bAAAAfjB8BgkqhkiG9w0BBwagbzBtAgEAMGgGCSqGSIb3DQEHATAeBglghkgBZQMEAS4wEQQMLp9uHaj",
                ["iv"] = "CgAHbCe2GgAAAAAj",
                ["context"] = "9BUgmlm48F92WUoqv97a49ZuEJJ50TCk9MVr3C7WMtQ0X6flVbufM4n8mjFLmbLVAPgaQ1Jydeaja94iAS49ljb",
                ["captchaScript"] = "https://example.token.awswaf.com/captcha.js",
                ["challengeScript"] = "https://example.token.awswaf.com/challenge.js",
            };

            var request = new AmazonWafProxylessRequest
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                WebsiteKey = expectedPayload["websiteKey"]!.ToString(),
                Iv = expectedPayload["iv"]!.ToString(),
                Context = expectedPayload["context"]!.ToString(),
                CaptchaScript = expectedPayload["captchaScript"]!.ToString(),
                ChallengeScript = expectedPayload["challengeScript"]!.ToString(),
            };

            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }

        [Fact]
        public void AmazonWafRequestSerializationTest()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "AmazonTask",
                ["websiteURL"] = "https://example.com/waf",
                ["websiteKey"] = "AQIDAHjcYu/GjX+QlghicBgQ/7bFaQZ+m5FKCMDnO+vTbNg96AF5H1K",
                ["iv"] = "CgAHbCe2GgAAAAAj",
                ["context"] = "9BUgmlm48F92WUoqv97a49ZuEJJ50TCk9MVr3C7WMtQ0X6flVbufM4n8mjFLmbLVAPgaQ1Jydeaja94iAS49ljb",
                ["proxyType"] = "http",
                ["proxyAddress"] = "proxyAddressTest",
                ["proxyPort"] = 1080,
                ["proxyLogin"] = "proxyLoginTest",
                ["proxyPassword"] = "proxyPasswordTest",
            };

            var request = new AmazonWafRequest
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                WebsiteKey = expectedPayload["websiteKey"]!.ToString(),
                Iv = expectedPayload["iv"]!.ToString(),
                Context = expectedPayload["context"]!.ToString(),
                ProxyConfig = new ProxyConfig
                {
                    ProxyType = ProxyTypeOption.Http,
                    ProxyAddress = expectedPayload["proxyAddress"]!.ToString(),
                    ProxyLogin = expectedPayload["proxyLogin"]!.ToString(),
                    ProxyPassword = expectedPayload["proxyPassword"]!.ToString(),
                    ProxyPort = int.Parse(expectedPayload["proxyPort"]!.ToString()),
                }
            };

            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }

        [Fact]
        public void AntiGateRequestSerializationTest()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "AntiGateTask",
                ["websiteURL"] = "https://anti-captcha.com/tutorials/v2-textarea",
                ["templateName"] = "Sign-in and wait for control text",
                ["variables"] = new JObject
                {
                    ["login_input_css"] = "#login",
                    ["login_input_value"] = "the login",
                    ["nested"] = new JObject { ["Key_With_Case"] = 1 },
                },
                ["domainsOfInterest"] = new JArray("anti-captcha.com", "example.com"),
                ["proxyAddress"] = "1.2.3.4",
                ["proxyPort"] = 3128,
                ["proxyLogin"] = "proxyLoginTest",
                ["proxyPassword"] = "proxyPasswordTest",
            };

            var request = new AntiGateRequest
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                TemplateName = expectedPayload["templateName"]!.ToString(),
                Variables = (JObject)expectedPayload["variables"]!.DeepClone(),
                DomainsOfInterest = new List<string> { "anti-captcha.com", "example.com" },
                ProxyConfig = new ProxyConfig
                {
                    ProxyType = ProxyTypeOption.Http,
                    ProxyAddress = expectedPayload["proxyAddress"]!.ToString(),
                    ProxyPort = 3128,
                    ProxyLogin = expectedPayload["proxyLogin"]!.ToString(),
                    ProxyPassword = expectedPayload["proxyPassword"]!.ToString(),
                }
            };

            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }

        [Fact]
        public void AntiGateRequestSerializationTest_OnlyRequiredArguments()
        {
            var expectedPayload = new JObject
            {
                ["type"] = "AntiGateTask",
                ["websiteURL"] = "https://anti-captcha.com/tutorials/v2-textarea",
                ["templateName"] = "Wait for control appearance",
                ["variables"] = new JObject { ["css_selector"] = "#x" },
            };

            var request = new AntiGateRequest
            {
                WebsiteUrl = expectedPayload["websiteURL"]!.ToString(),
                TemplateName = expectedPayload["templateName"]!.ToString(),
                Variables = JObject.FromObject(new { css_selector = "#x" }),
            };

            var Payload = CreateCaptchaRequestHelper.Build(request);
            AssertSame(expectedPayload, Payload);
        }

        [Fact]
        public void AntiGateRequest_CopyConstructor_CopiesAllProperties()
        {
            var source = new AntiGateRequest
            {
                WebsiteUrl = "https://example.com",
                TemplateName = "template",
                Variables = new JObject { ["a"] = "b" },
                DomainsOfInterest = new List<string> { "example.com" },
                ProxyConfig = new ProxyConfig { ProxyAddress = "1.2.3.4", ProxyPort = 80 },
            };

            var copy = new AntiGateRequest(source);

            Assert.Equal(source.WebsiteUrl, copy.WebsiteUrl);
            Assert.Equal(source.TemplateName, copy.TemplateName);
            Assert.Same(source.Variables, copy.Variables);
            Assert.Same(source.DomainsOfInterest, copy.DomainsOfInterest);
            Assert.Same(source.ProxyConfig, copy.ProxyConfig);
        }

    }
}
