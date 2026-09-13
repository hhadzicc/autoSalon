using Autosalon_OneZone.Models;

namespace Autosalon_OneZone.Presentation;

public static class VehicleColorPresentation
{
    public static string GetResourceKey(TipBoje color) => color switch
    {
        TipBoje.Bijela => "VehicleColorWhite",
        TipBoje.Crna => "VehicleColorBlack",
        TipBoje.Siva => "VehicleColorGray",
        TipBoje.Srebrna => "VehicleColorSilver",
        TipBoje.Zlatna => "VehicleColorGold",
        TipBoje.Bez => "VehicleColorBeige",
        TipBoje.Smedja => "VehicleColorBrown",
        TipBoje.Crvena => "VehicleColorRed",
        TipBoje.Plava => "VehicleColorBlue",
        TipBoje.Zelena => "VehicleColorGreen",
        TipBoje.Zuta => "VehicleColorYellow",
        TipBoje.Narandzasta => "VehicleColorOrange",
        TipBoje.Ljubicasta => "VehicleColorPurple",
        TipBoje.Roza => "VehicleColorPink",
        TipBoje.Visebojna => "VehicleColorMulticolor",
        TipBoje.Ostalo => "VehicleColorOther",
        _ => color.ToString()
    };

    public static string GetSwatchClass(TipBoje color) => color switch
    {
        TipBoje.Bijela => "is-white",
        TipBoje.Crna => "is-black",
        TipBoje.Siva => "is-gray",
        TipBoje.Srebrna => "is-silver",
        TipBoje.Zlatna => "is-gold",
        TipBoje.Bez => "is-beige",
        TipBoje.Smedja => "is-brown",
        TipBoje.Crvena => "is-red",
        TipBoje.Plava => "is-blue",
        TipBoje.Zelena => "is-green",
        TipBoje.Zuta => "is-yellow",
        TipBoje.Narandzasta => "is-orange",
        TipBoje.Ljubicasta => "is-purple",
        TipBoje.Roza => "is-pink",
        TipBoje.Visebojna => "is-multicolor",
        _ => "is-other"
    };
}
