using RentalCar.Application.Dtos.Cars;
using RentalCar.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RentalCar.Application.Contracts.Cars
{
    public class CarDetailsPageResult
    {
        public Car Car { get; set; } = null!;
        public CarSellerSummaryDto? Seller { get; set; }
        public List<Car> SimilarCars { get; set; } = new();
        public CarListingInsightResult? ListingInsights { get; set; }
        public List<Car> RecommendedCars { get; set; } = new();
        public int ReliabilityScore { get; set; }
        public string ReliabilityLabel { get; set; } = "Medium";
        public string ReliabilityTrustLevelTr { get; set; } = "Orta güven";
        public string ReliabilityExplanation { get; set; } = string.Empty;
        public IReadOnlyList<ListingReliabilityFactorDetail>? ReliabilityFactors { get; set; }
        public bool IsFavorite { get; set; }
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public int? CurrentUserRating { get; set; }
        public List<CarComment> Comments { get; set; } = new();
    }
}
