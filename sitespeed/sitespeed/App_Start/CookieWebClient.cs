using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace sitespeed
{
    public class CookieWebClient : WebClient
    {
        private CookieContainer m_container = new CookieContainer();
        public CookieContainer CookieContainer
        {
            get { return this.m_container; }
            set { this.m_container = value; }
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);

            if (request is HttpWebRequest)
            {
                (request as HttpWebRequest).CookieContainer = m_container;
                request.Timeout = 20 * 1000;
                (request as HttpWebRequest).AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }
            return request;
        }
    }
}