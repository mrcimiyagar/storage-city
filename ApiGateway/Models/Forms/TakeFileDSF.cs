using Microsoft.AspNetCore.Http;

namespace ApiGateway.Models.Forms
{
    public class TakeFileDSF
    {
        public IFormFile File { get; set; }
        public string StreamCode { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}