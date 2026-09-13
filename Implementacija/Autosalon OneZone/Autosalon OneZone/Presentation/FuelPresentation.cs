using Autosalon_OneZone.Models;

namespace Autosalon_OneZone.Presentation;

public static class FuelPresentation
{
    public static string GetResourceKey(TipGoriva fuel) => fuel switch
    {
        TipGoriva.Benzin => "FuelGasoline",
        TipGoriva.Dizel => "FuelDiesel",
        TipGoriva.Plin => "FuelGas",
        TipGoriva.Elektro => "FuelElectric",
        TipGoriva.Hibrid => "FuelHybrid",
        _ => fuel.ToString()
    };

    public static string GetChipClass(TipGoriva fuel) => fuel switch
    {
        TipGoriva.Benzin => "is-petrol",
        TipGoriva.Dizel => "is-diesel",
        TipGoriva.Plin => "is-gas",
        TipGoriva.Elektro => "is-electric",
        TipGoriva.Hibrid => "is-hybrid",
        _ => "is-default"
    };
}
