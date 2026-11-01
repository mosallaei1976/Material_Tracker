using System.Globalization;

namespace Material_Tracker.Utilities;

public static class PersianText
{
    public static string Digits(string value) => string.Concat(value.Select(character =>
        character is >= '0' and <= '9' ? (char)('۰' + character - '0') : character));

    public static string LatinDigits(string value) => string.Concat(value.Trim().Select(character => character switch
    {
        >= '۰' and <= '۹' => (char)('0' + character - '۰'),
        >= '٠' and <= '٩' => (char)('0' + character - '٠'),
        '٫' => '.',
        _ => character
    }));

    public static string Number(decimal value) => Digits(value.ToString("0.############################", CultureInfo.InvariantCulture)).Replace('.', '٫');

    public static decimal ParseDecimal(string value, string label)
    {
        if (!decimal.TryParse(LatinDigits(value), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out var result))
            throw new Services.SitePackDataException($"{label} را به صورت عدد معتبر وارد کنید.");
        return result;
    }
}
