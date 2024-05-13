using Damselfly.PaymentProcessing.PaymentProcessors;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.PaymentProcessing
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddPaymentServices(this IServiceCollection services)
        {
            return services.AddScoped<PayPalPaymentProcessor>()
                .AddScoped<IPaymentProcessorFactory, PaymentProcessorFactory>()
                .AddScoped<PaymentService>();
        }
    }
}
