using Microsoft.AspNetCore.Mvc.Rendering;
using RentalCar.Domain.Extensions;

namespace RentalCar.Web.Helpers;

/// <summary>
/// <see cref="Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelper.GetEnumSelectList{TEnum}"/> [Flags] enum'ları desteklemez.
/// İlan formundaki vites / yakıt / kasa / çekiş alanları için SelectList üretir (0 = None atlanır).
/// </summary>
public static class FlagsEnumSelectList
{
    public static IEnumerable<SelectListItem> For<TEnum>() where TEnum : struct, Enum
    {
        foreach (var v in Enum.GetValues<TEnum>())
        {
            if (Convert.ToInt64(v) == 0)
                continue;
            yield return new SelectListItem
            {
                Value = v.ToString(),
                Text = ((Enum)(object)v).GetDisplayName()
            };
        }
    }
}
