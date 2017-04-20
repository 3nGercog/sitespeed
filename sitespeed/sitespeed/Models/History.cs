using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace sitespeed.Models
{
    public class History
    {
        public SitemapNode SiteNode { get; set; }
        public TimeSpan Time { get; set; }
    }
}