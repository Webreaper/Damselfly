using Damselfly.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _redisDB;

        public RedisCacheService(IConfiguration config)
        {
            var connectionString = config["DamselflyConfiguration:RedisConnectionString"];
            if( string.IsNullOrEmpty(connectionString) )
            {
                throw new Exception("Redis connection string is not found in the configuration file.");
            }

            var connection = ConnectionMultiplexer.Connect(connectionString);
            _redisDB = connection.GetDatabase();
        }

        public async Task<string> GetAsync(string key)
        {
            return await _redisDB.StringGetAsync(key);
        }

        public async Task SetAsync(string key, string value, TimeSpan expirationTimeSpan)
        {
            await _redisDB.StringSetAsync(key, value, expirationTimeSpan);
        }
    }
}
