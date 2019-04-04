
using System.Net.Http;
using System.Threading.Tasks;
using ApiGateway.DbContexts;
using ApiGateway.Utils;
using Microsoft.AspNetCore.Mvc;
using SharedArea.Entities;
using SharedArea.Middles;
using SharedArea.Utils;

namespace ApiGateway.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class ModuleController : Controller
    {
        [Route("~/api/module/request_module")]
        [HttpPost]
        public async Task<ActionResult> GetMessages([FromBody] Packet packet)
        {
            using (var context = new DatabaseContext())
            {
                var app = Security.AuthenticateApp(context, Request.Headers[AuthExtracter.AppKeyHeader]);
                if (app == null) return Json(new Packet() {Status = "error_100"});
                
                var session = Security.Authenticate<BotSession>(context, Request.Headers[AuthExtracter.AK]);
                if (session == null) return Forbid();
                
                var client = new HttpClient();
                var content = new FormUrlEncodedContent(packet.ModuleRequest.Parameters);
                var response = await client.PostAsync(SharedArea.GlobalVariables.SERVER2_URL + "/" + packet.ModuleRequest.EndPointName, content);
                var responseString = await response.Content.ReadAsStringAsync();
                
                return Ok(responseString);
            }
        }
    }
}