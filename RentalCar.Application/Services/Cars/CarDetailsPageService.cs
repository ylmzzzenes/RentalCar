using RentalCar.Application.Abstractions.Services.Cars;
using RentalCar.Application.Contracts.Cars;
using RentalCar.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RentalCar.Application.Services.Cars
{
    public class CarDetailsPageService : ICarDetailsPageService
    {
        private readonly ICarInteractionService _carInteractionService;

        public CarDetailsPageService(ICarInteractionService carInteractionService)
        {
            _carInteractionService = carInteractionService;
        }
        public async Task<CarDetailsPageResult?> BuildAsync(int carId, string? currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
        {
            var result = await _carInteractionService.GetCarDetailAsync(
               carId,
               currentUserId,
               isAdmin,
               cancellationToken);

            if (result == null)
                return null;

            return new CarDetailsPageResult
            {
                Car = result.Car,
                Seller = result.Seller,
                SimilarCars = result.SimilarCars,
                RecommendedCars = result.RecommendedCars,
                Comments = result.Comments.Select(x => new CarComment
                {
                    Id = x.Id,
                    Content = $"{x.UserName}|{x.Content}",
                    CreatedOn = x.CreatedOn
                }).ToList(),
                AverageRating = result.AverageRating,
                RatingCount = result.RatingCount,
                CurrentUserRating = result.CurrentUserRating,
                IsFavorite = result.IsFavorite
            };
        }

        
    }
}
