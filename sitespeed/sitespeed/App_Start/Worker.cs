using sitespeed.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace sitespeed
{
    public class Worker: IDisposable
    {
        CookieWebClient _webClient;
        Uri _uri;
        Stopwatch _stopwatch;
        int _count = 0;
        public Dictionary<int, string> Timing { get; set; }
        public List<string> Xmls { get; set; }

        public Worker(Uri uri)
        {
            this._webClient = new CookieWebClient();
            this.Timing = new Dictionary<int, string>();
            this.Xmls = new List<string>();
            this._stopwatch = new Stopwatch();
            this._uri = uri;
        }

        string GetTimeFormat(Stopwatch st)
        {
            return String.Format("{0}.{1}", st.Elapsed.Seconds.ToString(), st.Elapsed.Milliseconds.ToString());
        }
        byte[] GetRequst(string url)
        {
            this._stopwatch.Start();

            byte[] data = this._webClient.DownloadData(url);

            this._stopwatch.Stop();
            Timing.Add(_count++, this.GetTimeFormat(this._stopwatch));
            return data;
        }
        string GetRequstFile(string url)
        {
            string textResponse = "";
            using (var responseStream = new GZipStream(this._webClient.OpenRead(url), CompressionMode.Decompress))
            {
                using (var reader = new StreamReader(responseStream))
                {
                    textResponse = reader.ReadToEnd();
                }
            }
            return textResponse;
        }
        string GetSitemapDocument(List<SitemapNode> sitemapNodes)
        {
            XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XElement root = new XElement(xmlns + "urlset");
            foreach (SitemapNode sitemapNode in sitemapNodes)
            {
                XElement urlElement = new XElement(
                    xmlns + "url",
                    new XElement(xmlns + "loc", Uri.EscapeUriString(sitemapNode.Url)),
                    sitemapNode.LastModified == null ? null : new XElement(
                        xmlns + "lastmod",
                        sitemapNode.LastModified.Value.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:sszzz")),
                    sitemapNode.Frequency == null ? null : new XElement(
                        xmlns + "changefreq",
                        sitemapNode.Frequency.Value.ToString().ToLowerInvariant()),
                    sitemapNode.Priority == null ? null : new XElement(
                        xmlns + "priority",
                        sitemapNode.Priority.Value.ToString("F1", CultureInfo.InvariantCulture)));
                root.Add(urlElement);
            }
            XDocument document = new XDocument(root);
            return document.ToString();
        }
        public void Work()
        {
            try
            {
                //robots.txt
                string fUrl = string.Format("{0}://{1}/robots.txt", _uri.Scheme, _uri.Host);

                byte[] data = this.GetRequst(fUrl);

                string res = Encoding.UTF8.GetString(data);
                Debug.Print(res);
                //Sitemap:
                if (res.Contains("Sitemap"))
                {
                    int startInd = res.IndexOf("Sitemap: ") + 9;
                    string smUrl = res.Substring(startInd);
                    XmlDocument xml = new XmlDocument();

                    if (smUrl.IndexOf(".gz") > 0)
                    {

                        res = this.GetRequstFile(smUrl);
                        this.Xmls.Add(res);
                        xml.LoadXml(res);
                        XmlNodeList lnodes = xml.GetElementsByTagName("loc");
                        XmlNodeList nwnodes;
                        string nwres = "";
                        XmlDocument nwxml = new XmlDocument();
                        foreach (XmlNode item in lnodes)
                        {
                            Debug.Print(item.InnerText);
                            if (item.InnerText.IndexOf(".gz") > 0)
                            {
                                nwres = this.GetRequstFile(item.InnerText);
                                this.Xmls.Add(nwres);
                                nwxml.LoadXml(nwres);
                                nwnodes = nwxml.GetElementsByTagName("loc");
                                foreach (XmlNode next in nwnodes)
                                {
                                    data = this.GetRequst(next.InnerText);
                                }
                            }
                            else
                            {
                                //xml
                                data = this.GetRequst(item.InnerText);
                                nwres = Encoding.UTF8.GetString(data);
                                this.Xmls.Add(nwres);
                                nwxml.LoadXml(nwres);
                                nwnodes = nwxml.GetElementsByTagName("loc");
                                foreach (XmlNode next in nwnodes)
                                {
                                    data = this.GetRequst(item.InnerText);
                                }
                            }
                        }
                    }
                    else
                    {
                        //xml
                        data = this.GetRequst(smUrl);
                        res = Encoding.UTF8.GetString(data);
                        this.Xmls.Add(res);
                        xml.LoadXml(res);
                        var lnodes = xml.GetElementsByTagName("loc");
                        foreach (XmlNode item in lnodes)
                        {
                            data = this.GetRequst(item.InnerText);
                        }
                    }
                }
                else
                {
                    fUrl = string.Format("{0}://{1}/sitemap.xml", _uri.Scheme, _uri.Host);

                    data = this.GetRequst(fUrl);
                    res = Encoding.UTF8.GetString(data);
                    this.Xmls.Add(res);
                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml(res);
                    var lnodes = xml.GetElementsByTagName("loc");
                    foreach (XmlNode item in lnodes)
                    {
                        data = this.GetRequst(item.InnerText);
                    }



                }
            }
            catch (Exception)
            {
                this._stopwatch.Stop();
                throw;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    this._webClient.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Worker() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}