using System.Collections.Generic;
namespace Autosalon_OneZone.ViewModels.Admin
{
    public class RecenzijaListViewModel
    {
        public string? SearchQuery { get; set; }
    }

    public sealed record AdminFilterOption(string Value, string Label);
}
