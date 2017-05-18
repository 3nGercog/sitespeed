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
using HtmlAgilityPack;

namespace sitespeed
{
    public class Worker: IDisposable
    {
        CookieWebClient _webClient;
        Uri _uri;
        Stopwatch _stopwatch;
        int Count { get; set; }
        public Dictionary<string, string> Timing { get; set; }
        public Dictionary<int, string> Queue { get; set; }
        public List<string> Xmls { get; set; }
        List<string> _urls;
        public Worker(Uri uri)
        {
            this._webClient = new CookieWebClient();
            this.Timing = new Dictionary<string, string>();
            this.Queue = new Dictionary<int, string>();
            this.Xmls = new List<string>();
            this._stopwatch = new Stopwatch();
            this._uri = uri;
            this.Count = 0;
            this._urls = new List<string>();
        }

        string GetTimeFormat(Stopwatch st)
        {
            TimeSpan ts = st.Elapsed;
            return String.Format("{0},{1}", ts.Seconds, ts.Milliseconds / 10);
        }
        byte[] GetRequst(string url)
        {
            this._stopwatch.Start();

            byte[] data = this._webClient.DownloadData(url);

            this._stopwatch.Stop();
            this.Timing.Add(url, this.GetTimeFormat(this._stopwatch));
            this.Queue.Add(Count++, url);
            this._stopwatch.Reset();
            return data;
        }
        HtmlAgilityPack.HtmlDocument GetHtmlDocumentWithRequst(string url)
        {
            HtmlWeb hw = new HtmlWeb();
            this._stopwatch.Start();

            HtmlAgilityPack.HtmlDocument data = hw.Load(url);

            this._stopwatch.Stop();
            this.Timing.Add(url, this.GetTimeFormat(this._stopwatch));
            this.Queue.Add(Count++, url);
            this._stopwatch.Reset();
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


        string GetHostUrl(Uri uri)
        {
            return string.Format("{0}://{1}/", uri.Scheme, uri.Host);
        }
        string GetHostUrl(Uri uri, string path)
        {
            return string.Format("{0}://{1}/{2}", uri.Scheme, uri.Host, path);
        }
        public void Work()
        {
            try
            {
                //robots.txt
                string fUrl = this.GetHostUrl(this._uri, "robots.txt");

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
                    fUrl = this.GetHostUrl(this._uri, "sitemap.xml");

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
        public List<KeyValuePair<string, string>> GetSortedList()
        {
            List<KeyValuePair<string, string>> sortList = this.Timing.ToList();

            sortList.Sort( delegate (KeyValuePair<string, string> pair1, KeyValuePair<string, string> pair2)
                {
                    return pair1.Value.CompareTo(pair2.Value);
                }
            );
            return sortList;
        }
        public void ParseUrl()
        {
            string stU = this.GetHostUrl(this._uri);
            this.AddUrlList(stU);
            this.LoadAndAddAllUrls(this._urls);
        }

        void AddUrlList(string url)
        {
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc = this.GetHtmlDocumentWithRequst(url);
            Uri hrefs; Uri h = new Uri(url);
            foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
            {
                // Get the value of the HREF attribute
                string hrefValue = link.GetAttributeValue("href", string.Empty);
                if (string.IsNullOrEmpty(hrefValue) || hrefValue.Contains(".."))
                {
                    continue;
                }
                bool result = Uri.TryCreate(hrefValue, UriKind.Absolute, out hrefs)
                    && (hrefs.Scheme == Uri.UriSchemeHttp || hrefs.Scheme == Uri.UriSchemeHttps);
                
                if (result)
                {
                    var loadsHrefs = this.GetHostUrl(hrefs);
                    if (hrefs.Host == h.Host)
                    {
                        Debug.Print("----------------Site absolute url " + hrefValue + "   ----------------");
                        this._urls.Add(hrefValue);
                    }
                }
                else
                {
                    var loadsHrefs = this.GetHostUrl(h, hrefValue);
                    Debug.Print("----------------Site relative url " + loadsHrefs + "   ----------------");
                    this._urls.Add(loadsHrefs);
                }
            }
        }
        void LoadAndAddAllUrls(List<string> list)
        {
            if(list.Count == 0)
            {
                return;
            }
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            List<string> tempUrls = new List<string>();
            Uri hrefs; Uri litems; string hrefValue = "";
            foreach (var item in list)
            {
                try
                {
                    doc = this.GetHtmlDocumentWithRequst(item);
                
                    foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
                    {
                        // Get the value of the HREF attribute
                        hrefValue = link.GetAttributeValue("href", string.Empty);
                        if (string.IsNullOrEmpty(hrefValue) || hrefValue.Contains(".."))
                        {
                            continue;
                        }

                        bool result = Uri.TryCreate(hrefValue, UriKind.Absolute, out hrefs)
                                        && (hrefs.Scheme == Uri.UriSchemeHttp || hrefs.Scheme == Uri.UriSchemeHttps);
                        litems = new Uri(item);
                        if (result)
                        {
                            var loadsHrefs = this.GetHostUrl(hrefs);
                            var lItem = this.GetHostUrl(litems);
                            if (hrefs.Host == litems.Host && !this._urls.Contains(hrefValue) && !tempUrls.Contains(loadsHrefs))
                            {
                                Debug.Print("----------------Temp absolute url " + hrefValue + "   ----------------");
                                tempUrls.Add(hrefValue);
                            }
                        }
                        else
                        {
                            
                            var loadsHrefs = this.GetHostUrl(litems, hrefValue);
                            if (!this._urls.Contains(loadsHrefs) && !tempUrls.Contains(loadsHrefs))
                            {
                                Debug.Print("----------------Temp relative url " + loadsHrefs + "   ----------------");
                                tempUrls.Add(loadsHrefs);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex = null;
                    continue;
                }
            }
            if(tempUrls.Count > 0)
            {
                this._urls.AddRange(tempUrls);
                this.LoadAndAddAllUrls(tempUrls);
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