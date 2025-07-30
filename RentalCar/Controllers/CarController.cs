
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalCar.Data.Enums;
using RentalCar.Data.Models;
using RentalCar.Data.Models.Requests;
using RentalCar.Data.Services;


namespace RentalCar.Controllers
{
    public class CarController : Controller
    {
        private readonly CarServices _carsServices;
        
        public CarController(CarServices carServices)
        {
            _carsServices = carServices;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Get(int id)
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(CreateCar request, List<int> SelectedSecurity, List<int> SelectedInternal, List<int> SelectedExternal, List<int> SelectedBodyType, List<int> SelectedFuelType, List<int> SelectedGear, CancellationToken cancellationToken = default)
        {
            Car newCar = new Car();

            var fileNames = new List<string>();

            if (request.ImageUrls != null && request.ImageUrls.Any())
            {
                foreach (var file in request.ImageUrls)
                {
                    var extension = Path.GetExtension(file.FileName);
                    var newFileName = Guid.NewGuid() + extension;
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images/Upload/", newFileName);

                    using var stream = new FileStream(path, FileMode.Create);
                    await file.CopyToAsync(stream);

                    fileNames.Add(newFileName);
                }
            }

            newCar.ImageUrls = string.Join(",", fileNames); 

            var x = DateTime.Now;

            newCar.Vin = request.Vin;
            newCar.Brand = request.Brand;
            newCar.Model = request.Model;
            newCar.Year = request.Year;
            newCar.Colour = request.Colour;
            newCar.Plate = request.Plate;
            newCar.Security = SelectedSecurity.Aggregate(Security.None, (acc, val) => acc | (Security)val);
            newCar.InternalEquipment = SelectedInternal.Aggregate(InternalEquipment.None, (acc, val) => acc | (InternalEquipment)val);
            newCar.ExternalEquipment = SelectedExternal.Aggregate(ExternalEquipment.None, (acc, val) => acc | (ExternalEquipment)val);
            newCar.BodyType = SelectedBodyType.Aggregate(BodyType.None, (acc, val) => acc | (BodyType)val);
            newCar.FuelType = SelectedFuelType.Aggregate(FuelType.None, (acc, val) => acc | (FuelType)val);
            newCar.Gear = SelectedGear.Aggregate(Gear.None, (acc, val) => acc | (Gear)val);
            newCar.CreatedOn = x;
            newCar.ModifiedOn = x;

            await _carsServices.CreateAsync(newCar, cancellationToken);
            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> List(string? brand, string? model, string? colour, FuelType? fuelType, Gear? gear,BodyType? bodyType, DateOnly? year,string ImageUrl,string searchString)
        {
            var query = (await _carsServices.GetAllAsync()).AsQueryable();
            
            if(_carsServices.GetAllAsync() == null)
            {
                return Problem("Entity set 'Rental Car' is null");
            }
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(s => s.Brand.ToUpper().Contains(searchString.ToUpper())||
                   s.Model.ToUpper().Contains(searchString.ToUpper()) ||
                   s.Colour.ToUpper().Contains(searchString.ToUpper()) 
                                    );
             

            }
     
            if (!string.IsNullOrWhiteSpace(brand))
                query = query.Where(c => c.Brand.Contains(brand, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(model))
                query = query.Where(c => c.Model.Contains(model, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(colour))
                query = query.Where(c => c.Colour.Contains(colour, StringComparison.OrdinalIgnoreCase));

            if (fuelType != null)
                query = query.Where(c => c.FuelType == fuelType);

            if (gear != null)
                query = query.Where(c => c.Gear == gear);

            if (bodyType != null)
                query = query.Where(c => c.BodyType == bodyType);

            if (year != null)
                query = query.Where(c => c.Year == year);
            if (ImageUrl != null)
                query = query.Where(c => c.ImageUrls == ImageUrl);

            return View(query.ToList());
        }




        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
        {
            var car = await _carsServices.GetByIdAsync(id, cancellationToken);
            if (car == null)
                return NotFound(); 

            return View(car);
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id,CancellationToken cancellationToken = default)
        {
            var car=await _carsServices.GetByIdAsync(id,cancellationToken);
            if(car==null)
                return NotFound();
            return View(car);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Car updateCar,CancellationToken cancellationToken = default)
        {
            if(!ModelState.IsValid)
                return View(updateCar);

            await _carsServices.UpdateAsync(updateCar,cancellationToken);
            return RedirectToAction(nameof(Index)); 
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id,CancellationToken cancellationToken = default)
        {
            var car=await _carsServices.GetByIdAsync(id,cancellationToken);
            if (car == null)
                return NotFound();
            return View(car);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirm(int id,CancellationToken cancellationToken = default)
        {
            var success=await _carsServices.DeleteAsync(id, cancellationToken); 
            if(!success)
                return NotFound();

            return RedirectToAction(nameof(List));
        }

       
        
    }
        
}

