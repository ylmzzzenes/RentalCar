using RentalCar.Application.Contracts.Cars;
using RentalCar.Application.Dtos.Cars;
using RentalCar.Domain.Entities;

namespace RentalCar.ViewModels;

public class CarDetailsViewModel
{
    public Car Car { get; set; } = null!;
    public CarSellerSummaryDto? Seller { get; set; }
    public List<Car> SimilarCars { get; set; } = new();
    public CarListingInsightResult? ListingInsights { get; set; }
    public List<Car> RecommendedCars { get; set; } = new();
    public int ReliabilityScore { get; set; }
    public string ReliabilityLabel { get; set; } = "Medium";
    public string ReliabilityTrustLevelTr { get; set; } = "Orta güven";
    public string ReliabilityExplanation { get; set; } = "";
    public IReadOnlyList<ListingReliabilityFactorDetail>? ReliabilityFactors { get; set; }
    public bool IsFavorite { get; set; }
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public int? CurrentUserRating { get; set; }
    public List<CarComment> Comments { get; set; } = new();
}
