using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.Services
{
    public class NotificationService(IConfiguration configuration)
    {
        private readonly IConfiguration _configuration = configuration;


        public async Task<bool> SendNotification(string title, string message)
        {
            var recipient = _configuration["HomeAssistant:Recipient"];
            var homeAssistantUrl = _configuration["HomeAssistant:Url"];
            var homeAssistantToken = _configuration["HomeAssistant:Token"];
            var options = new RestClientOptions(homeAssistantUrl)
            {
                Timeout = new TimeSpan(0, 0, 30),
            };
            var client = new RestClient(options);
            var request = new RestRequest($"/api/services/notify/{recipient}", Method.Post);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Authorization", $"Bearer {homeAssistantToken}");
            var homeAssistantMessage = new HomeAssistantMessage
            {
                Title = title,
                Message = message
            };
            var body = Newtonsoft.Json.JsonConvert.SerializeObject(homeAssistantMessage);
            request.AddStringBody(body, DataFormat.Json);
            var response = await client.ExecuteAsync(request);
            return response.IsSuccessful;
        }
    }

    public class HomeAssistantMessage
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
