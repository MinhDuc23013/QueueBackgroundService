using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailWorker.Enum
{
    public enum OutboxStatus
    {
        New,
        Processing,
        Published,
        Failed
    }
}
