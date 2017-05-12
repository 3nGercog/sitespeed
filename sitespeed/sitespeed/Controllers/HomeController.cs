using Newtonsoft.Json;
using sitespeed.Models;
using sitespeed.ViewModel;
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

        public ActionResult Index()
        {
            List<History> history = new List<History>();
            string fpath = Server.MapPath("~/App_Data/somedata.json");
            if (!System.IO.File.Exists(fpath))
            {
                return View();
            }
            using (StreamReader sr = System.IO.File.OpenText(fpath))
            {
                string s = ""; History h;
                while ((s = sr.ReadLine()) != null)
                {
                    Debug.WriteLine(s);
                    h = JsonConvert.DeserializeObject<History>(s);
                    history.Add(h);
                }
            }
            var grafs = history.GroupBy(h => h.SiteNode.Url).Select(h => new HistoryViewModel() { Url = h.Key, Historys = h.OrderBy(s => s.Time).ToList() }).ToList();
            var tables = history.OrderBy(h => h.SiteNode.Url).ThenBy(h => h.Time).Skip(0).Take(20).ToList();
            ViewData["graf"] = grafs;
            ViewData["table"] = tables;
            return View();
        }

        [HttpPost]
        public PartialViewResult Next(string query, int startIndex, int pageSize)
        {
            List<History> history = new List<History>();
            string fpath = Server.MapPath("~/App_Data/somedata.json");
            if (!System.IO.File.Exists(fpath))
            {
                return PartialView("_TableView");
            }
            using (StreamReader sr = System.IO.File.OpenText(fpath))
            {
                string s = ""; History h;
                while ((s = sr.ReadLine()) != null)
                {
                    Debug.WriteLine(s);
                    h = JsonConvert.DeserializeObject<History>(s);
                    history.Add(h);
                }
            }
            var tables = history.OrderBy(h => h.SiteNode.Url).ThenBy(h => h.Time).Skip(startIndex).Take(pageSize).ToList();
            ViewData["table"] = tables;
            //var page = source.Skip(startIndex).Take(pageSize);
            return PartialView("_TableView");
        }

        public ActionResult Create()
        {
            return RedirectToAction("Index");
        }

        // POST: SiteNodes/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            string fpath = Server.MapPath("~/App_Data/somedata.json");
            try
            {
                // TODO: Add insert logic here
                var url = collection[0];
                if (String.IsNullOrEmpty(url))
                    return RedirectToAction("Index");

                Uri nUrl = new Uri(url);             
                using (Worker w = new Worker(nUrl))
                {
                    w.Work();
                    foreach (KeyValuePair<int, string> kvp in w.Timing)
                    {
                        var sthist = new History()
                        {
                            SiteNode = new SitemapNode()
                            {
                                Url = url,
                                Priority = 1,
                                LastModified = DateTime.Now,
                                Frequency = SitemapFrequency.Always
                            },
                            Time = kvp.Value,
                            Number = kvp.Key
                        };
                        var str = JsonConvert.SerializeObject(sthist);
                        this.SaveFile(fpath, str);
                    }
                }
                return RedirectToAction("Index");
            }
            catch
            {
                Uri u = new Uri(collection[0]);
                var exU = string.Format("{0}://{1}", u.Scheme, u.Host);
                var time = "";
                for (int i = 0; i < 5; i++)
                {
                    time = this.CalcSpeed(exU);
                    var sthist = new History()
                    {
                        SiteNode = new SitemapNode()
                        {
                            Url = exU,
                            Priority = 1,
                            LastModified = DateTime.Now,
                            Frequency = SitemapFrequency.Always
                        },
                        Time = time,
                        Number = i
                    };
                    var str = JsonConvert.SerializeObject(sthist);
                    this.SaveFile(fpath, str);
                }
                return RedirectToAction("Index");
            }
        }
        void SaveFile(string path, string text)
        {
            if (!System.IO.File.Exists(path))
            {
                using (StreamWriter sw = System.IO.File.CreateText(path))
                {
                    sw.WriteLine(text);
                }
            }
            using (StreamWriter sw = System.IO.File.AppendText(path))
            {
                sw.WriteLine(text);
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


        protected string GetUrl(object routeValues)
        {
            RouteValueDictionary values = new RouteValueDictionary(routeValues);
            RequestContext context = new RequestContext(HttpContext, RouteData);

            string url = RouteTable.Routes.GetVirtualPath(context, values).VirtualPath;

            return new Uri(Request.Url, url).AbsoluteUri;
        }
    }
}