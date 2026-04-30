using RentalCar.Application.Contracts.Cars;

namespace RentalCar.Application.Abstractions.Services.Cars;

public interface ICarCatalogImageSyncService
{
    Task<IReadOnlyList<CarBrandModelExportRow>> GetBrandModelExportRowsAsync(CancellationToken cancellationToken = default);

    Task<CarCatalogImageSyncResult> DownloadAndAttachToCarsAsync(
        bool replaceRemoteUrls,
        CancellationToken cancellationToken = default);
}
