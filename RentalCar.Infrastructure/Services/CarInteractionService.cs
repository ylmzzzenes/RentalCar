using Microsoft.EntityFrameworkCore;
using RentalCar.Application.Abstractions.Services;
using RentalCar.Application.Dtos.Cars;
using RentalCar.Domain.Entities;
using RentalCar.Infrastructure.Persistence.Context;

namespace RentalCar.Infrastructure.Services
{
    public sealed class CarInteractionService : ICarInteractionService
    {
        private readonly RentalCarContext _context;
        private readonly CarServices _carServices;

        public CarInteractionService(RentalCarContext context, CarServices carServices)
        {
            _context = context;
            _carServices = carServices;
        }
        public async Task AddCommentAsync(int carId, string userId, string content, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Yorum içeriği boş olamaz", nameof(content));

            _context.CarComments.Add(new CarComment
            {
                CarId = carId,
                UserId = userId,
                Content = content.Trim(),
                CreatedOn = DateTime.Now,
                ModifiedOn = DateTime.Now
            });

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<CarDetailsDto> GetCarDetailAsync(int CarId, string? userId, bool isAdmin, CancellationToken cancellationToken = default)
        {
            var car = await _carServices.GetByIdAsync(CarId, cancellationToken);
            if (car == null) return null;

            if (!car.IsApproved && !isAdmin) return null;

            var comments = await _context.CarComments
                                 .AsNoTracking()
                                 .Where(x => x.CarId == CarId)
                                 .OrderByDescending(x => x.CreatedOn)
                                 .Take(50)
                                 .ToListAsync(cancellationToken);

            var commentUserIds = comments
                                .Select(x => x.UserId)
                                .Distinct()
                                .ToList();

            var commentUserMap = await _context.Users
                                       .Where(x => commentUserIds.Contains(x.Id))
                                       .ToDictionaryAsync(
                                        x => x.Id,
                                        x => x.UserName ?? "Kullanici",
                                        cancellationToken
                                        );

            var commentItems = comments.Select(c => new CarCommentItemDto
            {
                Id = c.Id,
                UserName = commentUserMap.TryGetValue(c.UserId, out var userName) ? userName : "Kullanici",
                Content = c.Content,
                CreatedOn = c.CreatedOn
            }).ToList();

            var ratings = await _context.CarRatings
                                .AsNoTracking()
                                .Where(x => x.CarId == CarId)
                                .ToListAsync(cancellationToken);

            var currentUserRating = userId is null
                                    ? null : ratings.FirstOrDefault(x => userId == userId)?.Score;

            var isFavorite = userId is not null &&
                             await _context.Favorites.AnyAsync(
                                 x => x.CarId == CarId && x.UserId == userId,
                                 cancellationToken);

            return new CarDetailsDto
            {
                Car = car,
                Comments = commentItems,
                AverageRating = ratings.Count == 0 ? 0 : ratings.Average(x => x.Score),
                RatingCount = ratings.Count,
                CurrentUserRating = currentUserRating,
                IsFavorite = isFavorite
            };


        }

        public async Task RateAsync(int carId, string userId, int score, CancellationToken cancellationToken = default)
        {
            if (score < 1 || score > 5)
                throw new ArgumentException(nameof(score), "Skor 1 ile 5 arasında olmalı.");

            var rating = await _context.CarRatings
                        .FirstOrDefaultAsync(
                         x => x.CarId == carId && x.UserId == userId,
                         cancellationToken);

            if (rating == null)
            {
                _context.CarRatings.Add(new CarRating
                {
                    CarId = carId,
                    UserId = userId,
                    Score = score,
                    CreatedOn = DateTime.Now,
                    ModifiedOn = DateTime.Now
                });
            }
            else
            {
                rating.Score = score;
                rating.ModifiedOn = DateTime.Now;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task ToggledFavoriteAsync(int carId, string userId, CancellationToken cancellationToken = default)
        {
            var favorite = await _context.Favorites
                           .FirstOrDefaultAsync(
                            x => x.CarId == carId && x.UserId == userId,
                            cancellationToken);

            if (favorite == null)
            {
                _context.Favorites.Add(new Favorite
                {
                    CarId = carId,
                    UserId = userId,
                    CreatedOn = DateTime.Now,
                    ModifiedOn = DateTime.Now
                });
            }
            else
            {
                {
                    _context.Favorites.Remove(favorite);
                }
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
