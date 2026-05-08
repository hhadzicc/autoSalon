using System.Collections.Generic;
using Autosalon_OneZone.Models;
using System;

namespace Autosalon_OneZone.ViewModels.Admin
{
    public class PodrskaListViewModel
    {
        public List<Podrska> Upiti { get; set; }
        public string SearchQuery { get; set; }
    }
}
