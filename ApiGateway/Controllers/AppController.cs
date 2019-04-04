using System.Linq;
using System.Threading.Tasks;
using ApiGateway.DbContexts;
using ApiGateway.Utils;
using Microsoft.AspNetCore.Mvc;
using SharedArea.Commands.App;
using SharedArea.Commands.Auth;
using SharedArea.Entities;
using SharedArea.Middles;
using SharedArea.Utils;

namespace ApiGateway.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class AppController : Controller
    {
        [Route("~/api/app/create_app")]
        [HttpPost]
        public ActionResult<Packet> CreateApp([FromBody] Packet packet)
        {
            using (var dbContext = new DatabaseContext())
            {
                var session = Security.Authenticate<UserSession>(dbContext, Request.Headers[AuthExtracter.AK]);
                if (session == null) return new Packet() {Status = "error_0"};

                dbContext.Entry(session).Reference(s => s.User).Load();
                var user = session.User;
                
                dbContext.Entry(user).Collection(u => u.Apps).Load();

                if (user.Apps.Count < 5)
                {
                    var app = new App()
                    {
                        Title = packet.App.Title,
                        Creator = user,
                        Token = Security.MakeKey64()
                    };

                    dbContext.Apps.Add(app);

                    dbContext.SaveChanges();

                    return new Packet()
                    {
                        Status = "success",
                        App = app
                    };
                }
                else
                {
                    return new Packet()
                    {
                        Status = "error_1"
                    };
                }
            }
        }
    }
}