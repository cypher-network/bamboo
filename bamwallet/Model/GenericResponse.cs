// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Net;

namespace BAMWallet.Model
{
    public class GenericResponse<T>
    {
        public T Data { get; set; }
        public HttpStatusCode HttpStatusCode { get; set; }
    }
}