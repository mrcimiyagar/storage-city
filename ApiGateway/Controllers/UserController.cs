
using System.Linq;
using System.Threading.Tasks;
using ApiGateway.DbContexts;
using ApiGateway.Utils;
using SharedArea.Middles;
using Microsoft.AspNetCore.Mvc;
using SharedArea.Commands.User;
using SharedArea.Entities;
using SharedArea.Utils;

namespace ApiGateway.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        [Route("~/api/user/update_user_profile")]
        [HttpPost]
        public async Task<ActionResult<Packet>> UpdateProfile([FromBody] Packet packet)
        {
            using (var dbContext = new DatabaseContext())
            {
                var app = Security.AuthenticateApp(dbContext, Request.Headers[AuthExtracter.AppKeyHeader]);
                if (app == null) return new Packet() {Status = "error_100"};
                
                var session = Security.Authenticate<UserSession>(dbContext, Request.Headers[AuthExtracter.AK]);
                if (session == null) return new Packet() {Status = "error_0"};
                
                var result = await SharedArea.Transport
                    .RequestService<UpdateUserProfileRequest, UpdateUserProfileResponse>(
                        Startup.Bus,
                        SharedArea.GlobalVariables.CITY_QUEUE_NAME,
                        session,
                        Request.Headers.ToDictionary(a => a.Key, a => a.Value.ToString()),
                        packet);

                return result.Packet;
            }
        }

        [Route("~/api/user/get_me")]
        [HttpPost]
        public async Task<ActionResult<Packet>> GetMe()
        {
            using (var context = new DatabaseContext())
            {
                var app = Security.AuthenticateApp(context, Request.Headers[AuthExtracter.AppKeyHeader]);
                if (app == null) return new Packet() {Status = "error_100"};
                
                var session = Security.Authenticate<UserSession>(context, Request.Headers[AuthExtracter.AK]);
                if (session == null) return new Packet {Status = "error_0"};

                var result = await SharedArea.Transport.RequestService<GetMeRequest, GetMeResponse>(
                    Startup.Bus,
                    SharedArea.GlobalVariables.CITY_QUEUE_NAME,
                    session,
                    Request.Headers.ToDictionary(a => a.Key, a => a.Value.ToString()));
            
                return result.Packet;
            }
        }

        [Route("~/api/user/get_user_by_id")]
        [HttpPost]
        public async Task<ActionResult<Packet>> GetUserById([FromBody] Packet packet)
        {
            using (var dbContext = new DatabaseContext())
            {
                var app = Security.AuthenticateApp(dbContext, Request.Headers[AuthExtracter.AppKeyHeader]);
                if (app == null) return new Packet() {Status = "error_100"};
                
                var result = await SharedArea.Transport.RequestService<GetUserByIdRequest, GetUserByIdResponse>(
                    Startup.Bus,
                    SharedArea.GlobalVariables.CITY_QUEUE_NAME,
                    Request.Headers.ToDictionary(a => a.Key, a => a.Value.ToString()),
                    packet);

                return result.Packet;
            }
        }

        [Route("~/api/user/search_users")]
        [HttpPost]
        public async Task<ActionResult<Packet>> SearchUsers([FromBody] Packet packet)
        {
            using (var dbContext = new DatabaseContext())
            {
                var app = Security.AuthenticateApp(dbContext, Request.Headers[AuthExtracter.AppKeyHeader]);
                if (app == null) return new Packet() {Status = "error_100"};
                
                var result = await SharedArea.Transport.RequestService<SearchUsersRequest, SearchUsersResponse>(
                    Startup.Bus,
                    SharedArea.GlobalVariables.MESSENGER_QUEUE_NAME,
                    Request.Headers.ToDictionary(a => a.Key, a => a.Value.ToString()),
                    packet);

                return result.Packet;
            }
        }
    }
}