using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Core.Interfaces;
using TwitchLib.Api.Core.Internal;

namespace BannerlordTwitch
{
    /// <summary>Main HttpClient used to call the Twitch API</summary>
    public class CustomTwitchHttpClient : IHttpCallHandler
    {
        private readonly HttpClient _http;

        public CustomTwitchHttpClient()
        {
            this._http = new HttpClient(new TwitchHttpClientHandler(null));
        }

        /// <summary>PUT Request with a byte array body</summary>
        /// <param name="url">URL to direct the PUT request at</param>
        /// <param name="payload">Payload to send with the request</param>
        /// <returns>Task for the request</returns>
        public async Task PutBytesAsync(string url, byte[] payload)
        {
            HttpResponseMessage errorResp = await this._http.PutAsync(new Uri(url), (HttpContent) new ByteArrayContent(payload)).ConfigureAwait(false);
            if (errorResp.IsSuccessStatusCode)
                return;
            this.HandleWebException(errorResp);
        }

        /// <summary>
        /// Used to make API calls to the Twitch API varying by Method, URL and payload
        /// </summary>
        /// <param name="url">URL to call</param>
        /// <param name="method">HTTP Method to use for the API call</param>
        /// <param name="payload">Payload to send with the API call</param>
        /// <param name="api">Which API version is called</param>
        /// <param name="clientId">Twitch ClientId</param>
        /// <param name="accessToken">Twitch AccessToken linked to the ClientId</param>
        /// <returns>KeyValuePair with the key being the returned StatusCode and the Value being the ResponseBody as string</returns>
        /// <exception cref="T:TwitchLib.Api.Core.Exceptions.InvalidCredentialException"></exception>
        public async Task<KeyValuePair<int, string>> GeneralRequestAsync(
            string url,
            string method,
            string payload = null,
            ApiVersion api = ApiVersion.Helix,
            string clientId = null,
            string accessToken = null)
        {
            HttpRequestMessage request = new HttpRequestMessage()
            {
                RequestUri = new Uri(url),
                Method = new HttpMethod(method)
            };
            if (api == ApiVersion.Helix)
            {
                if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(accessToken))
                    throw new InvalidCredentialException("A Client-Id and OAuth token is required to use the Twitch API.");
                request.Headers.Add("Client-ID", clientId);
            }
            string str = "OAuth";
            if (api == ApiVersion.Helix || api == ApiVersion.Auth)
            {
                request.Headers.Add(HttpRequestHeader.Accept.ToString(), "application/json");
                str = "Bearer";
            }
            if (!string.IsNullOrWhiteSpace(accessToken))
                request.Headers.Add(HttpRequestHeader.Authorization.ToString(), str + " " + TwitchLib.Api.Core.Common.Helpers.FormatOAuth(accessToken));
            if (payload != null)
                request.Content = (HttpContent) new StringContent(payload, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await this._http.SendAsync(request).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
                return new KeyValuePair<int, string>((int) response.StatusCode, await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            this.HandleWebException(response);
            return new KeyValuePair<int, string>(0, (string) null);
        }

        public async Task<int> RequestReturnResponseCodeAsync(
            string url,
            string method,
            List<KeyValuePair<string, string>> getParams = null)
        {
            if (getParams != null)
            {
                for (int index = 0; index < getParams.Count; ++index)
                {
                    if (index == 0)
                        url = url + "?" + getParams[index].Key + "=" + Uri.EscapeDataString(getParams[index].Value);
                    else
                        url = url + "&" + getParams[index].Key + "=" + Uri.EscapeDataString(getParams[index].Value);
                }
            }
            return (int) (await this._http.SendAsync(new HttpRequestMessage()
            {
                RequestUri = new Uri(url),
                Method = new HttpMethod(method)
            }).ConfigureAwait(false)).StatusCode;
        }

        private void HandleWebException(HttpResponseMessage errorResp)
        {
            string error;
            try
            {
                error = errorResp.Content?.ReadAsStringAsync().Result ?? "(no details given))";
            }
            catch(Exception ex)
            {
                error = $"(error while attempting to read error response: {ex.Message})";
            }

            throw new HttpRequestException($"Request error code {errorResp.StatusCode}: {error}");

            // switch (errorResp.StatusCode)
            // {
            //     case HttpStatusCode.BadRequest:
            //         throw new BadRequestException("Your request failed because either: \n 1. Your ClientID was invalid/not set. \n 2. Your refresh token was invalid. \n 3. You requested a username when the server was expecting a user ID.");
            //     case HttpStatusCode.Unauthorized:
            //         HttpHeaderValueCollection<AuthenticationHeaderValue> wwwAuthenticate = errorResp.Headers.WwwAuthenticate;
            //         if (wwwAuthenticate == null || wwwAuthenticate.Count <= 0)
            //             throw new BadScopeException("Your request was blocked due to bad credentials (Do you have the right scope for your access token?).");
            //         throw new TokenExpiredException("Your request was blocked due to an expired Token. Please refresh your token and update your API instance settings.");
            //     case HttpStatusCode.Forbidden:
            //         throw new BadTokenException("The token provided in the request did not match the associated user. Make sure the token you're using is from the resource owner (streamer? viewer?)");
            //     case HttpStatusCode.NotFound:
            //         throw new BadResourceException("The resource you tried to access was not valid.");
            //     case (HttpStatusCode) 429:
            //         IEnumerable<string> values;
            //         errorResp.Headers.TryGetValues("Ratelimit-Reset", out values);
            //         throw new TooManyRequestsException("You have reached your rate limit. Too many requests were made", values.FirstOrDefault<string>());
            //     case HttpStatusCode.InternalServerError:
            //         throw new InternalServerErrorException("The API answered with a 500 Internal Server Error. Please retry your request");
            //     case HttpStatusCode.BadGateway:
            //         throw new BadGatewayException("The API answered with a 502 Bad Gateway. Please retry your request");
            //     case HttpStatusCode.GatewayTimeout:
            //         throw new GatewayTimeoutException("The API answered with a 504 Gateway Timeout. Please retry your request");
            //     default:
            //         throw new HttpRequestException("Something went wrong during the request! Please try again later");
            // }
        }
    }
}