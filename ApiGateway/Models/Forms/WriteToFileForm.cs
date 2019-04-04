using Microsoft.AspNetCore.Http;

namespace ApiGateway.Models.Forms
{
    public class WriteToFileForm
    {
        public long FileId { get; set; }
        public IFormFile File { get; set; }
    }
}