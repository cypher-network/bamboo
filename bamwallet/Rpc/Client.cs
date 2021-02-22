// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json.Linq;

using Dawn;

using BAMWallet.Helper;

namespace BAMWallet.Rpc
{
    public class Client
    {
        internal const string ErrorMessage = "Please check the logs for any details.";

        private readonly ILogger _logger;
        private readonly IConfigurationSection _apiRestSection;

        public Client(IConfiguration apiRestSection, ILogger logger)
        {
            _apiRestSection = apiRestSection.GetSection(RestCall.Gateway);
            _logger = logger;
        }

        /// <summary>
        /// Get async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="baseAddress">Base address.</param>
        /// <param name="path">Path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public async Task<T> GetAsync<T>(Uri baseAddress, string path, CancellationToken cancellationToken)
        {
            Guard.Argument(baseAddress, nameof(baseAddress)).NotNull();
            Guard.Argument(path, nameof(path)).NotNull().NotEmpty();

            T result = default;

            using var client = new HttpClient
            {
                BaseAddress = baseAddress
            };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, path);
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                var read = response.Content.ReadAsStringAsync(cancellationToken).Result;
                var jObject = JObject.Parse(read);
                var jToken = jObject.GetValue("protobuf");
                var byteArray = Convert.FromBase64String(jToken.Value<string>());

                if (response.IsSuccessStatusCode)
                    result = Util.DeserializeProto<T>(byteArray);
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Result: {content}\n StatusCode: {(int)response.StatusCode}");
                    throw new Exception(content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
            }

            return Task.FromResult(result).Result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Uri GetBaseAddress()
        {
            return new Uri(_apiRestSection.GetValue<string>(RestCall.Endpoint));
        }

        /// <summary>
        /// Get range async.
        /// </summary>
        /// <returns>The range async.</returns>
        /// <param name="baseAddress">Base address.</param>
        /// <param name="path">Path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<IEnumerable<T>> GetRangeAsync<T>(Uri baseAddress, string path, CancellationToken cancellationToken) where T : new()
        {
            Guard.Argument(baseAddress, nameof(baseAddress)).NotNull();
            Guard.Argument(path, nameof(path)).NotNull().NotEmpty();

            IEnumerable<T> results = Enumerable.Empty<T>();

            using var client = new HttpClient
            {
                BaseAddress = baseAddress
            };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, path);
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                var read = response.Content.ReadAsStringAsync(cancellationToken).Result;
                var jObject = JObject.Parse(read);
                var jToken = jObject.GetValue("protobufs");
                var byteArray = Convert.FromBase64String(jToken.Value<string>());

                if (response.IsSuccessStatusCode)
                    results = Util.DeserializeListProto<T>(byteArray);
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Result: {content}\n StatusCode: {(int)response.StatusCode}");
                    throw new Exception(content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
            }

            return Task.FromResult(results).Result;
        }

        /// <summary>
        /// Sends a POST request.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="payload"></param>
        /// <param name="baseAddress"></param>
        /// <param name="path"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> PostAsync<T>(T payload, Uri baseAddress, string path, CancellationToken cancellationToken)
        {
            Guard.Argument(baseAddress, nameof(baseAddress)).NotNull();
            Guard.Argument(path, nameof(path)).NotNull().NotEmpty();

            using var client = new HttpClient
            {
                BaseAddress = baseAddress
            };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                var proto = Util.SerializeProto(payload);

                using var response = await client.PostAsJsonAsync(path, proto, cancellationToken);

                var read = response.Content.ReadAsStringAsync(cancellationToken).Result;

                if (response.IsSuccessStatusCode)
                    return true;
                else
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError($"Result: {content}\n StatusCode: {(int) response.StatusCode}");
                    throw new Exception(content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
            }

            return false;
        }

    }
}

