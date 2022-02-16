// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace BAMWallet.Rpc.Formatters
{
    public class BinaryInputFormatter : InputFormatter
    {
        private const string BinaryContentType = "application/octet-stream";
        private const int BufferLength = 16384;

        public BinaryInputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(BinaryContentType));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            await using var ms = new MemoryStream(BufferLength);
            await context.HttpContext.Request.Body.CopyToAsync(ms);
            object result = ms.ToArray();
            return await InputFormatterResult.SuccessAsync(result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected override bool CanReadType(Type type)
        {
            return type == typeof(byte[]);
        }
    }
}