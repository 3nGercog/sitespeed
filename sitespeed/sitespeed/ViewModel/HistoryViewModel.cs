using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using sitespeed.Models;

namespace sitespeed.ViewModel
{
    public class HistoryViewModel
    {
        public string Url { get; set; }
        public List<History> Historys { get; set; }
    }
}