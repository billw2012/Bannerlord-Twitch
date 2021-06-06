using System;
using System.Net;

namespace BannerlordApi
{
    public class BLTApiUrl
    {
        public string httpMethod;
        public string url;
        public string description;
        public Action<HttpListenerRequest, HttpListenerResponse> onVisit;
        public BLTApiUrl(string httpMethod, string url, string description, Action<HttpListenerRequest, HttpListenerResponse> onVisit)
        {
            this.httpMethod = httpMethod;
            this.url = url;
            this.description = description;
            this.onVisit = onVisit;
        }
    }
}