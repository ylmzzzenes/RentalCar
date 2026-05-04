using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RentalCar.Domain.Entities;
using RentalCar.Domain.Enums;
using RentalCar.Infrastructure.Persistence.Context;

namespace RentalCar.ImportTool;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var excelPath = args.Length > 0
            ? args[0]
            : @"c:\Users\enesy\Downloads\son_120_arac_final (2).xlsx";

        var solutionRelativeSettings = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "RentalCar.Web", "appsettings.json"));
        var settingsPath = args.Length > 1 ? args[1] : solutionRelativeSettings;

        if (!File.Exists(excelPath))
        {
            Console.Error.WriteLine($"Excel bulunamadı: {excelPath}");
            return 1;
        }

        if (!File.Exists(settingsPath))
        {
            Console.Error.WriteLine($"appsettings bulunamadı: {settingsPath}");
            return 1;
        }

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(settingsPath, optional: false)
            .Build();

        var cs = configuration.GetConnectionString("RentalCarDb");
        if (string.IsNullOrWhiteSpace(cs))
        {
            Console.Error.WriteLine("ConnectionStrings:RentalCarDb tanımsız.");
            return 1;
        }

        var options = new DbContextOptionsBuilder<RentalCarContext>()
            .UseSqlServer(cs)
            .Options;

        await using var db = new RentalCarContext(options);

        Console.WriteLine("Mevcut satın alma kayıtları siliniyor…");
        db.Purchases.RemoveRange(db.Purchases);
        await db.SaveChangesAsync();

        Console.WriteLine("Mevcut ilanlar (ve bağlı kiralama / favori / yorum / puanlar) siliniyor…");
        db.Cars.RemoveRange(db.Cars);
        await db.SaveChangesAsync();

        using var workbook = new XLWorkbook(excelPath);
        var ws = workbook.Worksheet(1);
        var firstRow = ws.FirstRowUsed()?.RowNumber() ?? 1;
        var headerRow = ws.Row(firstRow);
        var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            var name = cell.GetString().Trim();
            if (!string.IsNullOrEmpty(name))
                colMap[name] = cell.Address.ColumnNumber;
        }

        string? CellStr(IXLRow row, string header)
        {
            if (!colMap.TryGetValue(header, out var col))
                return null;
            var v = row.Cell(col).GetString().Trim();
            return string.IsNullOrEmpty(v) ? null : v;
        }

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? firstRow;
        var imported = 0;
        var errors = new List<string>();

        for (var r = firstRow + 1; r <= lastRow; r++)
        {
            var row = ws.Row(r);
            if (row.IsEmpty()) continue;

            try
            {
                var marka = CellStr(row, "Marka");
                var model = CellStr(row, "Model");
                if (string.IsNullOrWhiteSpace(marka) || string.IsNullOrWhiteSpace(model))
                    continue;

                var listed = ParseDecimal(CellStr(row, "Fiyat")) ?? 0;
                if (listed <= 0)
                {
                    errors.Add($"Satır {r}: geçersiz fiyat");
                    continue;
                }

                var car = new Car
                {
                    Brand = marka.Trim(),
                    CatalogBrand = marka.Trim(),
                    Series = CellStr(row, "Seri")?.Trim(),
                    CatalogModelName = model.Trim(),
                    Model = model.Trim(),
                    ModelYear = ParseInt(CellStr(row, "Yıl")),
                    OdometerKm = ParseInt(CellStr(row, "Kilometre")),
                    ListedPrice = listed,
                    Color = SanitizeColor(CellStr(row, "Renk")),
                    BodyWorkNotes = CellStr(row, "Boya - Değişen Durumu")?.Trim(),
                    FuelType = MapFuel(CellStr(row, "Yakıt Tipi")),
                    Transmission = MapGear(CellStr(row, "Vites Tipi")),
                    Drivetrain = MapDrive(CellStr(row, "Çekiş")),
                    BodyType = MapBody(CellStr(row, "Kasa Tipi")),
                    EngineDisplacementLiters = ParseEngineLiters(CellStr(row, "Motor Hacmi")),
                    EnginePowerHp = ParseHorsepower(CellStr(row, "Motor Gücü")),
                    FuelTankLiters = ParseTankLiters(CellStr(row, "Yakıt Deposu")),
                    VehicleCondition = MapCondition(CellStr(row, "Araç Durumu")),
                    TradeInAccepted = MapTradeIn(CellStr(row, "Takasa Uygunluk")),
                    SellerType = MapSeller(CellStr(row, "Kimden")),
                    ImageUrls = new List<string>(),
                    Security = Security.None,
                    InternalEquipment = InternalEquipment.None,
                    ExternalEquipment = ExternalEquipment.None,
                    IsApproved = true,
                    CreatedOn = DateTime.UtcNow,
                    ModifiedOn = DateTime.UtcNow
                };

                ApplyRentalFallbackPrices(car);
                db.Cars.Add(car);
                imported++;
            }
            catch (Exception ex)
            {
                errors.Add($"Satır {r}: {ex.Message}");
            }
        }

        await db.SaveChangesAsync();

        Console.WriteLine($"Tamamlandı. İçe aktarılan ilan: {imported}");
        if (errors.Count > 0)
        {
            Console.WriteLine("Uyarılar:");
            foreach (var e in errors.Take(20))
                Console.WriteLine("  " + e);
            if (errors.Count > 20)
                Console.WriteLine($"  … ve {errors.Count - 20} satır daha");
        }

        return errors.Count > 0 ? 2 : 0;
    }

    private static void ApplyRentalFallbackPrices(Car car)
    {
        var listed = car.ListedPrice ?? 0;
        if (listed <= 0) return;
        if (car.DailyPrice <= 0) car.DailyPrice = Math.Round(listed / 30m, 2);
        if (car.WeeklyPrice <= 0) car.WeeklyPrice = Math.Round(listed / 4m, 2);
        if (car.MonthlyPrice <= 0) car.MonthlyPrice = listed;
    }

    private static int? ParseInt(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Replace(".", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal);
        return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null;
    }

    private static decimal? ParseDecimal(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Replace(".", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal);
        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var n) ? n : null;
    }

    private static double? ParseEngineLiters(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim();
        var m = Regex.Match(s, @"(\d+)\s*-\s*(\d+)\s*cm3", RegexOptions.IgnoreCase);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var a) && int.TryParse(m.Groups[2].Value, out var b))
            return Math.Round((a + b) / 2000.0, 3);
        m = Regex.Match(s, @"(\d+(?:[.,]\d+)?)\s*cc", RegexOptions.IgnoreCase);
        if (m.Success && double.TryParse(m.Groups[1].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var cc))
            return Math.Round(cc / 1000.0, 3);
        m = Regex.Match(s, @"(\d+)", RegexOptions.IgnoreCase);
        if (m.Success && int.TryParse(m.Value, out var raw))
            return Math.Round(raw / 1000.0, 3);
        return null;
    }

    private static int? ParseHorsepower(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var m = Regex.Match(s, @"(\d+)\s*-\s*(\d+)", RegexOptions.IgnoreCase);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var a) && int.TryParse(m.Groups[2].Value, out var b))
            return (a + b) / 2;
        m = Regex.Match(s, @"(\d+)", RegexOptions.IgnoreCase);
        return m.Success && int.TryParse(m.Groups[1].Value, out var hp) ? hp : null;
    }

    private static int? ParseTankLiters(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var m = Regex.Match(s, @"(\d+)", RegexOptions.IgnoreCase);
        return m.Success && int.TryParse(m.Groups[1].Value, out var l) ? l : null;
    }

    private static string? SanitizeColor(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (s.Contains("hp", StringComparison.OrdinalIgnoreCase)) return null;
        if (Regex.IsMatch(s, @"^\d+$")) return null;
        return s.Trim();
    }

    private static string NormalizeTr(string s) => s.ToLowerInvariant()
        .Replace("ı", "i")
        .Replace("ğ", "g")
        .Replace("ü", "u")
        .Replace("ş", "s")
        .Replace("ö", "o")
        .Replace("ç", "c");

    private static Gear MapGear(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return Gear.Manuel;
        var n = NormalizeTr(s);
        if (n.Contains("otomatik")) return Gear.Otomatik;
        if (n.Contains("yari")) return Gear.YarıOtomatik;
        if (n.Contains("duz") || n.Contains("manuel")) return Gear.Manuel;
        return Gear.Manuel;
    }

    private static FuelType MapFuel(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return FuelType.Benzin;
        var n = NormalizeTr(s);
        if (n.Contains("dizel")) return FuelType.Dizel;
        if (n.Contains("elektrik")) return FuelType.Elektrik;
        if (n.Contains("hibrit")) return FuelType.Hibrit;
        if (n.Contains("lpg")) return FuelType.Benzin;
        return FuelType.Benzin;
    }

    private static RentalCar.Domain.Enums.DriveType MapDrive(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return RentalCar.Domain.Enums.DriveType.FWD;
        var n = NormalizeTr(s);
        if (n.Contains("arkadan")) return RentalCar.Domain.Enums.DriveType.RWD;
        if (n.Contains("4wd") || n.Contains("awd") || n.Contains("surekli") || n == "suv") return RentalCar.Domain.Enums.DriveType.AWD;
        if (n.Contains("4x4") || n.Contains("4 x 4")) return RentalCar.Domain.Enums.DriveType.FourByFour;
        if (n.Contains("onden") || (n.Contains("on") && n.Contains("cekis"))) return RentalCar.Domain.Enums.DriveType.FWD;
        if (n == "sedan") return RentalCar.Domain.Enums.DriveType.FWD;
        return RentalCar.Domain.Enums.DriveType.FWD;
    }

    private static BodyType MapBody(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return BodyType.Sedan;
        var n = NormalizeTr(s);
        if (n.Contains("hatch")) return BodyType.Hatchback;
        if (n.Contains("mpv")) return BodyType.Minivan;
        if (n.Contains("suv")) return BodyType.Suv;
        if (n.Contains("station") || n.Contains("wagon")) return BodyType.StationWagon;
        if (n.Contains("coupe")) return BodyType.Coupe;
        if (n.Contains("pick")) return BodyType.PıckUp;
        if (n.Contains("sedan")) return BodyType.Sedan;
        return BodyType.Sedan;
    }

    private static VehicleCondition MapCondition(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return VehicleCondition.IkinciEl;
        var n = NormalizeTr(s);
        if (n.Contains("sifir")) return VehicleCondition.Sifir;
        if (n.Contains("hasar")) return VehicleCondition.HasarKayitli;
        if (n.Contains("ikinci")) return VehicleCondition.IkinciEl;
        if (n.Contains("orjinal") || n.Contains("orijinal")) return VehicleCondition.IkinciEl;
        return VehicleCondition.IkinciEl;
    }

    private static bool MapTradeIn(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        var n = NormalizeTr(s);
        return n.Contains("uygun") && !n.Contains("degil") && !n.Contains("değil");
    }

    private static ListingSellerType MapSeller(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return ListingSellerType.Galeri;
        var n = NormalizeTr(s);
        if (n.Contains("sahib")) return ListingSellerType.Sahibinden;
        if (n.Contains("yetkili") || n.Contains("bayi")) return ListingSellerType.YetkiliBayi;
        if (n.Contains("galeri")) return ListingSellerType.Galeri;
        return ListingSellerType.Galeri;
    }
}
