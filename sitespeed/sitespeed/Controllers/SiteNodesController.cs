using sitespeed.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

namespace sitespeed.Controllers
{
    public class SiteNodesController : Controller
    {
        List<SitemapNode> sitenodes = new List<SitemapNode>();
        // GET: SiteNodes
        public ActionResult Index()
        {
            var history = new List<History>();
            foreach (var item in sitenodes)
            {
                history.Add(new History()
                {
                    SiteNode = item
                });
            }
            ViewData = new ViewDataDictionary(history);
            return View();
        }

        // GET: SiteNodes/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: SiteNodes/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: SiteNodes/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here
                this.AddSitemapNodes(collection[0]);
                //string xml = GetSitemapDocument(sitenodes);
                //return this.Content(xml, "xml", Encoding.UTF8);
                Debug.Write(collection[0]);
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        public void AddSitemapNodes(string urlHelper)
        {
            sitenodes.Add(
                new SitemapNode()
                {
                    Url = urlHelper,
                    Priority = 1
                });
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

        // GET: SiteNodes/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: SiteNodes/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: SiteNodes/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: SiteNodes/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
