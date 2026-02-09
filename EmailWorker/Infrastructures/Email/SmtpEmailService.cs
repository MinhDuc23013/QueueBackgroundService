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

            await using var tx = await db.Database.BeginTransactionAsync();

            var message = await db.OutboxMessages
                .Where(x => x.Id == request.Id)
                .FirstOrDefaultAsync();

            if (message == null)
            {
                Console.WriteLine("Message not found - Id:" + request.Id.ToString());
                return; 
            }

            //if (message.Status == OutboxStatus.Sent.ToString())
            //    return; // đã gửi rồi → idempotent exit

            //if (message.Status == OutboxStatus.Processing.ToString())
            //    return; // instance khác đang xử lý



            await tx.CommitAsync();

            Console.WriteLine($"Sending email to {message.Id} with subject {message.Type}");

            //message.Status = OutboxStatus.Sent.ToString();
            message.PublishedAt = DateTime.UtcNow;

            //try
            //{


            //}
            //catch
            //{
            //    message.Status = OutboxStatus.Failed;
            //    throw new SmtpException(
            //        SmtpStatusCode.MailboxBusy,
            //        "Mailbox busy – retry later");
            //}

            await db.SaveChangesAsync();
        }
    }
}
