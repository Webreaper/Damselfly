using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Shared.Utils
{
    public class BookingHub(ILogger<BookingHub> logger) : Hub
    {
    }
}
