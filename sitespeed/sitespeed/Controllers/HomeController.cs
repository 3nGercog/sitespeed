using Newtonsoft.Json;
using sitespeed.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Xml.Linq;

namespace sitespeed.Controllers
{

    public static class  UrlExt
    {
        public static string AbsoluteRouteUrl(
            this UrlHelper urlHelper,
            string routeName,
            object routeValues = null)
        {
            string scheme = urlHelper.RequestContext.HttpContext.Request.Url.Scheme;
            return urlHelper.RouteUrl(routeName, routeValues, scheme);
        }
    }
    public class HomeController : Controller
    {
        List<SitemapNode> sitenodes = new List<SitemapNode>();
        List<History> history = new List<History>();
        public ActionResult Index()
        {
            string fpath = Server.MapPath("~/App_Data/somedata.json");
            if (!System.IO.File.Exists(fpath))
            {
                return View();
            }
            using (StreamReader sr = System.IO.File.OpenText(fpath))
            {
                string s = "";
                while ((s = sr.ReadLine()) != null)
                {
                    Debug.WriteLine(s);
                    var h = JsonConvert.DeserializeObject<History>(s);
                    history.Add(h);
                }
            }
            ViewData = new ViewDataDictionary(history.OrderBy(h => h.Time));
            return View();
        }

        public ActionResult Create()
        {
            return RedirectToAction("Index");
        }

        // POST: SiteNodes/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here
                var url = collection[0];
                if (String.IsNullOrEmpty(url))
                    return RedirectToAction("Index");

                string fpath = Server.MapPath("~/App_Data/somedata.json");
                sitenodes.Add(new SitemapNode()
                {
                    Url = url,
                    Priority = 1,
                    LastModified = DateTime.Now,
                    Frequency = SitemapFrequency.Always
                });
                string xml = GetSitemapDocument(sitenodes);
                for (int i = 0; i < 5; i++)
                {
                    var time = this.CalcSpeed(url);
                    var sthist = new History()
                    {
                        SiteNode = new SitemapNode()
                        {
                            Url = url,
                            Priority = 1,
                            LastModified = DateTime.Now,
                            Frequency = SitemapFrequency.Always
                        },
                        Time = time,
                        Number = i,
                        Xml = xml
                    };
                    var str = JsonConvert.SerializeObject(sthist);
                    if (!System.IO.File.Exists(fpath))
                    {
                        using (StreamWriter sw = System.IO.File.CreateText(fpath))
                        {
                            sw.WriteLine(str);
                        }
                    }
                    using (StreamWriter sw = System.IO.File.AppendText(fpath))
                    {
                        sw.WriteLine(str);
                    }
                }
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        public string CalcSpeed(string url)
        {
            WebClient wc = new WebClient();
            DateTime dt1 = DateTime.Now;
            var st = new Stopwatch();
            st.Start();
            byte[] data = wc.DownloadData(url);
            st.Stop();
            DateTime dt2 = DateTime.Now;
            //return (dt2 - dt1).TotalSeconds;
            return String.Format("{0}.{1}", st.Elapsed.Seconds.ToString(), st.Elapsed.Milliseconds.ToString());
        }
        public string GetSitemapDocument(List<SitemapNode> sitemapNodes)
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

        protected string GetUrl(object routeValues)
        {
            RouteValueDictionary values = new RouteValueDictionary(routeValues);
            RequestContext context = new RequestContext(HttpContext, RouteData);

            string url = RouteTable.Routes.GetVirtualPath(context, values).VirtualPath;

            return new Uri(Request.Url, url).AbsoluteUri;
        }
    }
}