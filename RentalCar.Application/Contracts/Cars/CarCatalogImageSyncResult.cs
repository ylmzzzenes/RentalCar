namespace RentalCar.Application.Contracts.Cars;

public sealed class CarCatalogImageSyncResult
{
    public int CarsProcessed { get; set; }
    public int CarsUpdated { get; set; }
    public int CarsSkipped { get; set; }
    public int FilesDownloaded { get; set; }
    public List<string> Errors { get; set; } = new();
}
