using System.Net;
using System.Net.Mail;
using RentalCar.Application.Abstractions.Services;

namespace RentalCar.Infrastructure.Services.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly string _host;
    private readonly int _port;
    private readonly bool _enableSSL;
    private readonly string _userName;
    private readonly string _password;

    public SmtpEmailSender(string host, int port, bool enableSSL, string userName, string password)
    {
        _host = string.IsNullOrWhiteSpace(host) ? throw new ArgumentException("SMTP host bos olamaz.", nameof(host)) : host;
        _port = port;
        _enableSSL = enableSSL;
        _userName = string.IsNullOrWhiteSpace(userName) ? throw new ArgumentException("SMTP kullanici adi bos olamaz.", nameof(userName)) : userName;
        _password = string.IsNullOrWhiteSpace(password) ? throw new ArgumentException("SMTP sifresi bos olamaz.", nameof(password)) : password;
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        using var client = new SmtpClient(_host, _port)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_userName, _password),
            EnableSsl = _enableSSL
        };

        using var mail = new MailMessage(_userName, email, subject, message)
        {
            IsBodyHtml = true
        };

        await client.SendMailAsync(mail);
    }
}
