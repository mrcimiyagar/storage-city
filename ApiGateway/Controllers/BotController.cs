
using System.Linq;
using System.Threading.Tasks;
using ApiGateway.DbContexts;
using ApiGateway.Utils;
using SharedArea.Middles;
using Microsoft.AspNetCore.Mvc;
using SharedArea.Commands.Bot;
using SharedArea.Entities;
using SharedArea.Utils;

namespace ApiGateway.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class BotController : Controller
    {
        [Route("~/api/robot/get_bots")]
        [HttpPost]
        public async Task<ActionResult<Packet>> GetBots()
        {
            using (var context = new DatabaseContext())
            {
                var app = Security.AuthenticateApp(context, Request.Headers[AuthExtracter.AppKeyHeader]);
                if (app == null) return new Packet() {Status = "error_100"};
                
                var session = Security.Authenticate<UserSession>(context, Request.Headers[AuthExtracter.AK]);
                if (session == null) return new Packet {Status = "error_0"};
                
                var result = await SharedArea.Transport.RequestService<GetBotsRequest, GetBotsResponse>(
                    Startup.Bus,
                    SharedArea.GlobalVariables.MESSENGER_QUEUE_NAME,
                    session,
                    Request.Headers.ToDictionary(a => a.Key, a => a.Value.ToString()));
            
                return result.Packet;
            }
        }

        [Route("~/api/robot/get_created_bots")]
        [HttpPost]
        public async Task<ActionResult<Packet>> GetCreatedBots()
        {
            using (var context = new DatabaseContext())
            {
                var app = Security.AuthenticateApp(context, Request.Headers[AuthExtracter.AppKeyHeader]);
                if (app == null) return new Packet() {Status = "error_100"};
                
                var session = Security.Authenticate<UserSession>(context, Request.Headers[AuthExtracter.AK]);
                if (session == null) return new Packet {Status = "error_0"};
                
                var result = await SharedArea.Transport.RequestService<GetCreatedBotsRequest, GetCreatedBotsResponse>(
                    Startup.Bus,
                    SharedArea.GlobalVariables.MESSENGER_QUEUE_NAME,
                    session,
                    Request.Headers.ToDictionary(a => a.Key, a => a.Value.ToString()));
            
                return result.Packet;
            }
        }

        [Route("~/api/robot/create_bot")]
        [HttpPost]
        public async Task<ActionResult<Packet>> CreateBot([FromBody] Packet packet)
        {
            using (var context = new DatabaseContext())
            {
                var app = Security.AuthenticateApp(context, Request.Headers[AuthExtracter.AppKeyHeader]);
                if (app == null) return new Packet() {Status = "error_100"};
                
                var session = Security.Authenticate<UserSession>(context, Request.Headers[AuthExtracter.AK]);
                if (session == null) return new Packet {Status = "error_0"};
                
                var result = await SharedArea.Transport.RequestService<CreateBotRequest, CreateBotResponse>(
                    Startup.Bus,
                    SharedArea.GlobalVariables.MESSENGER_QUEUE_NAME,
                    session,
                    Request.Headers.ToDictionary(a => a.Key, a => a.Value.ToString()),
                    packet);

                return result.Packet;
            }
        }

        [Route("~/api/robot/get_bot")]
        [HttpPost]
        public async Task<ActionResult<Packet>> GetBot([FromBody] Packet packet)
        {
            using (var context = new DatabaseContext())
            {
                var app = Security.AuthenticateApp(context, Request.Headers[AuthExtracter.AppKeyHeader]);
                if (app == null) return new Packet() {Status = "error_100"};
                
                var session = Security.Authenticate<UserSession>(context, Request.Headers[AuthExtracter.AK]);
                if (session == null) return new Packet {Status = "error_081"};
                
                var result = await SharedArea.Transport.RequestService<GetBotRequest, GetBotResponse>(
                    Startup.Bus,
                    SharedArea.GlobalVariables.MESSENGER_QUEUE_NAME,
                    session,
                    Request.Headers.ToDictionary(a => a.Key, a => a.Value.ToString()),
                    packet);
            
                return result.Packet;
            }
        }

        [Route("~/api/robot/update_bot_profile")]
        [HttpPost]
        public async Task<ActionResult<Packet>> UpdateBotProfile([FromBody] Packet packet)
        {
            using (var context = new DatabaseContext())
            {
                var app = Security.AuthenticateApp(context, Request.Headers[AuthExtracter.AppKeyHeader]);
                if (app == null) return new Packet() {Status = "error_100"};
                
                var session = Security.Authenticate<UserSession>(context, Request.Headers[AuthExtracter.AK]);
                if (session == null) return new Packet {Status = "error_1"};
                
                var result = await SharedArea.Transport.RequestService<UpdateBotProfileRequest, UpdateBotProfileResponse>(
                    Startup.Bus,
                    SharedArea.GlobalVariables.MESSENGER_QUEUE_NAME,
                    session,
                    Request.Headers.ToDictionary(a => a.Key, a => a.Value.ToString()),
                    packet);
            
                return result.Packet;
            }
        }

        [Route("~/api/robot/search_bots")]
        [HttpPost]
        public async Task<ActionResult<Packet>> SearchBots([FromBody] Packet packet)
        {
            using (var context = new DatabaseContext())
            {
                var app = Security.AuthenticateApp(context, Request.Headers[AuthExtracter.AppKeyHeader]);
                if (app == null) return new Packet() {Status = "error_100"};
                
                var result = await SharedArea.Transport.RequestService<SearchBotsRequest, SearchBotsResponse>(
                    Startup.Bus,
                    SharedArea.GlobalVariables.MESSENGER_QUEUE_NAME,
                    packet);

                return result.Packet;
            }
        }
    }
}