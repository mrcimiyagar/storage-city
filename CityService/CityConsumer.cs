using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedArea.Commands.Auth;
using SharedArea.Commands.Bot;
using SharedArea.Commands.Internal.Notifications;
using SharedArea.Commands.Internal.Requests;
using SharedArea.Commands.User;
using SharedArea.Entities;
using SharedArea.Middles;

namespace CityService
{
    public class CityConsumer : IConsumer<RegisterRequest>, IConsumer<VerifyRequest>, IConsumer<DeleteAccountRequest>
        , IConsumer<LogoutRequest>, IConsumer<UpdateUserProfileRequest>, IConsumer<GetMeRequest>, IConsumer<SearchBotsRequest>
        , IConsumer<UpdateBotProfileRequest>, IConsumer<GetCreatedBotsRequest>, IConsumer<SearchUsersRequest>
        , IConsumer<GetBotRequest>, IConsumer<CreateBotRequest>
    {
        public async Task Consume(ConsumeContext<GetBotRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var sess = dbContext.Sessions.Find(context.Message.SessionId);

                var session = (UserSession) sess;
                
                dbContext.Entry(session).Reference(s => s.User).Load();
                var robot = dbContext.Bots.Find(packet.Bot.BotId);
                if (robot == null)
                {
                    await context.RespondAsync(new GetBotResponse()
                    {
                        Packet = new Packet {Status = "error_080"}
                    });
                    return;
                }

                dbContext.Entry(robot).Reference(r => r.BotSecret).Load();
                if (robot.BotSecret.CreatorId == session.UserId)
                {
                    await context.RespondAsync(new GetBotResponse()
                    {
                        Packet = new Packet {Status = "success", Bot = robot}
                    });
                    return;
                }

                using (var nextContext = new DatabaseContext())
                {
                    var nextBot = nextContext.Bots.Find(packet.Bot.BotId);
                    await context.RespondAsync(new GetBotResponse()
                    {
                        Packet = new Packet {Status = "success", Bot = nextBot}
                    });
                }
            }
        }

