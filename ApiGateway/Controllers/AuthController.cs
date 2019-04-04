using System.Linq;
using System.Threading.Tasks;
using ApiGateway.DbContexts;
using ApiGateway.Utils;
using SharedArea.Middles;
using Microsoft.AspNetCore.Mvc;
using SharedArea.Commands.Auth;
using SharedArea.Entities;
using SharedArea.Utils;

namespace ApiGateway.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : Controller
    {
        [Route("~/api/auth/register")]
        [HttpPost]
        public async Task<ActionResult<Packet>> Register([FromBody] Packet packet)
        {
            var result = await SharedArea.Transport
                .RequestService<RegisterRequest, RegisterResponse>(
                    Startup.Bus,
                    SharedArea.GlobalVariables.CITY_QUEUE_NAME,
                    Request.Headers.ToDictionary(a => a.Key, a => a.Value.ToString()),
                    packet);
            
            return result.Packet;
        }

        [Route("~/api/auth/verify")]
        [HttpPost]
        public async Task<ActionResult<Packet>> Verify([FromBody] Packet packet)
        {
            var result = await SharedArea.Transport
                .RequestService<VerifyRequest, VerifyResponse>(
                    Startup.Bus,
                    SharedArea.GlobalVariables.CITY_QUEUE_NAME,
                    Request.Headers.ToDictionary(a => a.Key, a => a.Value.ToString()),
                    packet);
            
            return result.Packet;
        }

        [Route("~/api/auth/logout")]
        [HttpPost]
        public async Task<ActionResult<Packet>> Logout()
        {
            using (var dbContext = new DatabaseContext())
            {
                var session = Security.Authenticate<UserSession>(dbContext, Request.Headers[AuthExtracter.AK]);
                if (session == null) return new Packet() {Status = "error_040"};
                
                var result = await SharedArea.Transport
                    .RequestService<LogoutRequest, LogoutResponse>(
                        Startup.Bus,
                        SharedArea.GlobalVariables.CITY_QUEUE_NAME,
                        session,
                        Request.Headers.ToDictionary(a => a.Key, a => a.Value.ToString()));
            
                return result.Packet;
            }
        }

        [Route("~/api/auth/delete_account")]
        [HttpPost]
        public async Task<ActionResult<Packet>> DeleteAccount()
        {
            using (var dbContext = new DatabaseContext())
            {
                var session = Security.Authenticate<UserSession>(dbContext, Request.Headers[AuthExtracter.AK]);
                if (session == null) return new Packet() {Status = "error_0"};
                
                var result = await SharedArea.Transport.RequestService<DeleteAccountRequest, DeleteAccountResponse>(
                    Startup.Bus,
                    SharedArea.GlobalVariables.CITY_QUEUE_NAME,
                    session,
                    Request.Headers.ToDictionary(a => a.Key, a => a.Value.ToString()));
            
                return result.Packet;
            }
        }
    }
}