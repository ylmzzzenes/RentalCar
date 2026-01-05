using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RentalCar.Data.Models
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string Email,string subject,string message);
    }
}
