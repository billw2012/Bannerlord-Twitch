using System;
using System.Collections.Generic;
using System.Linq;
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
 
         public delegate KeyValuePair<int, string> RedirectHandler(string payload, string clientId, string accessToken, Func<KeyValuePair<int, string>> realCall, Dictionary<string, string[]> urlParams);
 
         private readonly List<(string url, string method, RedirectHandler handler)> redirects = new();
         
         public void AddRedirect(ApiVersion api, string resource, string method, RedirectHandler handler)
         {
             string fullUrl = (api == ApiVersion.Helix ? BaseHelix : BaseV5) + (resource.StartsWith("/") ? "" : "/") + resource;
             redirects.Add((fullUrl, method, handler));
         }
 
         private KeyValuePair<int, string>? DoRedirect(string url, string method, string payload, ApiVersion api = ApiVersion.V5,
             string clientId = null, string accessToken = null)
         {
             string strippedUrl = url.Split('?').First();
             return redirects.FirstOrDefault(r => strippedUrl == r.url && r.method == method)
                 .handler?.Invoke(payload, clientId, accessToken, () => httpCallHandlerImplementation.GeneralRequest(url, method, payload, api, clientId, accessToken), ParseUrlParameters(url));
         }
         
         public KeyValuePair<int, string> GeneralRequest(
             string url, string method, string payload = null, ApiVersion api = ApiVersion.V5,
             string clientId = null, string accessToken = null)
         {
             return DoRedirect(url, method, payload, api, clientId, accessToken)
                 ?? httpCallHandlerImplementation.GeneralRequest(url, method, payload, api, clientId, accessToken);
         }
 
         public void PutBytes(string url, byte[] payload)
         {
             httpCallHandlerImplementation.PutBytes(url, payload);
         }
 
         public int RequestReturnResponseCode(string url, string method, List<KeyValuePair<string, string>> getParams = null)
         {
             return httpCallHandlerImplementation.RequestReturnResponseCode(url, method, getParams);
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