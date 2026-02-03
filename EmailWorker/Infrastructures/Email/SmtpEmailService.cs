using EmailWorker.Applications.Interfaces;
using EmailWorker.Applications.Interfaces.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailWorker.Infrastructures.Email
{
    public sealed class SmtpEmailService : IEmailService
    {
        public Task SendAsync(EmailRequestDTO request)
        {
            Console.WriteLine($"Sending mail to {request.Email} with template {request.Body}");

            return Task.CompletedTask;
        }
    }
}
