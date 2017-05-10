using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Web;

namespace sitespeed
{
    public class Worker: IDisposable
    {
        CookieWebClient _webClient;
        Uri _uri;
        Stopwatch _stopwatch;
        int _count = 0;
        public Dictionary<int, string> Timing { get; set; }

        public Worker(Uri uri)
        {
            this._webClient = new CookieWebClient();
            this.Timing = new Dictionary<int, string>();
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
            var responseStream = new GZipStream(this._webClient.OpenRead(url), CompressionMode.Decompress);
            var reader = new StreamReader(responseStream);
            var textResponse = reader.ReadToEnd();
            return textResponse;
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


                    if(smUrl.IndexOf(".gz") > 0)
                    {

                        res = this.GetRequstFile(smUrl);
                    }
                    else
                    {
                        data = this.GetRequst(smUrl);
                        res = Encoding.UTF8.GetString(data);
                    }
                    
                    Debug.Print(res);
                }
                else
                {
                    fUrl = string.Format("{0}://{1}/sitemap.xml", _uri.Scheme, _uri.Host);
                    data = this.GetRequst(fUrl);
                    res = Encoding.UTF8.GetString(data);
                    Debug.Print(res);
                }
                //SitemapIndex
                //urlset
            }
            catch (Exception)
            {

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