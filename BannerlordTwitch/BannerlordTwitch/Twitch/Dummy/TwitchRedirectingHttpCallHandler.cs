﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.HttpCallHandlers;
using TwitchLib.Api.Core.Interfaces;

namespace BannerlordTwitch.Dummy
{
    public class TwitchRedirectingHttpCallHandler : IHttpCallHandler
     {
         private readonly IHttpCallHandler httpCallHandlerImplementation;
         private const string BaseV5 = "https://api.twitch.tv/kraken";
         private const string BaseHelix = "https://api.twitch.tv/helix";
         private const string BaseOauthToken = "https://id.twitch.tv/oauth2/token";
         
         public TwitchRedirectingHttpCallHandler(
             IHttpCallHandler http = null,
             ILogger<TwitchHttpClient> logger = null)
         {
             httpCallHandlerImplementation = http ?? new TwitchHttpClient(logger);
         }
 
         public delegate Task<KeyValuePair<int, string>> RedirectHandler(string payload, string clientId, string accessToken, Func<Task<KeyValuePair<int, string>>> realCall, Dictionary<string, string[]> urlParams);
 
         private readonly List<(string url, string method, RedirectHandler handler)> redirects = new();
         
         public void AddRedirect(ApiVersion api, string resource, string method, RedirectHandler handler)
         {
             string fullUrl = (api == ApiVersion.Helix ? BaseHelix : BaseV5) + (resource.StartsWith("/") ? "" : "/") + resource;
             redirects.Add((fullUrl, method, handler));
         }
 
         private async Task<KeyValuePair<int, string>?> DoRedirect(string url, string method, string payload, ApiVersion api = ApiVersion.Helix,
             string clientId = null, string accessToken = null)
         {
             string strippedUrl = url.Split('?').First();
             var redirect = redirects.FirstOrDefault(r => strippedUrl == r.url && r.method == method);
             if (redirect == default)
                 return null;
             return await redirect.handler.Invoke(payload, clientId, accessToken, () => httpCallHandlerImplementation.GeneralRequestAsync(url, method, payload, api, clientId, accessToken), ParseUrlParameters(url));
         }
         
         public async Task<KeyValuePair<int, string>> GeneralRequestAsync(
             string url, string method, string payload = null, ApiVersion api = ApiVersion.Helix,
             string clientId = null, string accessToken = null)
         {
             return await DoRedirect(url, method, payload, api, clientId, accessToken)
                 ?? await httpCallHandlerImplementation.GeneralRequestAsync(url, method, payload, api, clientId, accessToken);
         }
 
         public async Task PutBytesAsync(string url, byte[] payload)
         {
             await httpCallHandlerImplementation.PutBytesAsync(url, payload);
         }
         
         public async Task<int> RequestReturnResponseCodeAsync(string url, string method, List<KeyValuePair<string, string>> getParams = null)
         {
             return await httpCallHandlerImplementation.RequestReturnResponseCodeAsync(url, method, getParams);
         }
 
         public static Dictionary<string, string[]> ParseUrlParameters(string url)
         {
             string paramsString = url.Split('?')
                 .Skip(1)
                 .FirstOrDefault();
 
             if (paramsString == null)
                 return new();
             
             return HttpUtility.UrlDecode(paramsString)
                 .Split('&')
                 .Select(kv => kv.Split('='))
                 .Select(kv => (key: kv[0], value: kv[1]))
                 .GroupBy(kv => kv.key)
                 .ToDictionary(kv => kv.Key, kv => kv.Select(x => x.value).ToArray());
         }
         
     }
}