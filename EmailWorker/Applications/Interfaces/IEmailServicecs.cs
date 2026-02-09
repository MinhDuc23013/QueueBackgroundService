using EmailWorker.Applications.Interfaces.DTO;
using EmailWorker.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailWorker.Applications.Interfaces
{
    public interface IEmailService
    {
        Task SendAsync(OutboxMessage request);
    }
}