        public async Task Consume(ConsumeContext<CreateBotRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var sess = dbContext.Sessions.Find(context.Message.SessionId);

                var session = (UserSession) sess;
                
                dbContext.Entry(session).Reference(s => s.User).Load();
                var user = (User) session.User;

                var token = "+" + Security.MakeKey64();

                var bot = new Bot()
                {
                    Title = packet.Bot.Title,
                    Sessions = new List<Session>()
                    {
                        new BotSession()
                        {
                            Token = token
                        }
                    }
                };

                ((BotSession) bot.Sessions[0]).Bot = bot;
                                
                var botSecret = new BotSecret()
                {
                    Bot = bot,
                    Creator = user,
                    Token = token,
                };
                bot.BotSecret = botSecret;

                var botCreation = new BotCreation()
                {
                    Bot = bot,
                    Creator = user
                };
                dbContext.AddRange(bot, botSecret, botCreation);
                dbContext.SaveChanges();

                await context.RespondAsync(new CreateBotResponse()
                {
                    Packet = new Packet
                    {
                        Status = "success",
                        Bot = bot,
                        BotCreation = botCreation
                    }
                });
            }
        }
        
        public async Task Consume(ConsumeContext<SearchUsersRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var users = (from u in dbContext.Users
                    where EF.Functions.Like(u.Title, "%" + packet.SearchQuery + "%")
                    select u).ToList();

                await context.RespondAsync(new SearchUsersResponse()
                {
                    Packet = new Packet {Status = "success", Users = users}
                });
            }
        }
        
        public async Task Consume(ConsumeContext<GetCreatedBotsRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var sess = dbContext.Sessions.Find(context.Message.SessionId);

                var session = (UserSession) sess;
                
                dbContext.Entry(session).Reference(s => s.User).Load();
                var user = (User) session.User;
                var bots = new List<Bot>();
                dbContext.Entry(user).Collection(u => u.CreatedBots).Load();
                var creations = user.CreatedBots.ToList();
                foreach (var botCreation in user.CreatedBots)
                {
                    dbContext.Entry(botCreation).Reference(bc => bc.Bot).Load();
                    dbContext.Entry(botCreation.Bot).Reference(b => b.BotSecret).Load();
                    bots.Add(botCreation.Bot);
                }

                await context.RespondAsync(new GetCreatedBotsResponse()
                {
                    Packet = new Packet {Status = "success", Bots = bots, BotCreations = creations}
                });
            }
        }
        
        public async Task Consume(ConsumeContext<SearchBotsRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;

                var bots = (from b in dbContext.Bots
                    where EF.Functions.Like(b.Title, "%" + packet.SearchQuery + "%")
                    select b).ToList();

                await context.RespondAsync(new SearchBotsResponse()
                {
                    Packet = new Packet {Status = "success", Bots = bots}
                });
            }
        }
     
        public async Task Consume(ConsumeContext<UpdateBotProfileRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var sess = dbContext.Sessions.Find(context.Message.SessionId);

                var session = (UserSession) sess;
                
                dbContext.Entry(session).Reference(s => s.User).Load();
                var user = (User) session.User;
                dbContext.Entry(user).Collection(u => u.CreatedBots).Load();
                var botCreation = user.CreatedBots.Find(bc => bc.BotId == packet.Bot.BotId);
                if (botCreation == null)
                {
                    await context.RespondAsync(new UpdateBotProfileResponse()
                    {
                        Packet = new Packet {Status = "error_0"}
                    });
                    return;
                }

                dbContext.Entry(botCreation).Reference(bc => bc.Bot).Load();
                var bot = botCreation.Bot;
                bot.Title = packet.Bot.Title;
                dbContext.SaveChanges();

                await context.RespondAsync(new UpdateBotProfileResponse()
                {
                    Packet = new Packet {Status = "success"}
                });
            }
        }
        
        public async Task Consume(ConsumeContext<GetMeRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var session = (UserSession) dbContext.Sessions.Find(context.Message.SessionId);
                
                dbContext.Entry(session).Reference(s => s.User).Load();
                var user = session.User;
                dbContext.Entry(user).Reference(u => u.UserSecret).Load();
                
                await context.RespondAsync(new GetMeResponse()
                {
                    Packet = new Packet
                    {
                        Status = "success",
                        User = user,
                        UserSecret = user.UserSecret,
                    }
                });
            }
        }

        public async Task Consume(ConsumeContext<UpdateUserProfileRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;

                if (packet.User.Title.Length == 0)
                {
                    await context.RespondAsync(new UpdateUserProfileResponse()
                    {
                        Packet = new Packet() {Status = "error_2"}
                    });
                    return;
                }

                var session = (UserSession) dbContext.Sessions.Find(context.Message.SessionId);
                dbContext.Entry(session).Reference(s => s.User).Load();
                var user = session.User;
                user.Title = packet.User.Title;
                dbContext.SaveChanges();
                
                SharedArea.Transport.NotifyService<UserProfileUpdatedNotif>(
                    Program.Bus,
                    new Packet() {User = user},
                    SharedArea.GlobalVariables.AllQueuesExcept(new[]
                    {
                        SharedArea.GlobalVariables.CITY_QUEUE_NAME
                    }));

                await context.RespondAsync(new UpdateUserProfileResponse()
                {
                    Packet = new Packet()
                    {
                        Status = "success",
                    }
                });
            }
        }
        
        private const string EmailAddress = "keyhan.mohammadi1997@gmail.com";
        private const string EmailPassword = "2&b165sf4j)684tkt87El^o9w68i87u6s*4h48#98aq";

        public async Task Consume(ConsumeContext<RegisterRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var code = Security.MakeKey8();
                SendEmail(packet.Email,
                    "Verify new device",
                    "Your verification code is : " + code);
                var pending = dbContext.Pendings.FirstOrDefault(p => p.Email == packet.Email);
                if (pending == null)
                {
                    pending = new Pending
                    {
                        Email = packet.Email,
                        VerifyCode = code
                    };
                    dbContext.AddRange(pending);
                    dbContext.SaveChanges();
                }
                else
                {
                    pending.VerifyCode = code;
                }

                dbContext.SaveChanges();
                await context.RespondAsync(
                    new RegisterResponse()
                    {
                        Packet = new Packet()
                        {
                            Status = "success"
                        }
                    });
            }
        }

        public async Task Consume(ConsumeContext<VerifyRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var pending = dbContext.Pendings.FirstOrDefault(p => p.Email == packet.Email);
                if (pending != null)
                {
                    if (packet.VerifyCode == pending.VerifyCode)
                    {
                        User user;
                        var token = Security.MakeKey64();
                        var userAuth = dbContext.UserSecrets.FirstOrDefault(ua => ua.Email == packet.Email);
                        if (userAuth == null)
                        {
                            user = new User()
                            {
                                Title = "New User",
                                UserSecret = new UserSecret()
                                {
                                    Email = packet.Email
                                },
                            };
                            user.UserSecret.User = user;
        
                            dbContext.AddRange(user);

                            dbContext.SaveChanges();
                    
                            SharedArea.Transport.NotifyService<AccountCreatedNotif>(
                                Program.Bus,
                                new Packet()
                                {
                                    User = user,
                                    UserSecret = user.UserSecret,
                                },
                                SharedArea.GlobalVariables.AllQueuesExcept(new[]
                                {
                                    SharedArea.GlobalVariables.CITY_QUEUE_NAME,
                                }));

                            userAuth = user.UserSecret;
                        }
                        else
                        {
                            dbContext.Entry(userAuth).Reference(us => us.User).Load();
                            user = userAuth.User;
                        }

                        var session = new UserSession()
                        {
                            Token = token,
                            User = user
                        };

                        dbContext.AddRange(session);
                        dbContext.Pendings.Remove(pending);
                        dbContext.SaveChanges();

                        SharedArea.Transport.NotifyService<SessionCreatedNotif>(
                            Program.Bus,
                            new Packet() {Session = session, User = user},
                            SharedArea.GlobalVariables.AllQueuesExcept(new[]
                            {
                                SharedArea.GlobalVariables.CITY_QUEUE_NAME
                            }));

                        await SharedArea.Transport
                            .RequestApiGateway<ConsolidateSessionRequest, ConsolidateSessionResponse>(
                                Program.Bus,
                                new Packet() {Session = session});

                        dbContext.Entry(session).Reference(s => s.User).Load();

                        await context.RespondAsync(
                            new VerifyResponse()
                            {
                                Packet = new Packet()
                                {
                                    Status = "success",
                                    Session = session,
                                    UserSecret = userAuth,
                                }
                            });
                    }
                    else
                    {
                        await context.RespondAsync(
                            new VerifyResponse()
                            {
                                Packet = new Packet()
                                {
                                    Status = "error_020"
                                }
                            });
                    }
                }
                else
                {
                    await context.RespondAsync(
                        new VerifyResponse()
                        {
                            Packet = new Packet()
                            {
                                Status = "error_021"
                            }
                        });
                }
            }
        }

        public async Task Consume(ConsumeContext<DeleteAccountRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var session = (UserSession) dbContext.Sessions.Find(context.Message.SessionId);
                dbContext.Entry(session).Reference(s => s.User).Load();
                var user = session.User;
                dbContext.Entry(user).Collection(u => u.Sessions).Load();
                dbContext.Entry(user).Reference(u => u.UserSecret).Load();

                user.Title = "Deleted User";
                user.UserSecret.Email = "";
                dbContext.Sessions.RemoveRange(user.Sessions);

                dbContext.SaveChanges();

                var sesses = user.Sessions.ToList();

                SharedArea.Transport.NotifyService<AccountDeletedNotif>(
                    Program.Bus,
                    new Packet() {User = user, Sessions = sesses},
                    SharedArea.GlobalVariables.AllQueuesExcept(new[]
                    {
                        SharedArea.GlobalVariables.CITY_QUEUE_NAME
                    }));

                await context.RespondAsync(new DeleteAccountResponse()
                {
                    Packet = new Packet() {Status = "success"}
                });
            }
        }

        public async Task Consume(ConsumeContext<LogoutRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var session = dbContext.Sessions.Find(context.Message.SessionId);

                if (session != null)
                {
                    dbContext.Sessions.Remove(session);
                    dbContext.SaveChanges();
                }
                else
                {
                    session = new Session {SessionId = context.Message.SessionId};
                }

                SharedArea.Transport.NotifyService<LogoutNotif>(
                    Program.Bus,
                    new Packet() {Session = session},
                    SharedArea.GlobalVariables.AllQueuesExcept(new[]
                    {
                        SharedArea.GlobalVariables.CITY_QUEUE_NAME
                    }));

                await context.RespondAsync(
                    new LogoutResponse()
                    {
                        Packet = new Packet()
                        {
                            Status = "success"
                        }
                    });
            }
        }

        private static void SendEmail(string to, string subject, string content)
        {
            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(EmailAddress, EmailPassword),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = true
            };
            var mailMessage = new MailMessage
            {
                From = new MailAddress(EmailAddress)
            };
            mailMessage.To.Add(to);
            mailMessage.Body = content;
            mailMessage.Subject = subject;
            client.Send(mailMessage);
        }
    }
}