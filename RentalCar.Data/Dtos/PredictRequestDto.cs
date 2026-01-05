using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RentalCar.Data.Dtos
{
    public class PredictRequestDto
    {
        public string marka { get; set; } = "Bilinmiyor";
        public string model_adi { get; set; } = "Bilinmiyor";
        public string paket { get; set; } = "Standard";
        public string motor_kodu { get; set; } = "Benzin";
        public string cekis { get; set; } = "FWD";
        public string sanziman_kodu { get; set; } = "AT";
        public string vites { get; set; } = "Manuel";
        public string yakitTuru { get; set; } = "Benzin";
        public string renk { get; set; } = "Bilinmiyor";
        public int yil { get; set; }
        public int kilometre { get; set; }
        public int vergi { get; set; }
        public double lt_100km { get; set; }
        public double motorHacmi { get; set; }
        public string? sehir { get; set; }
        public string? kasaTipi { get; set; }
        public string? donanimSeviyesi { get; set; }
        public int? hasarKaydi { get; set; }          
        public string? degisenBoyanan { get; set; }
        public int? servisGecmisi { get; set; }       

        public int? motorGuc_hp { get; set; }
        public int? tork_nm { get; set; }
        public int? sahipSayisi { get; set; }
    }
}
