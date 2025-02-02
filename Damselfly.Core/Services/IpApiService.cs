using Damselfly.Core.Interfaces;
using Damselfly.Core.Models.IpApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.Cms;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordPressPCL.Models;

namespace Damselfly.Core.Services
{
    public class IpApiService(ILogger<IpApiService> logger) : IIpOriginService
    {
        private readonly ILogger<IpApiService> _logger = logger;

        public async Task<string> GetIpOrigin(string ipAddress)
        {
            try
            {
                var options = new RestClientOptions("https://ipapi.co");
                var client = new RestClient(options);
                var request = new RestRequest($"/{ipAddress}/json", Method.Get);
                request.AddHeader("Content-Type", "application/json");
                var response = await client.ExecuteAsync(request);
                var result = JsonConvert.DeserializeObject<IpCheckResult>(response.Content);
                return result.CountryCode;
            }
            catch( Exception ex )
            {
                _logger.LogError(ex, "Error getting IP origin");
                return ex.Message;
            }
        }
    }
}
