using EmailWorker.Applications.Interfaces;
using EmailWorker.Applications.Interfaces.DTO;
using EmailWorker.Entity;
using EmailWorker.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace EmailWorker.Infrastructures.Email
{
    public sealed class SmtpEmailService : IEmailService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public SmtpEmailService(
            IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }


        public async Task SendAsync(OutboxMessage request)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;

            var rows = await db.Database.ExecuteSqlInterpolatedAsync($@"
                    UPDATE OutboxMessages
                    SET Status = {OutboxStatus.Sent.ToString()},
                        ProcessingAt = {now}
                    WHERE Id = {request.Id}
                      AND Status IN ({OutboxStatus.Published.ToString()})
                ");

            if (rows == 0)
            {
                Console.WriteLine($"Skip {request.Id} - already processed by another worker");
                return;
            }

            var message = await db.OutboxMessages
                .Where(x => x.Id == request.Id)
                .FirstAsync();

            try
            {
                Console.WriteLine($"Sending email for {message.Id}");

                SendEmail(message);

                message.Status = OutboxStatus.Sent.ToString();
                message.PublishedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                message.Status = OutboxStatus.Failed.ToString();
                //message.Error = ex.Message;
                throw;
            }

            await db.SaveChangesAsync();
        }
        public bool SendEmail(OutboxMessage message)
        {
            try
            {
                var payload = System.Text.Json.JsonSerializer.Deserialize<EmailRequestDTO>(message.Payload);
                Console.WriteLine($"Sending email to {payload?.To} with subject {payload?.Template}");
                //var mailMessage = new MailMessage(""
                //    , message.Destination);
                //mailMessage.Subject = "Order Confirmation";
                //mailMessage.Body = message.Payload;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email to {message.Destination}: {ex.Message}");
                return false;
            }
        }
    }
}