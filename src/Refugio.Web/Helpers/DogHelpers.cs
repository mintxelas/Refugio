using Microsoft.Extensions.Localization;
using Refugio.Domain.Entities;

namespace Refugio.Web.Helpers;

public static class DogHelpers
{
    public static string DogStatusDisplay(DogStatus s, IStringLocalizer<SharedResources> L) => s switch
    {
        DogStatus.Medical    => L["DogStatus_Medical"].Value,
        DogStatus.Available  => L["DogStatus_Available"].Value,
        DogStatus.Adopted    => L["DogStatus_Adopted"].Value,
        DogStatus.Foster     => L["DogStatus_Foster"].Value,
        DogStatus.Quarantine => L["DogStatus_Quarantine"].Value,
        _                    => s.ToString()
    };

    public static string AgeDisplay(int months, IStringLocalizer<SharedResources> L)
    {
        if (months >= 12)
        {
            var years = months / 12;
            return string.Format(years > 1 ? L["DogDetail_Years"].Value : L["DogDetail_Year"].Value, years);
        }
        return string.Format(L["DogDetail_Months"].Value, months);
    }

    public static string StatusChipClass(DogStatus s) => s switch
    {
        DogStatus.Medical    => "bg-error text-on-error",
        DogStatus.Adopted    => "bg-secondary-container text-on-secondary-container",
        DogStatus.Foster     => "bg-secondary-fixed text-on-secondary-fixed",
        DogStatus.Quarantine => "bg-tertiary text-on-tertiary",
        _                    => "bg-primary-fixed text-on-primary-fixed"
    };

    public static string StatusIcon(DogStatus s) => s switch
    {
        DogStatus.Medical => "medical_services",
        DogStatus.Adopted => "favorite",
        DogStatus.Foster  => "home",
        _                 => "pets"
    };
}
