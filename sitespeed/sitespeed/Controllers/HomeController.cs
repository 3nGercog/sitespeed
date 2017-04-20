using sitespeed.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
            return View();
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