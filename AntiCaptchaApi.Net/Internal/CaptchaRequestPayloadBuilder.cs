﻿using System;
using System.Collections.Generic;
using System.Linq;
using AntiCaptchaApi.Net.Enums;
using AntiCaptchaApi.Net.Internal.Helpers;
using AntiCaptchaApi.Net.Internal.Validation;
using AntiCaptchaApi.Net.Internal.Validation.Validators;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests;
using AntiCaptchaApi.Net.Requests.Abstractions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace AntiCaptchaApi.Net.Internal;
public class KnownTypesBinder : ISerializationBinder
{
    public IList<Type> KnownTypes { get; set; }

    public Type BindToType(string assemblyName, string typeName)
    {
        return KnownTypes.SingleOrDefault(t => t.Name == typeName);
    }

    public void BindToName(Type serializedType, out string assemblyName, out string typeName)
    {
        assemblyName = null;
        typeName = serializedType.Name;
    }
}

internal static class CaptchaPayloadBuilder
{
    
    private static Func<ValidationResult> GetCaptchaRequestCreationValidator<TSolution>(ICaptchaRequest<TSolution> request) 
        where TSolution : BaseSolution
    {
        var @switch = new Dictionary<Type, Func<ValidationResult>> {
            { typeof(AntiGateRequest), () => new AntiGateRequestValidator().Validate(request as AntiGateRequest) },
            { typeof(FunCaptchaRequest), () => new FunCaptchaRequestValidator().Validate(request as FunCaptchaRequest) },
            { typeof(FunCaptchaProxylessRequest), () => new FunCaptchaProxylessRequestValidator().Validate(request as FunCaptchaProxylessRequest) },
            { typeof(GeeTestV3Request), () => new GeeTestV3RequestValidator().Validate(request as GeeTestV3Request) },
            { typeof(GeeTestV3ProxylessRequest), () => new GeeTestV3ProxylessRequestValidator().Validate(request as GeeTestV3ProxylessRequest) },
            { typeof(GeeTestV4Request), () => new GeeTestV4RequestValidator().Validate(request as GeeTestV4Request) },
            { typeof(GeeTestV4ProxylessRequest), () => new GeeTestV4ProxylessRequestValidator().Validate(request as GeeTestV4ProxylessRequest) },
            { typeof(HCaptchaProxylessRequest), () => new HCaptchaProxylessRequestValidator().Validate(request as HCaptchaProxylessRequest) },
            { typeof(HCaptchaRequest), () => new HCaptchaRequestValidator().Validate(request as HCaptchaRequest) },
            { typeof(ImageToTextRequest), () => new ImageToTextRequestValidator().Validate(request as ImageToTextRequest) },
            { typeof(RecaptchaV2EnterpriseProxylessRequest), () => new RecaptchaV2EnterpriseProxylessRequestValidator().Validate(request as RecaptchaV2EnterpriseProxylessRequest) },
            { typeof(RecaptchaV2EnterpriseRequest), () => new RecaptchaV2EnterpriseRequestValidator().Validate(request as RecaptchaV2EnterpriseRequest) },
            { typeof(RecaptchaV2ProxylessRequest), () => new RecaptchaV2ProxylessRequestValidator().Validate(request as RecaptchaV2ProxylessRequest) },
            { typeof(RecaptchaV2Request), () => new RecaptchaV2RequestValidator().Validate(request as RecaptchaV2Request) },
            { typeof(RecaptchaV3Request), () => new RecaptchaV3RequestValidator().Validate(request as RecaptchaV3Request) },
            { typeof(RecaptchaV3EnterpriseRequest), () => new RecaptchaV3RequestValidator().Validate(request as RecaptchaV3EnterpriseRequest) },
            { typeof(TurnstileCaptchaProxylessRequest), () => new TurnstileProxylessRequestValidator().Validate(request as TurnstileCaptchaProxylessRequest) },
            { typeof(TurnstileCaptchaRequest), () => new TurnstileProxylessRequestValidator().Validate(request as TurnstileCaptchaProxylessRequest) },
        };
        return @switch[request.GetType()];
    }

    internal static ValidationResult Validate<TSolution>(ICaptchaRequest<TSolution> request)  
        where TSolution : BaseSolution
    {
        return GetCaptchaRequestCreationValidator(request).Invoke();
    }

    internal static JObject Build<TSolution>(ICaptchaRequest<TSolution> request)
        where TSolution : BaseSolution
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var jsonSerializer = JsonSerializerHelper.GetJsonSerializer();
        
        var serialized = JObject.FromObject(request, jsonSerializer);
        serialized["type"] = RequestTaskNameHelper.GetTaskName<ICaptchaRequest<TSolution>, TSolution>(request);

        if (request is GeeTestV3ProxylessRequest or GeeTestV3Request)
        {
            serialized["version"] = 3;
        }

        if (request is GeeTestV4ProxylessRequest or GeeTestV4Request)
        {
            serialized["version"] = 4;
        }
        
        if (request is RecaptchaV3EnterpriseRequest)
        {
            serialized["isEnterprise"] = true;
        }

        if (serialized.ContainsKey("proxyConfig"))
        {
            if(!string.IsNullOrEmpty(serialized["proxyConfig"]?["proxyType"]?.ToString()) && request is not AntiGateRequest)
                serialized["proxyType"] = ((ProxyTypeOption)int.Parse(serialized["proxyConfig"]?["proxyType"]?.ToString())).ToString().ToLower();
            
            if(!string.IsNullOrEmpty(serialized["proxyConfig"]?["proxyAddress"]?.ToString()))
                serialized["proxyAddress"] = serialized["proxyConfig"]?["proxyAddress"];
            
            if(!string.IsNullOrEmpty(serialized["proxyConfig"]?["proxyPort"]?.ToString()))
                serialized["proxyPort"] = serialized["proxyConfig"]?["proxyPort"];
            
            if(!string.IsNullOrEmpty(serialized["proxyConfig"]?["proxyLogin"]?.ToString()))
                serialized["proxyLogin"] = serialized["proxyConfig"]?["proxyLogin"];
            
            if(!string.IsNullOrEmpty(serialized["proxyConfig"]?["proxyPassword"]?.ToString()))
                serialized["proxyPassword"] = serialized["proxyConfig"]?["proxyPassword"];

            serialized.Remove("proxyConfig");
        }
        
        return serialized;
    }
}