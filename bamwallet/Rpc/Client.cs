// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using BAMWallet.Extensions;
using Newtonsoft.Json.Linq;
using MessagePack;
using Dawn;
using BAMWallet.Model;
using Newtonsoft.Json;
using Serilog;

namespace BAMWallet.Rpc
{
    public class Client
    {
        private readonly ILogger _logger;
        private readonly NetworkSettings _networkSettings;

        public Client(NetworkSettings networkSettings, ILogger logger)
        {
            _networkSettings = networkSettings;
            _logger = logger.ForContext("SourceContext", nameof(Client));
        }

        /// <summary>
        /// </summary>
        /// <param name="baseAddress">Base address.</param>
        /// <param name="path">Path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public GenericResponse<T> GetAsync<T>(Uri baseAddress, string path,
            CancellationToken cancellationToken) where T : class
        {
            Guard.Argument(baseAddress, nameof(baseAddress)).NotNull();
            Guard.Argument(path, nameof(path)).NotNull().NotEmpty();
            using var client = new HttpClient
            {
                BaseAddress = baseAddress,
                DefaultRequestHeaders =
                {
                    Accept = {new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json)}
                }
            };
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, path);
                using var response = client.Send(request, HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var read = response.Content.ReadAsStringAsync(cancellationToken).Result;
                    var jObject = JObject.Parse(read);
                    var jToken = jObject.GetValue("messagepack");
                    var byteArray = Convert.FromBase64String(jToken.Value<string>());
                    var t = MessagePackSerializer.Deserialize<T>(byteArray, cancellationToken: cancellationToken);
                    return new GenericResponse<T> { Data = t, HttpStatusCode = HttpStatusCode.OK };
                }

                var content = response.Content.ReadAsStringAsync(cancellationToken);
                _logger.Here().Error("Result: {@Content}\n StatusCode: {@StatusCode}", content, response.StatusCode);
                return new GenericResponse<T> { Data = null, HttpStatusCode = response.StatusCode };
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error while communicating with client");
                return new GenericResponse<T> { Data = null, HttpStatusCode = HttpStatusCode.ServiceUnavailable };
            }
        }

        public BlockHeight GetBlockHeightAsync(Uri baseAddress, string path, CancellationToken cancellationToken)
        {
            Guard.Argument(baseAddress, nameof(baseAddress)).NotNull();
            Guard.Argument(path, nameof(path)).NotNull().NotEmpty();
            using var client = new HttpClient
            {
                BaseAddress = baseAddress,
                DefaultRequestHeaders =
                {
                    Accept = {new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json)}
                }
            };
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, path);
                using var response = client.Send(request, HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var read = response.Content.ReadAsStringAsync(cancellationToken).Result;
                    return JsonConvert.DeserializeObject<BlockHeight>(read);
                }

                var content = response.Content.ReadAsStringAsync(cancellationToken);
                _logger.Here().Error("Result: {@Content}\n StatusCode: {@StatusCode}", content, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error getting block height");
                return null;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public Uri GetBaseAddress()
        {
            var uriString = _networkSettings.RemoteNode;
            if (string.IsNullOrEmpty(uriString))
            {
                _logger.Here().Error("Remote node address not set in config");
            }
            else
            {
                if (!Uri.TryCreate(uriString, UriKind.Absolute, out var baseAddress)) return null;
                if (baseAddress.Scheme == Uri.UriSchemeHttp)
                {
                    return baseAddress;
                }

                _logger.Here().Error("Invalid URI scheme '{@Scheme}' in '{@UriString}'",
                    baseAddress.Scheme, uriString);
            }
            throw new Exception("Cannot get base address");
        }

        /// <summary>
        /// Get range.
        /// </summary>
        /// <returns>The range.</returns>
        /// <param name="baseAddress">Base address.</param>
        /// <param name="path">Path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public GenericList<T> GetRangeAsync<T>(Uri baseAddress, string path, CancellationToken cancellationToken)
        {
            Guard.Argument(baseAddress, nameof(baseAddress)).NotNull();
            Guard.Argument(path, nameof(path)).NotNull().NotEmpty();

            GenericList<T> results = null;

            using var client = new HttpClient
            {
                BaseAddress = baseAddress,
                DefaultRequestHeaders =
                {
                    Accept =
                    {
                        new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json)
                    }
                }
            };

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, path);
                using var response = client.Send(request, HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var read = response.Content.ReadAsStringAsync(cancellationToken).Result;
                    var jObject = JObject.Parse(read);
                    var jToken = jObject.GetValue("messagepack");
                    var byteArray = Convert.FromBase64String(jToken.Value<string>());
                    results = MessagePackSerializer.Deserialize<GenericList<T>>(byteArray,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    var content = response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.Here().Error("Result: {@Content}\n StatusCode: {@StatusCode}",
                        content, response.StatusCode);
                    throw new Exception(content.Result);
                }
            }
            catch (TaskCanceledException)
            {

            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error getting range");
            }

            return results;
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
        public HttpStatusCode PostAsync<T>(T payload, Uri baseAddress, string path,
            CancellationToken cancellationToken) where T : class
        {
            Guard.Argument(baseAddress, nameof(baseAddress)).NotNull();
            Guard.Argument(path, nameof(path)).NotNull().NotEmpty();
            using var client = new HttpClient
            {
                BaseAddress = baseAddress,
                DefaultRequestHeaders = { Accept = { new MediaTypeWithQualityHeaderValue("application/x-protobuf") } }
            };
            try
            {
                var buffer = MessagePackSerializer.Serialize(payload, cancellationToken: cancellationToken);
                using var response = client.PostAsJsonAsync(path, buffer, cancellationToken);
                var _ = response.Result.Content.ReadAsStringAsync(cancellationToken).Result;
                if (response.Result.IsSuccessStatusCode) return HttpStatusCode.OK;
                var content = response.Result.Content.ReadAsStringAsync(cancellationToken);
                _logger.Here().Error("Result: {@Content}\n StatusCode: {@StatusCode}",
                    content, response.Result.Content);
                return response.Result.StatusCode;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error POSTing data to client");
            }

            return HttpStatusCode.ServiceUnavailable;
        }
    }
}

