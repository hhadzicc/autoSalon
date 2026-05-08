using System.Collections.Generic;

namespace Autosalon_OneZone.Models.ViewModels
{
    public class CartViewModel
    {
        public List<CartItemViewModel> VozilaUKorpi { get; set; }

        public decimal UkupnaCijena { get; set; }
    }
}
