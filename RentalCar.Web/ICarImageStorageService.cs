namespace RentalCar.Web
{
    public interface ICarImageStorageService
    {
        Task<IReadOnlyList<string>> SaveAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken = default);
    }
}
