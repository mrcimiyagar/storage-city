using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SharedArea.Commands.Bot;
using SharedArea.Commands.Complex;
using SharedArea.Commands.File;
using SharedArea.Commands.Internal.Notifications;
using SharedArea.Commands.Internal.Requests;
using SharedArea.Commands.Internal.Responses;
using SharedArea.Commands.Message;
using SharedArea.Commands.Module;
using SharedArea.Commands.Pulse;
using SharedArea.Commands.Pushes;
using SharedArea.Commands.User;
using SharedArea.Entities;
using SharedArea.Forms;
using SharedArea.Middles;
using SharedArea.Notifications;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using File = System.IO.File;
using JsonSerializer = SharedArea.Utils.JsonSerializer;
using Version = SharedArea.Entities.Version;

namespace StorageService
{
    public class MessengerConsumer : IConsumer<UpdateBotProfileRequest>, IConsumer<GetBotRequest>, IConsumer<CreateBotRequest>, IConsumer<SubscribeBotRequest>
        , IConsumer<SearchBotsRequest>, IConsumer<GetSubscribedBotsRequest>, IConsumer<GetCreatedBotsRequest>, IConsumer<GetBotsRequest>
        , IConsumer<GetComplexWorkersRequest>, IConsumer<AddBotToRoomRequest>, IConsumer<UpdateWorkershipRequest>
        , IConsumer<RemoveBotFromRoomRequest>, IConsumer<GetWorkershipsRequest>, IConsumer<GetBotStoreContentRequest>, IConsumer<BotGetWorkershipsRequest>
        , IConsumer<SearchUsersRequest>, IConsumer<SearchComplexesRequest>, IConsumer<GetFileSizeRequest>, IConsumer<WriteToFileRequest>
        , IConsumer<UploadPhotoRequest>, IConsumer<UploadAudioRequest>, IConsumer<UploadVideoRequest>, IConsumer<DownloadFileRequest>
        , IConsumer<ClickBotViewRequest>, IConsumer<RequestBotViewRequest>, IConsumer<SendBotViewRequest>, IConsumer<UpdateBotViewRequest>
        , IConsumer<AnimateBotViewRequest>, IConsumer<RunCommandsOnBotViewRequest>, IConsumer<GetMessagesRequest>
        , IConsumer<CreateTextMessageRequest>, IConsumer<CreateFileMessageRequest>, IConsumer<BotCreateTextMessageRequest>
        , IConsumer<BotCreateFileMessageRequest>, IConsumer<PutServiceMessageRequest>, IConsumer<GetLastActionsRequest>
        , IConsumer<NotifyMessageSeenRequest>, IConsumer<GetMessageSeenCountRequest>, IConsumer<CreateModuleRequest>
        , IConsumer<UpdateModuleProfileRequest>, IConsumer<SearchModulesRequest>, IConsumer<BotAppendTextToTxtFileRequest>
        , IConsumer<BotExecuteSqlOnSqlFileRequest>, IConsumer<BotExecuteMongoComOnMongoFileRequest>, IConsumer<BotCreateDocumentFileRequest>
        , IConsumer<BotPermitModuleRequest>, IConsumer<GetModuleServerAddressRequest>
    {
        private readonly string _dirPath;

        public MessengerConsumer()
        {
            _dirPath = Path.Combine(Path.GetFullPath(Directory.GetCurrentDirectory()), "Aseman");
            Directory.CreateDirectory(_dirPath);
        }

        public async Task Consume(ConsumeContext<GetModuleServerAddressRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;

                var module = (Module) dbContext.BaseUsers.Find(packet.Module.BaseUserId);
                
                dbContext.Entry(module).Reference(m => m.ModuleSecret).Load();

                await context.RespondAsync(new GetModuleServerAddressResponse()
                {
                    Packet = new Packet()
                    {
                        ModuleSecret = new ModuleSecret() {ServerAddress = module.ModuleSecret.ServerAddress}
                    }
                });
            }
        }
        
        public async Task Consume(ConsumeContext<CreateModuleRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var session = dbContext.Sessions.Find(context.Message.SessionId);
                
                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var user = (User) session.BaseUser;
                var module = new Module();

                var token = "-" + Security.MakeKey64();

                var modSess = new Session()
                {
                    Token = token
                };
                
                var result = await SharedArea.Transport.RequestService<ModuleCreatedWithBackRequest, ModuleCreatedWithBackResponse>(
                    Program.Bus,
                    SharedArea.GlobalVariables.CITY_QUEUE_NAME,
                    new Packet() {Module = module, Session = modSess});

                module.BaseUserId = result.Packet.Module.BaseUserId;
                module.Title = packet.Module.Title;
                module.Avatar = packet.Module.Avatar > 0 ? packet.Module.Avatar : 0;
                module.Description = packet.Module.Description;

                modSess.SessionId = result.Packet.Module.Sessions[0].SessionId;
                modSess.BaseUser = module;
                
                var modSecret = new ModuleSecret()
                {
                    Module = module,
                    Creator = user,
                    Token = token,
                    ServerAddress = packet.ModuleSecret.ServerAddress
                };
                module.ModuleSecret = modSecret;

                var modCreation = new ModuleCreation()
                {
                    Module = module,
                    Creator = user
                };
                dbContext.AddRange(module, modSecret, modSess, modCreation);
                dbContext.SaveChanges();
                
                var versions = new List<Version>()
                {
                    new Version()
                    {
                        VersionId = "BaseUser_" + module.BaseUserId + "_MessengerService",
                        Number = module.Version
                    },
                    new Version()
                    {
                        VersionId = "Session_" + modSess.SessionId + "_MessengerService",
                        Number = module.Version
                    }
                };
                
                SharedArea.Transport.NotifyPacketRouter<EntitiesVersionUpdatedNotif>(
                    Program.Bus,
                    new Packet()
                    {
                        Versions = versions
                    });

                await context.RespondAsync(new CreateModuleResponse()
                {
                    Packet = new Packet
                    {
                        Status = "success",
                        Module = module,
                        ModuleSecret = modSecret,
                        ModuleCreation = modCreation,
                        Versions = versions
                    }
                });
            }
        }
        
        public async Task Consume(ConsumeContext<UpdateModuleProfileRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var session = dbContext.Sessions.Find(context.Message.SessionId);

                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var user = (User) session.BaseUser;
                dbContext.Entry(user).Collection(u => u.CreatedModules).Load();
                var modCreation = user.CreatedModules.Find(bc => bc.ModuleId == packet.Module.BaseUserId);
                if (modCreation == null)
                {
                    await context.RespondAsync(new UpdateModuleProfileResponse()
                    {
                        Packet = new Packet {Status = "error_1"}
                    });
                    return;
                }

                dbContext.Entry(modCreation).Reference(bc => bc.Module).Load();
                dbContext.Entry(modCreation.Module).Reference(m => m.ModuleSecret).Load();
                var module = modCreation.Module;
                module.Title = packet.Module.Title;
                module.Avatar = packet.Module.Avatar;
                module.Description = packet.Module.Description;
                module.ModuleSecret.ServerAddress = packet.ModuleSecret.ServerAddress;
                dbContext.SaveChanges();

                await context.RespondAsync(new UpdateModuleProfileResponse()
                {
                    Packet = new Packet {Status = "success"}
                });
            }
        }
        
        public async Task Consume(ConsumeContext<SearchModulesRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var modules = (from u in dbContext.BaseUsers where u is Module
                    where EF.Functions.Like(u.Title, "%" + packet.SearchQuery + "%")
                    select u).Select(bu => (Module) bu).ToList();

                await context.RespondAsync(new SearchModulesResponse()
                {
                    Packet = new Packet {Status = "success", Modules = modules}
                });
            }
        }

        public async Task Consume(ConsumeContext<BotPermitModuleRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var session = dbContext.Sessions.Find(context.Message.SessionId);
                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var bot = (Bot) session.BaseUser;
                
                dbContext.Entry(bot).Collection(b => b.ModulePermissions).Load();
                var modPermis = bot.ModulePermissions.Find(mp => mp.ModuleId == packet.Module.BaseUserId);

                if (modPermis != null)
                {
                    await context.RespondAsync(new BotPermitModuleResponse()
                    {
                        Packet = new Packet() {Status = "error_1"}
                    });
                    return;
                }

                var module = dbContext.BaseUsers.Find(packet.Module.BaseUserId) as Module;
                if (module == null)
                {
                    await context.RespondAsync(new BotPermitModuleResponse()
                    {
                        Packet = new Packet() {Status = "error_2"}
                    });
                    return;
                }

                modPermis = new ModulePermission()
                {
                    Bot = bot,
                    Module = module
                };

                dbContext.ModulePermissions.Add(modPermis);

                dbContext.SaveChanges();

                dbContext.Entry(module).Collection(m => m.Sessions).Load();
                var sessionIds = new List<long>() {module.Sessions.FirstOrDefault().SessionId};
                
                ModulePermission finalModPermis;
                using (var finalContext = new DatabaseContext())
                {
                    finalModPermis = finalContext.ModulePermissions.Find(modPermis.ModulePermissionId);
                    finalContext.Entry(finalModPermis).Reference(mp => mp.Bot).Load();
                    finalContext.Entry(finalModPermis).Reference(mp => mp.Module).Load();
                }
                
                SharedArea.Transport.Push<ModulePermissionGrantedPush>(
                    Program.Bus,
                    new ModulePermissionGrantedPush()
                    {
                        Notif = new ModulePermissionGrantedNotification()
                        {
                            ModulePermission = finalModPermis
                        },
                        SessionIds = sessionIds
                    });

                await context.RespondAsync(new BotPermitModuleResponse()
                {
                    Packet = new Packet() {Status = "success", ModulePermission = finalModPermis}
                });
            }
        }

        public async Task Consume(ConsumeContext<BotCreateDocumentFileRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var session = dbContext.Sessions.Find(context.Message.SessionId);
                
                Document document;
                FileUsage fileUsage;

                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var user = session.BaseUser;

                if (packet.Room.RoomId > 0)
                {
                    if (user is Module mod)
                    {
                        dbContext.Entry(mod).Collection(m => m.ModulePermissions).Load();
                        var modPermis = mod.ModulePermissions.Find(mp => mp.BotId == packet.Bot.BaseUserId);
                        if (modPermis == null)
                        {
                            await context.RespondAsync(new BotCreateDocumentFileResponse()
                            {
                                Packet = new Packet() {Status = "error_1"}
                            });
                            return;
                        }
                        
                        dbContext.Entry(modPermis).Reference(mp => mp.Bot).Load();
                        user = modPermis.Bot;
                    }

                    var bot = (Bot) user;
                    
                    dbContext.Entry(bot).Collection(b => b.Workerships).Load();
                    var ws = bot.Workerships.Find(w => w.RoomId == packet.Room.RoomId);
                    if (ws == null)
                    {
                        await context.RespondAsync(new BotCreateDocumentFileResponse()
                        {
                            Packet = new Packet {Status = "error_2"}
                        });
                        return;
                    }

                    dbContext.Entry(ws).Reference(w => w.Room).Load();
                    var room = ws.Room;
                    if (room == null)
                    {
                        await context.RespondAsync(new BotCreateDocumentFileResponse()
                        {
                            Packet = new Packet {Status = "error_3"}
                        });
                        return;
                    }

                    document = new Document()
                    {
                        IsPublic = false,
                        Uploader = user,
                        Name = packet.Document.Name
                    };
                    dbContext.Files.Add(document);
                    fileUsage = new FileUsage()
                    {
                        File = document,
                        Room = room
                    };
                    dbContext.FileUsages.Add(fileUsage);
                }
                else
                {
                    document = new Document()
                    {
                        IsPublic = true,
                        Uploader = user,
                        Name = packet.Document.Name
                    };
                    dbContext.Files.Add(document);
                    fileUsage = null;
                }

                dbContext.SaveChanges();

                var filePath = Path.Combine(_dirPath, document.FileId.ToString());
                System.IO.File.Create(filePath).Close();

                SharedArea.Transport.NotifyService<PhotoCreatedNotif>(
                    Program.Bus,
                    new Packet() {Document = document, FileUsage = fileUsage, BaseUser = user},
                    new[]
                    {
                        SharedArea.GlobalVariables.MESSENGER_QUEUE_NAME
                    });

                await context.RespondAsync(new BotCreateDocumentFileResponse()
                {
                    Packet = new Packet {Status = "success", File = document, FileUsage = fileUsage}
                });
            }
        }

        public async Task Consume(ConsumeContext<BotAppendTextToTxtFileRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var session = dbContext.Sessions.Find(context.Message.SessionId);
                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var user = session.BaseUser;
                
                if (user is Module mod)
                {
                    dbContext.Entry(mod).Collection(m => m.ModulePermissions).Load();
                    var modPermis = mod.ModulePermissions.Find(mp => mp.BotId == packet.Bot.BaseUserId);
                    if (modPermis == null)
                    {
                        await context.RespondAsync(new BotAppendTextToTxtFileResponse()
                        {
                            Packet = new Packet() {Status = "error_1"}
                        });
                        return;
                    }
                    
                    dbContext.Entry(modPermis).Reference(mp => mp.Bot).Load();
                    user = modPermis.Bot;
                }

                var bot = (Bot) user;

                var file = dbContext.Files.Find(packet.File.FileId);
                
                if (file is Document doc)
                {
                    if (file.UploaderId == bot.BaseUserId)
                    {
                        await File.AppendAllTextAsync(Path.Combine(_dirPath, file.FileId.ToString()), packet.Text);

                        await context.RespondAsync(new BotAppendTextToTxtFileResponse()
                        {
                            Packet = new Packet() {Status = "success"}
                        });
                    }
                    else
                    {
                        await context.RespondAsync(new BotAppendTextToTxtFileResponse()
                        {
                            Packet = new Packet() {Status = "error_2"}
                        });
                    }
                }
                else
                {
                    await context.RespondAsync(new BotAppendTextToTxtFileResponse()
                    {
                        Packet = new Packet() {Status = "error_3"}
                    });
                } 
            }
        }
        
        public async Task Consume(ConsumeContext<BotExecuteSqlOnSqlFileRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var session = dbContext.Sessions.Find(context.Message.SessionId);
                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var user = session.BaseUser;
                
                if (user is Module mod)
                {
                    dbContext.Entry(mod).Collection(m => m.ModulePermissions).Load();
                    var modPermis = mod.ModulePermissions.Find(mp => mp.BotId == packet.Bot.BaseUserId);
                    if (modPermis == null)
                    {
                        await context.RespondAsync(new BotExecuteSqlOnSqlFileResponse()
                        {
                            Packet = new Packet() {Status = "error_1"}
                        });
                        return;
                    }
                    
                    dbContext.Entry(modPermis).Reference(mp => mp.Bot).Load();
                    user = modPermis.Bot;
                }

                var bot = (Bot) user;

                var file = dbContext.Files.Find(packet.File.FileId);

                if (file.UploaderId != bot.BaseUserId)
                {
                    await context.RespondAsync(new BotExecuteSqlOnSqlFileResponse()
                    {
                        Packet = new Packet() {Status = "error_2"}
                    });
                    return;
                }
                
                var dbConnection = new SqliteConnection("Data Source=" + Path.Combine(_dirPath, file.FileId.ToString()) + ";");
                dbConnection.Open();
                var command = new SqliteCommand(packet.SqlCommand.SqlScript, dbConnection);
                try
                {
                    if (packet.SqlCommand.IsQuery && !packet.SqlCommand.MustReturnId)
                    {
                        var sqliteDataReader = command.ExecuteReader();
                        var r = ConverSqliteDataReaderToDict(sqliteDataReader);
                        var result = JsonConvert.SerializeObject(r);
                        await context.RespondAsync(new BotExecuteSqlOnSqlFileResponse()
                        {
                            Packet = new Packet()
                            {
                                Status = "success",
                                SqlResult = new SqlResult() {QueryResultJson = result}
                            }
                        });
                    }
                    else if (!packet.SqlCommand.IsQuery && !packet.SqlCommand.MustReturnId)
                    {
                        var result = command.ExecuteNonQuery();
                        await context.RespondAsync(new BotExecuteSqlOnSqlFileResponse()
                        {
                            Packet = new Packet()
                            {
                                Status = "success",
                                SqlResult = new SqlResult() {NonQueryResultNumber = result}
                            }
                        });
                    }
                    else if (packet.SqlCommand.MustReturnId)
                    {
                        var result = Convert.ToInt64(command.ExecuteScalar());
                        await context.RespondAsync(new BotExecuteSqlOnSqlFileResponse()
                        {
                            Packet = new Packet()
                            {
                                Status = "success",
                                SqlResult = new SqlResult() {ScalarResultNumber = result}
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    await context.RespondAsync(new BotExecuteSqlOnSqlFileResponse()
                    {
                        Packet = new Packet()
                        {
                            Status = "error_3",
                            SqlResult = new SqlResult() {ErrorMessage = ex.ToString()}
                        }
                    });
                }
                finally
                {
                    dbConnection.Close();
                }
            }
        }
        
        public IEnumerable<Dictionary<string, object>> ConverSqliteDataReaderToDict(SqliteDataReader reader)
        {
            var results = new List<Dictionary<string, object>>();
            var cols = new List<string>();
            for (var i = 0; i < reader.FieldCount; i++) 
                cols.Add(reader.GetName(i));

            while (reader.Read()) 
                results.Add(BuildSqliteRowInDict(cols, reader));

            return results;
        }
        private Dictionary<string, object> BuildSqliteRowInDict(IEnumerable<string> cols, 
            SqliteDataReader reader) {
            var result = new Dictionary<string, object>();
            foreach (var col in cols) 
                result.Add(col, reader[col]);
            return result;
        }
        
        public async Task Consume(ConsumeContext<BotExecuteMongoComOnMongoFileRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var session = dbContext.Sessions.Find(context.Message.SessionId);
                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var user = session.BaseUser;
                
                if (user is Module mod)
                {
                    dbContext.Entry(mod).Collection(m => m.ModulePermissions).Load();
                    var modPermis = mod.ModulePermissions.Find(mp => mp.BotId == packet.Bot.BaseUserId);
                    if (modPermis == null)
                    {
                        await context.RespondAsync(new BotCreateDocumentFileResponse()
                        {
                            Packet = new Packet() {Status = "error_1"}
                        });
                        return;
                    }
                        
                    dbContext.Entry(modPermis).Reference(mp => mp.Bot).Load();
                    user = modPermis.Bot;
                }

                var bot = (Bot) user;

                var file = dbContext.Files.Find(packet.File.FileId);

                if (file.UploaderId != bot.BaseUserId)
                {
                    await context.RespondAsync(new BotExecuteMongoComOnMongoFileResponse()
                    {
                        Packet = new Packet() {Status = "error_2"}
                    });
                    return;
                }
                
                await context.RespondAsync(new BotExecuteMongoComOnMongoFileResponse()
                {
                    Packet = new Packet() {Status = "success"}
                });
            }
        }
        
        public async Task Consume(ConsumeContext<NotifyMessageSeenRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var session = dbContext.Sessions.Find(context.Message.SessionId);
                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var user = (User) session.BaseUser;
                var message = dbContext.Messages.Find(context.Message.Packet.Message.MessageId);
                if (message == null)
                {
                    await context.RespondAsync(new NotifyMessageSeenResponse()
                    {
                        Packet = new Packet() {Status = "error_2"}
                    });
                    return;
                }

                var messageSeen = dbContext.MessageSeens.Find(user.BaseUserId + "_" + message.MessageId);
                if (messageSeen != null)
                {
                    await context.RespondAsync(new NotifyMessageSeenResponse()
                    {
                        Packet = new Packet() {Status = "error_3"}
                    });
                    return;
                }

                dbContext.Entry(message).Reference(m => m.Room).Load();
                var room = message.Room;
                dbContext.Entry(room).Reference(r => r.Complex).Load();
                var complex = room.Complex;
                dbContext.Entry(complex).Collection(c => c.Members).Load();
                if (complex.Members.Any(m => m.UserId == user.BaseUserId))
                {
                    if (message.AuthorId == user.BaseUserId)
                    {
                        await context.RespondAsync(new NotifyMessageSeenResponse()
                        {
                            Packet = new Packet() {Status = "error_5"}
                        });
                        return;
                    }

                    messageSeen = new MessageSeen()
                    {
                        MessageSeenId = user.BaseUserId + "_" + message.MessageId,
                        Message = message,
                        User = user
                    };
                    dbContext.MessageSeens.Add(messageSeen);

                    dbContext.SaveChanges();
                    var notif = new MessageSeenNotification()
                    {
                        MessageId = message.MessageId,
                        MessageSeenCount =
                            dbContext.MessageSeens.LongCount(ms => ms.MessageId == message.MessageId)
                    };
                    dbContext.Entry(complex)
                        .Collection(c => c.Members).Query()
                        .Include(m => m.User)
                        .ThenInclude(u => u.Sessions)
                        .Load();
                    var push = new MessageSeenPush()
                    {
                        Notif = notif,
                        SessionIds = (from m in complex.Members
                            where m.User.BaseUserId != user.BaseUserId
                            from s in m.User.Sessions
                            select s.SessionId).ToList()
                    };
                    SharedArea.Transport.Push<MessageSeenPush>(
                        Program.Bus,
                        push);

                    await context.RespondAsync(new NotifyMessageSeenResponse()
                    {
                        Packet = new Packet() {Status = "success"}
                    });
                }
                else
                {
                    await context.RespondAsync(new NotifyMessageSeenResponse()
                    {
                        Packet = new Packet() {Status = "error_4"}
                    });
                }
            }
        }

        public async Task Consume(ConsumeContext<GetMessageSeenCountRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var session = dbContext.Sessions.Find(context.Message.SessionId);

                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var user = (User) session.BaseUser;

                var message = dbContext.Messages.Find(context.Message.Packet.Message.MessageId);
                if (message == null)
                {
                    await context.RespondAsync(new NotifyMessageSeenResponse()
                    {
                        Packet = new Packet() {Status = "error_2"}
                    });
                    return;
                }

                dbContext.Entry(message).Reference(m => m.Room).Load();
                var room = message.Room;
                dbContext.Entry(room).Reference(r => r.Complex).Load();
                var complex = room.Complex;
                dbContext.Entry(complex).Collection(c => c.Members).Load();
                if (complex.Members.Any(m => m.UserId == user.BaseUserId))
                {
                    var seenCount = dbContext.MessageSeens.LongCount(ms => ms.MessageId == message.MessageId);

                    await context.RespondAsync(new NotifyMessageSeenResponse()
                    {
                        Packet = new Packet() {Status = "success", MessageSeenCount = seenCount}
                    });
                }
                else
                {
                    await context.RespondAsync(new NotifyMessageSeenResponse()
                    {
                        Packet = new Packet() {Status = "error_3"}
                    });
                }
            }
        }

        public async Task Consume(ConsumeContext<GetLastActionsRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var rooms = context.Message.Packet.Rooms;
                
                var session = dbContext.Sessions.Find(context.Message.SessionId);
                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var user = (User) session.BaseUser;
                dbContext.Entry(user).Collection(u => u.Memberships).Load();

                foreach (var roomC in rooms)
                {
                    var membership = user.Memberships.Find(mem => mem.ComplexId == roomC.ComplexId);
                    if (membership == null)
                    {
                        await context.RespondAsync(new GetLastActionsResponse()
                        {
                            Packet = new Packet() {Status = "error_1"}
                        });
                        return;
                    }

                    dbContext.Entry(membership).Reference(m => m.Complex).Load();
                    var complex = membership.Complex;
                    dbContext.Entry(complex).Collection(c => c.Rooms).Load();
                    var room = complex.Rooms.Find(r => r.RoomId == roomC.RoomId);
                    if (room == null)
                    {
                        await context.RespondAsync(new GetLastActionsResponse()
                        {
                            Packet = new Packet() {Status = "error_2"}
                        });
                        return;
                    }

                    roomC.LastAction = dbContext.Messages.Last(m => m.RoomId == room.RoomId);
                    
                    if (roomC.LastAction != null)
                    {
                        dbContext.Entry(roomC.LastAction).Reference(m => m.Author).Load();
                        
                        if (roomC.LastAction.GetType() == typeof(PhotoMessage))
                        {
                            dbContext.Entry(roomC.LastAction).Reference(m => ((PhotoMessage) m).Photo).Load();
                            dbContext.Entry(((PhotoMessage) roomC.LastAction).Photo).Collection(f => f.FileUsages)
                                .Query().Where(fu => fu.RoomId == roomC.RoomId).Load();
                        }
                        else if (roomC.LastAction.GetType() == typeof(AudioMessage))
                        {
                            dbContext.Entry(roomC.LastAction).Reference(m => ((AudioMessage) m).Audio).Load();
                            dbContext.Entry(((AudioMessage) roomC.LastAction).Audio).Collection(f => f.FileUsages)
                                .Query().Where(fu => fu.RoomId == roomC.RoomId).Load();
                        }
                        else if (roomC.LastAction.GetType() == typeof(VideoMessage))
                        {
                            dbContext.Entry(roomC.LastAction).Reference(m => ((VideoMessage) m).Video).Load();
                            dbContext.Entry(((VideoMessage) roomC.LastAction).Video).Collection(f => f.FileUsages)
                                .Query().Where(fu => fu.RoomId == roomC.RoomId).Load();
                        }
                        
                        roomC.LastAction.SeenByMe =
                            (dbContext.MessageSeens.Find(user.BaseUserId + "_" + roomC.LastAction.MessageId) != null);
                        roomC.LastAction.SeenCount =
                            dbContext.MessageSeens.LongCount(ms => ms.MessageId == roomC.LastAction.MessageId);
                    }
                }

                await context.RespondAsync(new GetLastActionsResponse()
                {
                    Packet = new Packet() {Status = "success", Rooms = rooms}
                });
            }
        }
        
        public async Task Consume(ConsumeContext<GetMessagesRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                if (packet.FetchNext == null)
                {
                    await context.RespondAsync(new GetMessagesResponse()
                    {
                        Packet = new Packet() {Status = "error_1"}
                    });
                    return;
                }
                
                var session = dbContext.Sessions.Find(context.Message.SessionId);

                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var user = (User) session.BaseUser;
                dbContext.Entry(user).Collection(u => u.Memberships).Load();
                var membership = user.Memberships.Find(m => m.ComplexId == packet.Complex.ComplexId);
                if (membership == null)
                {
                    await context.RespondAsync(new GetMessagesResponse()
                    {
                        Packet = new Packet {Status = "error_0U1"}
                    });
                    return;
                }

                dbContext.Entry(membership).Reference(m => m.Complex).Load();
                var complex = membership.Complex;
                dbContext.Entry(complex).Collection(c => c.Rooms).Load();
                var room = complex.Rooms.Find(r => r.RoomId == packet.Room.RoomId);
                if (room == null)
                {
                    await context.RespondAsync(new GetMessagesResponse()
                    {
                        Packet = new Packet {Status = "error_0U2"}
                    });
                    return;
                }

                dbContext.Entry(room).Collection(r => r.Messages).Load();

                List<Message> messages;
                
                if (packet.Message.MessageId > 0)
                {
                    messages = packet.FetchNext.Value
                        ? room.Messages.Where(m =>
                                m.MessageId > packet.Message.MessageId && m.MessageId - packet.Message.MessageId <= 100)
                            .ToList()
                        : room.Messages.Where(m =>
                                m.MessageId < packet.Message.MessageId && packet.Message.MessageId - m.MessageId <= 100)
                            .ToList();
                }
                else
                {
                    messages = room.Messages.TakeLast(100).ToList();
                }

                foreach (var msg in messages)
                {
                    if (msg.GetType() == typeof(PhotoMessage))
                    {
                        dbContext.Entry(msg).Reference(m => ((PhotoMessage) m).Photo).Load();
                        dbContext.Entry(((PhotoMessage) msg).Photo).Collection(f => f.FileUsages).Load();
                    }
                    else if (msg.GetType() == typeof(AudioMessage))
                    {
                        dbContext.Entry(msg).Reference(m => ((AudioMessage) m).Audio).Load();
                        dbContext.Entry(((AudioMessage) msg).Audio).Collection(f => f.FileUsages).Load();
                    }
                    else if (msg.GetType() == typeof(VideoMessage))
                    {
                        dbContext.Entry(msg).Reference(m => ((VideoMessage) m).Video).Load();
                        dbContext.Entry(((VideoMessage) msg).Video).Collection(f => f.FileUsages).Load();
                    }

                    msg.SeenByMe = (dbContext.MessageSeens.Find(user.BaseUserId + "_" + msg.MessageId) != null);
                }

                await context.RespondAsync(new GetMessagesResponse()
                {
                    Packet = new Packet {Status = "success", Messages = messages}
                });
            }
        }

        public async Task Consume(ConsumeContext<CreateTextMessageRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var session = dbContext.Sessions.Find(context.Message.SessionId);

                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var human = (User) session.BaseUser;
                dbContext.Entry(human).Collection(h => h.Memberships).Load();
                var membership = human.Memberships.Find(m => m.ComplexId == packet.Complex.ComplexId);
                if (membership == null)
                {
                    await context.RespondAsync(new CreateTextMessageResponse()
                    {
                        Packet = new Packet {Status = "error_1"}
                    });
                    return;
                }

                dbContext.Entry(membership).Reference(m => m.Complex).Load();
                var complex = membership.Complex;
                dbContext.Entry(complex).Collection(c => c.Rooms).Load();
                var room = complex.Rooms.Find(r => r.RoomId == packet.Room.RoomId);
                if (room == null)
                {
                    await context.RespondAsync(new CreateTextMessageResponse()
                    {
                        Packet = new Packet {Status = "error_2"}
                    });
                    return;
                }

                var message = new TextMessage()
                {
                    Author = human,
                    Room = room,
                    Text = packet.TextMessage.Text,
                    Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                dbContext.Messages.Add(message);
                dbContext.SaveChanges();
                dbContext.Entry(human).Collection(h => h.Sessions).Load();
                dbContext.Entry(complex)
                    .Collection(c => c.Members)
                    .Query().Include(m => m.User)
                    .ThenInclude(u => u.Sessions)
                    .Load();
                dbContext.Entry(room).Collection(r => r.Workers).Load();
                TextMessage nextMessage;
                using (var nextContext = new DatabaseContext())
                {
                    nextMessage = (TextMessage) nextContext.Messages.Find(message.MessageId);
                    nextContext.Entry(nextMessage).Reference(m => m.Room).Load();
                    nextContext.Entry(nextMessage.Room).Reference(r => r.Complex).Load();
                    nextContext.Entry(nextMessage).Reference(m => m.Author).Load();
                }

                var sessionIds = (from m in complex.Members
                    where m.User.BaseUserId != human.BaseUserId
                    from s in m.User.Sessions
                    select s.SessionId).ToList();
                sessionIds.AddRange((from w in room.Workers
                    from s in dbContext.Bots.Include(b => b.Sessions)
                        .FirstOrDefault(b => b.BaseUserId == w.BotId)
                        ?.Sessions
                    select s.SessionId).ToList());
                var mcn = new TextMessageNotification()
                {
                    Message = nextMessage
                };
                SharedArea.Transport.Push<TextMessagePush>(
                    Program.Bus,
                    new TextMessagePush()
                    {
                        Notif = mcn,
                        SessionIds = sessionIds
                    });

                await context.RespondAsync(new CreateTextMessageResponse()
                {
                    Packet = new Packet {Status = "success", TextMessage = nextMessage}
                });
            }
        }

        public async Task Consume(ConsumeContext<CreateFileMessageRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var session = dbContext.Sessions.Find(context.Message.SessionId);

                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var human = (User) session.BaseUser;
                dbContext.Entry(human).Collection(h => h.Memberships).Load();
                var membership = human.Memberships.Find(mem => mem.ComplexId == packet.Complex.ComplexId);
                if (membership == null)
                {
                    await context.RespondAsync(new CreateFileMessageResponse()
                    {
                        Packet = new Packet {Status = "error_1"}
                    });
                    return;
                }

                dbContext.Entry(membership).Reference(mem => mem.Complex).Load();
                var complex = membership.Complex;
                dbContext.Entry(complex).Collection(c => c.Rooms).Load();
                var room = complex.Rooms.Find(r => r.RoomId == packet.Room.RoomId);
                if (room == null)
                {
                    await context.RespondAsync(new CreateFileMessageResponse()
                    {
                        Packet = new Packet {Status = "error_2"}
                    });
                    return;
                }

                Message message = null;
                dbContext.Entry(room).Collection(r => r.Files).Load();
                var fileUsage = room.Files.Find(fu => fu.FileId == packet.File.FileId);
                if (fileUsage == null)
                {
                    await context.RespondAsync(new CreateFileMessageResponse()
                    {
                        Packet = new Packet {Status = "error_3"}
                    });
                    return;
                }

                dbContext.Entry(fileUsage).Reference(fu => fu.File).Load();
                var file = fileUsage.File;
                switch (file)
                {
                    case Photo photo:
                        message = new PhotoMessage()
                        {
                            Author = human,
                            Room = room,
                            Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            Photo = photo
                        };
                        break;
                    case Audio audio:
                        message = new AudioMessage()
                        {
                            Author = human,
                            Room = room,
                            Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            Audio = audio
                        };
                        break;
                    case Video video:
                        message = new VideoMessage()
                        {
                            Author = human,
                            Room = room,
                            Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            Video = video
                        };
                        break;
                }

                if (message == null)
                {
                    await context.RespondAsync(new CreateFileMessageResponse()
                    {
                        Packet = new Packet {Status = "error_4"}
                    });
                    return;
                }

                dbContext.Messages.Add(message);
                dbContext.SaveChanges();

                Message nextMessage;
                using (var nextContext = new DatabaseContext())
                {
                    nextMessage = nextContext.Messages
                        .Include(msg => msg.Room)
                        .ThenInclude(r => r.Complex)
                        .Include(msg => msg.Author)
                        .FirstOrDefault(msg => msg.MessageId == message.MessageId);
                    switch (nextMessage)
                    {
                        case PhotoMessage _:
                            nextContext.Entry(nextMessage).Reference(msg => ((PhotoMessage) msg).Photo).Load();
                            nextContext.Entry(((PhotoMessage) nextMessage).Photo).Collection(f => f.FileUsages)
                                .Query().Where(fu => fu.FileUsageId == fileUsage.FileUsageId).Load();
                            break;
                        case AudioMessage _:
                            nextContext.Entry(nextMessage).Reference(msg => ((AudioMessage) msg).Audio).Load();
                            nextContext.Entry(((AudioMessage) nextMessage).Audio).Collection(f => f.FileUsages)
                                .Query().Where(fu => fu.FileUsageId == fileUsage.FileUsageId).Load();
                            break;
                        case VideoMessage _:
                            nextContext.Entry(nextMessage).Reference(msg => ((VideoMessage) msg).Video).Load();
                            nextContext.Entry(((VideoMessage) nextMessage).Video).Collection(f => f.FileUsages)
                                .Query().Where(fu => fu.FileUsageId == fileUsage.FileUsageId).Load();
                            break;
                    }
                }

                dbContext.Entry(complex)
                    .Collection(c => c.Members)
                    .Query().Include(m => m.User)
                    .ThenInclude(u => u.Sessions)
                    .Load();
                dbContext.Entry(room).Collection(r => r.Workers).Load();
                var sessionIds = (from m in complex.Members
                    where m.User.BaseUserId != human.BaseUserId
                    from s in m.User.Sessions
                    select s.SessionId).ToList();
                sessionIds.AddRange((from w in room.Workers
                    from s in dbContext.Bots.Include(b => b.Sessions)
                        .FirstOrDefault(b => b.BaseUserId == w.BotId)
                        ?.Sessions
                    select s.SessionId).ToList());

                switch (nextMessage)
                {
                    case PhotoMessage msg:
                    {
                        var notif = new PhotoMessageNotification()
                        {
                            Message = msg,
                        };
                        SharedArea.Transport.Push<PhotoMessagePush>(
                            Program.Bus,
                            new PhotoMessagePush()
                            {
                                Notif = notif,
                                SessionIds = sessionIds
                            });
                        break;
                    }
                    case AudioMessage msg:
                    {
                        var notif = new AudioMessageNotification()
                        {
                            Message = msg
                        };
                        SharedArea.Transport.Push<AudioMessagePush>(
                            Program.Bus,
                            new AudioMessagePush()
                            {
                                Notif = notif,
                                SessionIds = sessionIds
                            });
                        break;
                    }
                    case VideoMessage msg:
                    {
                        var notif = new VideoMessageNotification()
                        {
                            Message = msg
                        };
                        SharedArea.Transport.Push<VideoMessagePush>(
                            Program.Bus,
                            new VideoMessagePush()
                            {
                                Notif = notif,
                                SessionIds = sessionIds
                            });
                        break;
                    }
                }

                switch (nextMessage)
                {
                    case PhotoMessage msg:
                        await context.RespondAsync(new CreateFileMessageResponse()
                        {
                            Packet = new Packet {Status = "success", PhotoMessage = msg}
                        });
                        break;
                    case AudioMessage msg:
                        await context.RespondAsync(new CreateFileMessageResponse()
                        {
                            Packet = new Packet {Status = "success", AudioMessage = msg}
                        });
                        break;
                    case VideoMessage msg:
                        await context.RespondAsync(new CreateFileMessageResponse()
                        {
                            Packet = new Packet {Status = "success", VideoMessage = msg}
                        });
                        break;
                    default:
                        await context.RespondAsync(new CreateFileMessageResponse()
                        {
                            Packet = new Packet {Status = "error_5"}
                        });
                        break;
                }
            }
        }

        public async Task Consume(ConsumeContext<BotCreateTextMessageRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var session = dbContext.Sessions.Find(context.Message.SessionId);
                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var bot = (Bot) session.BaseUser;
                dbContext.Entry(bot).Reference(b => b.BotSecret).Load();
                var complex = dbContext.Complexes.Find(packet.Complex.ComplexId);
                if (complex == null)
                {
                    await context.RespondAsync(new BotCreateTextMessageResponse()
                    {
                        Packet = new Packet {Status = "error_3"}
                    });
                    return;
                }

                dbContext.Entry(complex).Collection(c => c.Rooms).Load();
                var room = complex.Rooms.Find(r => r.RoomId == packet.Room.RoomId);
                if (room == null)
                {
                    await context.RespondAsync(new BotCreateTextMessageResponse()
                    {
                        Packet = new Packet {Status = "error_4"}
                    });
                    return;
                }

                dbContext.Entry(room).Collection(r => r.Workers).Load();
                var workership = room.Workers.Find(w => w.BotId == bot.BaseUserId);
                if (workership == null)
                {
                    await context.RespondAsync(new BotCreateTextMessageResponse()
                    {
                        Packet = new Packet {Status = "error_5"}
                    });
                    return;
                }

                var message = new TextMessage()
                {
                    Author = bot,
                    Room = room,
                    Text = packet.TextMessage.Text,
                    Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                dbContext.Messages.Add(message);
                dbContext.SaveChanges();
                TextMessage nextMessage;
                using (var nextContext = new DatabaseContext())
                {
                    nextMessage = (TextMessage) nextContext.Messages.Find(message.MessageId);
                    nextContext.Entry(nextMessage).Reference(m => m.Room).Load();
                    nextContext.Entry(nextMessage.Room).Reference(r => r.Complex).Load();
                    nextContext.Entry(nextMessage).Reference(m => m.Author).Load();
                }

                dbContext.Entry(complex)
                    .Collection(c => c.Members)
                    .Query().Include(m => m.User)
                    .ThenInclude(u => u.Sessions)
                    .Load();
                dbContext.Entry(room).Collection(r => r.Workers).Load();

                var sessionIds = (from m in complex.Members
                    from s in m.User.Sessions
                    select s.SessionId).ToList();
                sessionIds.AddRange((from w in room.Workers
                    where w.BotId != bot.BaseUserId
                    from s in dbContext.Bots.Include(b => b.Sessions)
                        .FirstOrDefault(b => b.BaseUserId == w.BotId)
                        ?.Sessions
                    select s.SessionId).ToList());
                var mcn = new TextMessageNotification()
                {
                    Message = nextMessage
                };
                SharedArea.Transport.Push<TextMessagePush>(
                    Program.Bus,
                    new TextMessagePush()
                    {
                        Notif = mcn,
                        SessionIds = sessionIds
                    });

                await context.RespondAsync(new BotCreateTextMessageResponse()
                {
                    Packet = new Packet {Status = "success", TextMessage = nextMessage}
                });
            }
        }

        public async Task Consume(ConsumeContext<BotCreateFileMessageRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var session = dbContext.Sessions.Find(context.Message.SessionId);
                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var bot = (Bot) session.BaseUser;
                dbContext.Entry(bot).Reference(b => b.BotSecret).Load();
                var complex = dbContext.Complexes.Find(packet.Complex.ComplexId);
                if (complex == null)
                {
                    await context.RespondAsync(new BotCreateFileMessageResponse()
                    {
                        Packet = new Packet {Status = "error_3"}
                    });
                    return;
                }

                dbContext.Entry(complex).Collection(c => c.Rooms).Load();
                var room = complex.Rooms.Find(r => r.RoomId == packet.Room.RoomId);
                if (room == null)
                {
                    await context.RespondAsync(new BotCreateFileMessageResponse()
                    {
                        Packet = new Packet {Status = "error_4"}
                    });
                    return;
                }

                dbContext.Entry(room).Collection(r => r.Workers).Load();
                var workership = room.Workers.Find(w => w.BotId == bot.BaseUserId);
                if (workership == null)
                {
                    await context.RespondAsync(new BotCreateFileMessageResponse()
                    {
                        Packet = new Packet {Status = "error_5"}
                    });
                    return;
                }

                Message message = null;
                dbContext.Entry(room).Collection(r => r.Files).Load();
                var fileUsage = room.Files.Find(fu => fu.FileId == packet.File.FileId);
                if (fileUsage == null)
                {
                    await context.RespondAsync(new BotCreateFileMessageResponse()
                    {
                        Packet = new Packet {Status = "error_6"}
                    });
                    return;
                }

                dbContext.Entry(fileUsage).Reference(fu => fu.File).Load();
                var file = fileUsage.File;
                switch (file)
                {
                    case Photo photo:
                        message = new PhotoMessage()
                        {
                            Author = bot,
                            Room = room,
                            Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            Photo = photo
                        };
                        break;
                    case Audio audio:
                        message = new AudioMessage()
                        {
                            Author = bot,
                            Room = room,
                            Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            Audio = audio
                        };
                        break;
                    case Video video:
                        message = new VideoMessage()
                        {
                            Author = bot,
                            Room = room,
                            Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            Video = video
                        };
                        break;
                }

                if (message == null)
                {
                    await context.RespondAsync(new BotCreateFileMessageResponse()
                    {
                        Packet = new Packet {Status = "error_7"}
                    });
                    return;
                }

                dbContext.Messages.Add(message);
                dbContext.SaveChanges();

                Message nextMessage;
                using (var nextContext = new DatabaseContext())
                {
                    nextMessage = nextContext.Messages.Find(message.MessageId);
                    nextContext.Entry(nextMessage).Reference(msg => msg.Room).Load();
                    nextContext.Entry(nextMessage.Room).Reference(r => r.Complex).Load();
                    nextContext.Entry(nextMessage).Reference(msg => msg.Author).Load();
                    switch (nextMessage)
                    {
                        case PhotoMessage _:
                            nextContext.Entry(nextMessage).Reference(msg => ((PhotoMessage) msg).Photo).Load();
                            nextContext.Entry(((PhotoMessage) nextMessage).Photo).Collection(f => f.FileUsages)
                                .Query().Where(fu => fu.FileUsageId == fileUsage.FileUsageId).Load();
                            break;
                        case AudioMessage _:
                            nextContext.Entry(nextMessage).Reference(msg => ((AudioMessage) msg).Audio).Load();
                            nextContext.Entry(((AudioMessage) nextMessage).Audio).Collection(f => f.FileUsages)
                                .Query().Where(fu => fu.FileUsageId == fileUsage.FileUsageId).Load();
                            break;
                        case VideoMessage _:
                            nextContext.Entry(nextMessage).Reference(msg => ((VideoMessage) msg).Video).Load();
                            nextContext.Entry(((VideoMessage) nextMessage).Video).Collection(f => f.FileUsages)
                                .Query().Where(fu => fu.FileUsageId == fileUsage.FileUsageId).Load();
                            break;
                    }
                }

                dbContext.Entry(complex)
                    .Collection(c => c.Members)
                    .Query().Include(m => m.User)
                    .ThenInclude(u => u.Sessions)
                    .Load();
                dbContext.Entry(room).Collection(r => r.Workers).Load();

                var sessionIds = (from m in complex.Members
                    from s in m.User.Sessions
                    select s.SessionId).ToList();
                sessionIds.AddRange((from w in room.Workers
                    where w.BotId != bot.BaseUserId
                    from s in dbContext.Bots.Include(b => b.Sessions)
                        .FirstOrDefault(b => b.BaseUserId == w.BotId)
                        ?.Sessions
                    select s.SessionId).ToList());

                switch (nextMessage)
                {
                    case PhotoMessage msg:
                    {
                        var notif = new PhotoMessageNotification()
                        {
                            Message = msg
                        };
                        SharedArea.Transport.Push<PhotoMessagePush>(
                            Program.Bus,
                            new PhotoMessagePush()
                            {
                                Notif = notif,
                                SessionIds = sessionIds
                            });
                        break;
                    }
                    case AudioMessage msg:
                    {
                        var notif = new AudioMessageNotification()
                        {
                            Message = msg
                        };
                        SharedArea.Transport.Push<AudioMessagePush>(
                            Program.Bus,
                            new AudioMessagePush()
                            {
                                Notif = notif,
                                SessionIds = sessionIds
                            });
                        break;
                    }
                    case VideoMessage msg:
                    {
                        var notif = new VideoMessageNotification()
                        {
                            Message = msg
                        };
                        SharedArea.Transport.Push<VideoMessagePush>(
                            Program.Bus,
                            new VideoMessagePush()
                            {
                                Notif = notif,
                                SessionIds = sessionIds
                            });
                        break;
                    }
                }

                switch (nextMessage)
                {
                    case PhotoMessage msg:
                        await context.RespondAsync(new BotCreateFileMessageResponse()
                        {
                            Packet = new Packet {Status = "success", PhotoMessage = msg}
                        });
                        return;
                    case AudioMessage msg:
                        await context.RespondAsync(new BotCreateFileMessageResponse()
                        {
                            Packet = new Packet {Status = "success", AudioMessage = msg}
                        });
                        return;
                    case VideoMessage msg:
                        await context.RespondAsync(new BotCreateFileMessageResponse()
                        {
                            Packet = new Packet {Status = "success", VideoMessage = msg}
                        });
                        return;
                    default:
                        await context.RespondAsync(new BotCreateFileMessageResponse()
                        {
                            Packet = new Packet {Status = "error_5"}
                        });
                        return;
                }
            }
        }

        public async Task Consume(ConsumeContext<PutServiceMessageRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var message = context.Message.Packet.ServiceMessage;

                var room = dbContext.Rooms.Find(message.Room.RoomId);

                message.Room = room;

                dbContext.Messages.Add(message);

                message = (ServiceMessage) dbContext.Messages.Find(message.MessageId);

                dbContext.SaveChanges();

                await context.RespondAsync(new PutServiceMessageResponse()
                {
                    Packet = new Packet() {ServiceMessage = message}
                });
            }
        }
        
        public async Task Consume(ConsumeContext<RequestBotViewRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                try
                {
                    var packet = context.Message.Packet;
                    var session = dbContext.Sessions.Find(context.Message.SessionId);

                    dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                    var user = (User) session.BaseUser;

                    var complexId = packet.Complex.ComplexId;
                    var roomId = packet.Room.RoomId;
                    var botId = packet.Bot.BaseUserId;
                    var sessionId = context.Message.SessionId;

                    dbContext.Entry(user).Collection(u => u.Memberships).Load();
                    var mem = user.Memberships.Find(m => m.ComplexId == complexId);
                    if (mem == null)
                    {
                        await context.RespondAsync(new RequestBotViewResponse()
                        {
                            Packet = new Packet() {Status = "error_1"}
                        });
                        return;
                    }

                    dbContext.Entry(mem).Reference(m => m.Complex).Load();
                    var complex = mem.Complex;
                    if (complex == null)
                    {
                        await context.RespondAsync(new RequestBotViewResponse()
                        {
                            Packet = new Packet() {Status = "error_2"}
                        });
                        return;
                    }

                    dbContext.Entry(complex).Collection(c => c.Rooms).Load();
                    var room = complex.Rooms.Find(r => r.RoomId == roomId);
                    if (room == null)
                    {
                        await context.RespondAsync(new RequestBotViewResponse()
                        {
                            Packet = new Packet() {Status = "error_3"}
                        });
                        return;
                    }

                    dbContext.Entry(room).Collection(r => r.Workers).Load();
                    var worker = room.Workers.Find(w => w.BotId == botId);
                    if (worker == null)
                    {
                        await context.RespondAsync(new RequestBotViewResponse()
                        {
                            Packet = new Packet() {Status = "error_4"}
                        });
                        return;
                    }

                    var bot = dbContext.Bots.Find(botId);
                    dbContext.Entry(bot).Collection(b => b.Sessions).Load();

                    User finalUser;
                    using (var finalContext = new DatabaseContext())
                    {
                        finalUser = (User) finalContext.BaseUsers.Find(user.BaseUserId);
                    }

                    var notif = new UserRequestedBotViewNotification()
                    {
                        ComplexId = complexId,
                        RoomId = roomId,
                        BotId = botId,
                        User = finalUser
                    };

                    SharedArea.Transport.Push<UserRequestedBotViewPush>(
                        Program.Bus,
                        new UserRequestedBotViewPush()
                        {
                            Notif = notif,
                            SessionIds = new List<long> {bot.Sessions.FirstOrDefault().SessionId}
                        });

                    await context.RespondAsync(new RequestBotViewResponse()
                    {
                        Packet = new Packet() {Status = "success"}
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        public async Task Consume(ConsumeContext<SendBotViewRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var session = dbContext.Sessions.Find(context.Message.SessionId);

                var complexId = packet.Complex.ComplexId;
                var roomId = packet.Room.RoomId;
                var userId = packet.User.BaseUserId;
                var viewData = packet.RawJson;

                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var bot = (Bot) session.BaseUser;
                
                var complex = dbContext.Complexes.Find(complexId);
                if (complex == null)
                {
                    await context.RespondAsync(new SendBotViewResponse()
                    {
                        Packet = new Packet() {Status = "error_1"}
                    });
                    return;
                }
                dbContext.Entry(complex).Collection(c => c.Rooms).Load();
                var room = complex.Rooms.Find(r => r.RoomId == roomId);
                if (room == null)
                {
                    await context.RespondAsync(new SendBotViewResponse()
                    {
                        Packet = new Packet() {Status = "error_2"}
                    });
                    return;
                }
                dbContext.Entry(room).Collection(r => r.Workers).Load();
                var worker = room.Workers.Find(w => w.BotId == bot.BaseUserId);
                if (worker == null)
                {
                    await context.RespondAsync(new SendBotViewResponse()
                    {
                        Packet = new Packet() {Status = "error_3"}
                    });
                    return;
                }

                User user = null;
                if (userId > 0)
                {
                    dbContext.Entry(complex).Collection(c => c.Members).Load();
                    user = (User) dbContext.BaseUsers.Find(userId);
                    dbContext.Entry(user).Collection(u => u.Sessions).Load();
                    var membership = complex.Members.Find(m => m.UserId == user.BaseUserId);
                    if (membership == null)
                    {
                        await context.RespondAsync(new UpdateBotViewResponse()
                        {
                            Packet = new Packet() {Status = "error_4"}
                        });
                        return;
                    }
                }
                else
                {
                    dbContext.Entry(complex).Collection(c => c.Members).Query()
                        .Include(m => m.User).ThenInclude(u => u.Sessions).Load();  
                }
                
                var notif = new BotSentBotViewNotification()
                {
                    ComplexId = complexId,
                    RoomId = roomId,
                    BotId = bot.BaseUserId,
                    ViewData = viewData
                };

                var sessionIds = user == null ? (from m in complex.Members from s in m.User.Sessions 
                    select s.SessionId).ToList() : user.Sessions.Select(s => s.SessionId).ToList();
                
                SharedArea.Transport.Push<BotSentBotViewPush>(
                    Program.Bus,
                    new BotSentBotViewPush()
                    {
                        SessionIds = sessionIds,
                        Notif = notif
                    });

                await context.RespondAsync(new SendBotViewResponse()
                {
                    Packet = new Packet() {Status = "success"}
                });
            }
        }
        
        public async Task Consume(ConsumeContext<UpdateBotViewRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var session = dbContext.Sessions.Find(context.Message.SessionId);

                var complexId = packet.Complex.ComplexId;
                var roomId = packet.Room.RoomId;
                var userId = packet.User.BaseUserId;
                var viewData = packet.RawJson;
                var batchData = packet.BatchData;

                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var bot = (Bot) session.BaseUser;

                var complex = dbContext.Complexes.Find(complexId);
                if (complex == null)
                {
                    await context.RespondAsync(new UpdateBotViewResponse()
                    {
                        Packet = new Packet() {Status = "error_1"}
                    });
                    return;
                }
                dbContext.Entry(complex).Collection(c => c.Rooms).Load();
                var room = complex.Rooms.Find(r => r.RoomId == roomId);
                if (room == null)
                {
                    await context.RespondAsync(new UpdateBotViewResponse()
                    {
                        Packet = new Packet() {Status = "error_2"}
                    });
                    return;
                }
                dbContext.Entry(room).Collection(r => r.Workers).Load();
                var worker = room.Workers.Find(w => w.BotId == bot.BaseUserId);
                if (worker == null)
                {
                    await context.RespondAsync(new UpdateBotViewResponse()
                    {
                        Packet = new Packet() {Status = "error_3"}
                    });
                    return;
                }

                User user = null;
                if (userId > 0)
                {
                    dbContext.Entry(complex).Collection(c => c.Members).Load();
                    user = (User) dbContext.BaseUsers.Find(userId);
                    dbContext.Entry(user).Collection(u => u.Sessions).Load();
                    var membership = complex.Members.Find(m => m.UserId == user.BaseUserId);
                    if (membership == null)
                    {
                        await context.RespondAsync(new UpdateBotViewResponse()
                        {
                            Packet = new Packet() {Status = "error_4"}
                        });
                        return;
                    }
                }
                else
                {
                    dbContext.Entry(complex).Collection(c => c.Members).Query()
                        .Include(m => m.User).ThenInclude(u => u.Sessions).Load();     
                }

                var notif = new BotUpdatedBotViewNotification()
                {
                    ComplexId = complexId,
                    RoomId = roomId,
                    BotId = bot.BaseUserId,
                    UpdateData = viewData,
                    BatchData = batchData ?? false
                };
                
                var sessionIds = user == null ? (from m in complex.Members from s in m.User.Sessions 
                    select s.SessionId).ToList() : user.Sessions.Select(s => s.SessionId).ToList();
                
                SharedArea.Transport.Push<BotUpdatedBotViewPush>(
                    Program.Bus,
                    new BotUpdatedBotViewPush()
                    {
                        SessionIds = sessionIds,
                        Notif = notif
                    });

                await context.RespondAsync(new UpdateBotViewResponse()
                {
                    Packet = new Packet() {Status = "success"}
                });
            }
        }

        public async Task Consume(ConsumeContext<AnimateBotViewRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var session = dbContext.Sessions.Find(context.Message.SessionId);

                var complexId = packet.Complex.ComplexId;
                var roomId = packet.Room.RoomId;
                var userId = packet.User.BaseUserId;
                var viewData = packet.RawJson;
                var batchData = packet.BatchData;

                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var bot = (Bot) session.BaseUser;

                var complex = dbContext.Complexes.Find(complexId);
                if (complex == null)
                {
                    await context.RespondAsync(new AnimateBotViewResponse()
                    {
                        Packet = new Packet() {Status = "error_1"}
                    });
                    return;
                }
                dbContext.Entry(complex).Collection(c => c.Rooms).Load();
                var room = complex.Rooms.Find(r => r.RoomId == roomId);
                if (room == null)
                {
                    await context.RespondAsync(new AnimateBotViewResponse()
                    {
                        Packet = new Packet() {Status = "error_2"}
                    });
                    return;
                }
                dbContext.Entry(room).Collection(r => r.Workers).Load();
                var worker = room.Workers.Find(w => w.BotId == bot.BaseUserId);
                if (worker == null)
                {
                    await context.RespondAsync(new AnimateBotViewResponse()
                    {
                        Packet = new Packet() {Status = "error_3"}
                    });
                    return;
                }

                User user = null;
                if (userId > 0)
                {
                    dbContext.Entry(complex).Collection(c => c.Members).Load();
                    user = (User) dbContext.BaseUsers.Find(userId);
                    dbContext.Entry(user).Collection(u => u.Sessions).Load();
                    var membership = complex.Members.Find(m => m.UserId == user.BaseUserId);
                    if (membership == null)
                    {
                        await context.RespondAsync(new UpdateBotViewResponse()
                        {
                            Packet = new Packet() {Status = "error_4"}
                        });
                        return;
                    }
                }
                else
                {
                    dbContext.Entry(complex).Collection(c => c.Members).Query()
                        .Include(m => m.User).ThenInclude(u => u.Sessions).Load();  
                }

                var notif = new BotAnimatedBotViewNotification()
                {
                    ComplexId = complexId,
                    RoomId = roomId,
                    BotId = bot.BaseUserId,
                    AnimData = viewData,
                    BatchData = batchData ?? false
                };
                
                var sessionIds = user == null ? (from m in complex.Members from s in m.User.Sessions 
                    select s.SessionId).ToList() : user.Sessions.Select(s => s.SessionId).ToList();
                
                SharedArea.Transport.Push<BotAnimatedBotViewPush>(
                    Program.Bus,
                    new BotAnimatedBotViewPush()
                    {
                        SessionIds = sessionIds,
                        Notif = notif
                    });

                await context.RespondAsync(new AnimateBotViewResponse()
                {
                    Packet = new Packet() {Status = "success"}
                });
            }
        }

        public async Task Consume(ConsumeContext<RunCommandsOnBotViewRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var session = dbContext.Sessions.Find(context.Message.SessionId);

                var complexId = packet.Complex.ComplexId;
                var roomId = packet.Room.RoomId;
                var userId = packet.User.BaseUserId;
                var viewData = packet.RawJson;
                var batchData = packet.BatchData;

                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var bot = (Bot) session.BaseUser;

                var complex = dbContext.Complexes.Find(complexId);
                if (complex == null)
                {
                    await context.RespondAsync(new RunCommandsOnBotViewResponse()
                    {
                        Packet = new Packet() {Status = "error_1"}
                    });
                    return;
                }
                dbContext.Entry(complex).Collection(c => c.Rooms).Load();
                var room = complex.Rooms.Find(r => r.RoomId == roomId);
                if (room == null)
                {
                    await context.RespondAsync(new RunCommandsOnBotViewResponse()
                    {
                        Packet = new Packet() {Status = "error_2"}
                    });
                    return;
                }
                dbContext.Entry(room).Collection(r => r.Workers).Load();
                var worker = room.Workers.Find(w => w.BotId == bot.BaseUserId);
                if (worker == null)
                {
                    await context.RespondAsync(new RunCommandsOnBotViewResponse()
                    {
                        Packet = new Packet() {Status = "error_3"}
                    });
                    return;
                }

                User user = null;
                if (userId > 0)
                {
                    dbContext.Entry(complex).Collection(c => c.Members).Load();
                    user = (User) dbContext.BaseUsers.Find(userId);
                    dbContext.Entry(user).Collection(u => u.Sessions).Load();
                    var membership = complex.Members.Find(m => m.UserId == user.BaseUserId);
                    if (membership == null)
                    {
                        await context.RespondAsync(new UpdateBotViewResponse()
                        {
                            Packet = new Packet() {Status = "error_4"}
                        });
                        return;
                    }
                }
                else
                {
                    dbContext.Entry(complex).Collection(c => c.Members).Query()
                        .Include(m => m.User).ThenInclude(u => u.Sessions).Load();  
                }

                var notif = new BotRanCommandsOnBotViewNotification()
                {
                    ComplexId = complexId,
                    RoomId = roomId,
                    BotId = bot.BaseUserId,
                    CommandsData = viewData,
                    BatchData = batchData ?? false
                };

                var sessionIds = user == null
                    ? (from m in complex.Members
                        from s in m.User.Sessions
                        select s.SessionId).ToList()
                    : user.Sessions.Select(s => s.SessionId).ToList();
                
                SharedArea.Transport.Push<BotRanCommandsOnBotViewPush>(
                    Program.Bus,
                    new BotRanCommandsOnBotViewPush()
                    {
                        SessionIds = sessionIds,
                        Notif = notif
                    });

                await context.RespondAsync(new RunCommandsOnBotViewResponse()
                {
                    Packet = new Packet() {Status = "success"}
                });
            }
        }

        public async Task Consume(ConsumeContext<ClickBotViewRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var session = dbContext.Sessions.Find(context.Message.SessionId);
                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var user = (User) session.BaseUser;

                var membership = dbContext.Memberships.FirstOrDefault(mem =>
                    mem.ComplexId == packet.Complex.ComplexId && mem.UserId == user.BaseUserId);
                if (membership == null)
                {
                    await context.RespondAsync(new ClickBotViewResponse()
                    {
                        Packet = new Packet() {Status = "error_1"}
                    });
                    return;
                }

                dbContext.Entry(membership).Reference(mem => mem.Complex).Load();
                var room = dbContext.Rooms.FirstOrDefault(r =>
                    r.ComplexId == packet.Complex.ComplexId && r.RoomId == packet.Room.RoomId);
                if (room == null)
                {
                    await context.RespondAsync(new ClickBotViewResponse()
                    {
                        Packet = new Packet() {Status = "error_2"}
                    });
                    return;
                }

                var workership = dbContext.Workerships.FirstOrDefault(w =>
                    w.RoomId == packet.Room.RoomId && w.BotId == packet.Bot.BaseUserId);
                if (workership == null)
                {
                    await context.RespondAsync(new ClickBotViewResponse()
                    {
                        Packet = new Packet() {Status = "error_3"}
                    }); 
                    return;
                }

                var notif = new UserClickedBotViewNotification()
                {
                    Complex = membership.Complex,
                    Room = room,
                    User = user,
                    ControlId = packet.ControlId
                };

                var bot = (Bot) dbContext.BaseUsers.Find(packet.Bot.BaseUserId);
                dbContext.Entry(bot).Collection(b => b.Sessions).Load();
                
                SharedArea.Transport.Push<UserClickedBotViewPush>(
                    Program.Bus,
                    new UserClickedBotViewPush()
                    {
                        Notif = notif,
                        SessionIds = new List<long> {bot.Sessions.FirstOrDefault().SessionId}
                    });

                await context.RespondAsync(new ClickBotViewResponse()
                {
                    Packet = new Packet() {Status = "success"}
                });
            }
        }
        
        public async Task Consume(ConsumeContext<GetFileSizeRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var file = dbContext.Files.Find(context.Message.Packet.File.FileId);
                if (file == null)
                {
                    await context.RespondAsync(new GetFileSizeResponse()
                    {
                        Packet = new Packet() {Status = "error_1"}
                    });
                    return;
                }

                await context.RespondAsync(new GetFileSizeResponse()
                {
                    Packet = new Packet() {Status = "success", File = file}
                });
            }
        }

        public async Task Consume(ConsumeContext<WriteToFileRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var session = dbContext.Sessions.Find(context.Message.SessionId);
                var streamCode = context.Message.Packet.StreamCode;

                var file = dbContext.Files.Find(context.Message.Packet.File.FileId);
                if (file == null)
                {
                    await context.RespondAsync(new WriteToFileResponse()
                    {
                        Packet = new Packet() {Status = "error_1"}
                    });
                    return;
                }

                if (file.UploaderId != session.BaseUserId)
                {
                    await context.RespondAsync(new WriteToFileResponse()
                    {
                        Packet = new Packet() {Status = "error_2"}
                    });
                    return;
                }

                var myContent = JsonConvert.SerializeObject(new Packet()
                {
                    Username = SharedArea.GlobalVariables.FILE_TRANSFER_USERNAME,
                    Password = SharedArea.GlobalVariables.FILE_TRANSFER_PASSWORD,
                    StreamCode = streamCode
                });
                var buffer = Encoding.UTF8.GetBytes(myContent);

                var client = WebRequest.Create(SharedArea.GlobalVariables.SERVER_URL +
                                               SharedArea.GlobalVariables.FILE_TRANSFER_GET_UPLOAD_STREAM_URL);
                client.Method = "POST";
                client.ContentType = "application/json";

                var newStream = client.GetRequestStream();
                newStream.Write(buffer, 0, buffer.Length);
                newStream.Close();

                var res = client.GetResponse();
                var responseStream = res.GetResponseStream();

                if (responseStream != null)
                {
                    Directory.CreateDirectory(_dirPath);
                    var filePath = Path.Combine(_dirPath, file.FileId.ToString());
                    System.IO.File.Create(filePath).Close();

                    using (var stream = new FileStream(filePath, FileMode.Append))
                    {
                        var b = new byte[128 * 1024];
                        int read;
                        while ((read = responseStream.Read(b, 0, b.Length)) > 0)
                        {
                            stream.Write(b, 0, read);
                            file.Size += read;
                            dbContext.SaveChanges();
                        }
                    }

                    if (file is Photo photo && photo.IsAvatar)
                    {
                        using (var image = Image.Load(filePath))
                        {
                            float width, height;
                            if (image.Width > image.Height)
                            {
                                width = image.Width > 256 ? 256 : image.Width;
                                height = (float) image.Height / (float) image.Width * width;
                            }
                            else
                            {
                                height = image.Height > 256 ? 256 : image.Height;
                                width = (float) image.Width / (float) image.Height * height;
                            }

                            image.Mutate(x => x.Resize((int) width, (int) height));
                            System.IO.File.Delete(filePath);
                            image.Save(filePath + ".png");
                        }
                    }
                }

                await context.RespondAsync(new WriteToFileResponse()
                {
                    Packet = new Packet() {Status = "success"}
                });
            }
        }

        public async Task Consume(ConsumeContext<UploadPhotoRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var form = context.Message.Form;
                var session = dbContext.Sessions.Find(context.Message.SessionId);

                Photo photo;
                FileUsage fileUsage;

                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var user = (User) session.BaseUser;

                if (form.RoomId > 0)
                {
                    dbContext.Entry(user).Collection(u => u.Memberships).Load();
                    var membership = user.Memberships.Find(m => m.ComplexId == form.ComplexId);
                    if (membership == null)
                    {
                        await context.RespondAsync(new UploadPhotoResponse()
                        {
                            Packet = new Packet {Status = "error_1"}
                        });
                        return;
                    }

                    dbContext.Entry(membership).Reference(m => m.Complex).Load();
                    var complex = membership.Complex;
                    dbContext.Entry(complex).Collection(c => c.Rooms).Load();
                    var room = complex.Rooms.Find(r => r.RoomId == form.RoomId);
                    if (room == null)
                    {
                        await context.RespondAsync(new UploadPhotoResponse()
                        {
                            Packet = new Packet {Status = "error_2"}
                        });
                        return;
                    }

                    photo = new Photo()
                    {
                        Width = form.Width,
                        Height = form.Height,
                        IsPublic = false,
                        Uploader = user,
                        IsAvatar = form.IsAvatar
                    };
                    dbContext.Files.Add(photo);
                    fileUsage = new FileUsage()
                    {
                        File = photo,
                        Room = room
                    };
                    dbContext.FileUsages.Add(fileUsage);
                }
                else
                {
                    photo = new Photo()
                    {
                        Width = form.Width,
                        Height = form.Height,
                        IsPublic = true,
                        Uploader = user,
                        IsAvatar = form.IsAvatar
                    };
                    dbContext.Files.Add(photo);
                    fileUsage = null;
                }

                dbContext.SaveChanges();

                var filePath = Path.Combine(_dirPath, photo.FileId.ToString());
                System.IO.File.Create(filePath).Close();

                SharedArea.Transport.NotifyService<PhotoCreatedNotif>(
                    Program.Bus,
                    new Packet() {Photo = photo, FileUsage = fileUsage, BaseUser = user},
                    new[]
                    {
                        SharedArea.GlobalVariables.MESSENGER_QUEUE_NAME
                    });

                await context.RespondAsync(new UploadPhotoResponse()
                {
                    Packet = new Packet {Status = "success", File = photo, FileUsage = fileUsage}
                });
            }
        }

        public async Task Consume(ConsumeContext<UploadAudioRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var form = context.Message.Form;
                var session = dbContext.Sessions.Find(context.Message.SessionId);
                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var user = (User) session.BaseUser;

                Audio audio;
                FileUsage fileUsage;

                if (form.RoomId > 0)
                {
                    dbContext.Entry(user).Collection(u => u.Memberships).Load();
                    var membership = user.Memberships.Find(m => m.ComplexId == form.ComplexId);
                    if (membership == null)
                    {
                        await context.RespondAsync(new UploadAudioResponse()
                        {
                            Packet = new Packet {Status = "error_1"}
                        });
                        return;
                    }

                    dbContext.Entry(membership).Reference(m => m.Complex).Load();
                    var complex = membership.Complex;
                    dbContext.Entry(complex).Collection(c => c.Rooms).Load();
                    var room = complex.Rooms.Find(r => r.RoomId == form.RoomId);
                    if (room == null)
                    {
                        await context.RespondAsync(new UploadAudioResponse()
                        {
                            Packet = new Packet {Status = "error_2"}
                        });
                        return;
                    }

                    audio = new Audio()
                    {
                        Title = form.Title,
                        Duration = form.Duration,
                        IsPublic = false,
                        Uploader = user,
                    };
                    dbContext.Files.Add(audio);
                    fileUsage = new FileUsage()
                    {
                        File = audio,
                        Room = room
                    };
                    dbContext.FileUsages.Add(fileUsage);
                }
                else
                {
                    audio = new Audio()
                    {
                        Title = form.Title,
                        Duration = form.Duration,
                        IsPublic = true,
                        Uploader = user
                    };
                    dbContext.Files.Add(audio);
                    fileUsage = null;
                }

                dbContext.SaveChanges();

                var filePath = Path.Combine(_dirPath, audio.FileId.ToString());
                System.IO.File.Create(filePath).Close();

                SharedArea.Transport.NotifyService<AudioCreatedNotif>(
                    Program.Bus,
                    new Packet() {Audio = audio, FileUsage = fileUsage, BaseUser = user},
                    new[]
                    {
                        SharedArea.GlobalVariables.MESSENGER_QUEUE_NAME
                    });

                await context.RespondAsync(new UploadAudioResponse()
                {
                    Packet = new Packet {Status = "success", File = audio, FileUsage = fileUsage}
                });
            }
        }

        public async Task Consume(ConsumeContext<UploadVideoRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var form = context.Message.Form;
                var session = dbContext.Sessions.Find(context.Message.SessionId);
                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var user = (User) session.BaseUser;

                Video video;
                FileUsage fileUsage;

                if (form.RoomId > 0)
                {
                    dbContext.Entry(user).Collection(u => u.Memberships).Load();
                    var membership = user.Memberships.Find(m => m.ComplexId == form.ComplexId);
                    if (membership == null)
                    {
                        await context.RespondAsync(new UploadVideoResponse()
                        {
                            Packet = new Packet {Status = "error_1"}
                        });
                        return;
                    }

                    dbContext.Entry(membership).Reference(m => m.Complex).Load();
                    var complex = membership.Complex;
                    dbContext.Entry(complex).Collection(c => c.Rooms).Load();
                    var room = complex.Rooms.Find(r => r.RoomId == form.RoomId);
                    if (room == null)
                    {
                        await context.RespondAsync(new UploadVideoResponse()
                        {
                            Packet = new Packet {Status = "error_2"}
                        });
                        return;
                    }

                    video = new Video()
                    {
                        Title = form.Title,
                        Duration = form.Duration,
                        IsPublic = false,
                        Uploader = user
                    };
                    dbContext.Files.Add(video);
                    fileUsage = new FileUsage()
                    {
                        File = video,
                        Room = room
                    };
                    dbContext.FileUsages.Add(fileUsage);
                }
                else
                {
                    video = new Video()
                    {
                        Title = form.Title,
                        Duration = form.Duration,
                        IsPublic = true,
                        Uploader = user
                    };
                    dbContext.Files.Add(video);
                    fileUsage = null;
                }

                dbContext.SaveChanges();

                var filePath = Path.Combine(_dirPath, video.FileId.ToString());
                System.IO.File.Create(filePath).Close();

                SharedArea.Transport.NotifyService<VideoCreatedNotif>(
                    Program.Bus,
                    new Packet() {Video = video, FileUsage = fileUsage, BaseUser = user},
                    new[]
                    {
                        SharedArea.GlobalVariables.MESSENGER_QUEUE_NAME
                    });

                await context.RespondAsync(new UploadVideoResponse()
                {
                    Packet = new Packet {Status = "success", File = video, FileUsage = fileUsage}
                });
            }
        }

        public async Task Consume(ConsumeContext<DownloadFileRequest> context)
        {
            var fileId = context.Message.Packet.File.FileId;

            if (System.IO.File.Exists(Path.Combine(_dirPath, fileId.ToString())) || System.IO.File.Exists(Path.Combine(_dirPath, fileId + ".png")))
            {
                var streamCode = context.Message.Packet.StreamCode;
                var offset = context.Message.Packet.Offset;
                using (var dbContext = new DatabaseContext())
                {
                    var session = dbContext.Sessions.Find(context.Message.SessionId);
                    if (session == null)
                    {
                        await context.RespondAsync(new DownloadFileResponse()
                        {
                            Packet = new Packet {Status = "error_0"}
                        });
                        return;
                    }

                    var file = dbContext.Files.Find(fileId);
                    if (file == null)
                    {
                        await context.RespondAsync(new DownloadFileResponse()
                        {
                            Packet = new Packet {Status = "error_1"}
                        });
                        return;
                    }

                    dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                    var user = (User) session.BaseUser;
                    dbContext.Entry(user).Collection(u => u.Memberships).Load();
                    if (file.IsPublic)
                    {
                        await UploadFileToApiGateWay(streamCode, file.FileId, offset.Value);
                        await context.RespondAsync(new DownloadFileResponse()
                        {
                            Packet = new Packet {Status = "success"}
                        });
                        return;
                    }

                    dbContext.Entry(file).Collection(f => f.FileUsages).Query().Include(fu => fu.Room).Load();
                    var foundPath = (from fu in file.FileUsages select fu.Room.ComplexId)
                        .Intersect(from mem in user.Memberships select mem.ComplexId).Any();
                    if (foundPath)
                    {
                        await UploadFileToApiGateWay(streamCode, file.FileId, offset.Value);
                        await context.RespondAsync(new DownloadFileResponse()
                        {
                            Packet = new Packet {Status = "success"}
                        });
                    }
                    else
                    {
                        await context.RespondAsync(new DownloadFileResponse()
                        {
                            Packet = new Packet {Status = "error_2"}
                        });
                    }
                }
            }
            else
            {
                await context.RespondAsync(new DownloadFileResponse()
                {
                    Packet = new Packet {Status = "error_3"}
                });
            }
        }

        private async Task UploadFileToApiGateWay(string streamCode, long fileId, long offset)
        {
            using (var client = new HttpClient())
            {
                using (var content = new MultipartFormDataContent())
                {
                    using (var stream =
                        System.IO.File.Exists(Path.Combine(_dirPath, fileId.ToString()))
                            ? System.IO.File.OpenRead(Path.Combine(_dirPath, fileId.ToString()))
                            : System.IO.File.OpenRead(Path.Combine(_dirPath, fileId + ".png")))
                    {
                        Console.WriteLine(offset);
                        stream.Seek(offset, SeekOrigin.Current);

                        content.Add(new StreamContent(stream), "File", "File");
                        content.Add(new StringContent(streamCode), "StreamCode");
                        content.Add(new StringContent(SharedArea.GlobalVariables.FILE_TRANSFER_USERNAME), "Username");
                        content.Add(new StringContent(SharedArea.GlobalVariables.FILE_TRANSFER_PASSWORD), "Password");

                        using (var message = await client.PostAsync(
                            SharedArea.GlobalVariables.SERVER_URL +
                            SharedArea.GlobalVariables.FILE_TRANSFER_TAKE_DOWNLOAD_STREAM_URL, content))
                        {
                            await message.Content.ReadAsStringAsync();
                        }
                    }
                }
            }
        }
        
        public async Task Consume(ConsumeContext<SearchUsersRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var users = (from u in dbContext.BaseUsers where u is User
                    where EF.Functions.Like(u.Title, "%" + packet.SearchQuery + "%")
                    select u).Select(bu => (User) bu).ToList();

                await context.RespondAsync(new SearchUsersResponse()
                {
                    Packet = new Packet {Status = "success", Users = users}
                });
            }
        }
        
        public async Task Consume(ConsumeContext<SearchComplexesRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var session = dbContext.Sessions.Find(context.Message.SessionId);
                
                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var user = (User) session.BaseUser;
                dbContext.Entry(user).Collection(u => u.Memberships).Query().Include(m => m.Complex).Load();
                var complexes = (from c in (from m in user.Memberships
                    where EF.Functions.Like(m.Complex.Title, "%" + packet.SearchQuery + "%")
                    select m) select c.Complex).ToList();

                await context.RespondAsync(new SearchComplexesResponse()
                {
                    Packet = new Packet {Status = "success", Complexes = complexes}
                });
            }
        }
        
        public async Task Consume(ConsumeContext<BotGetWorkershipsRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var session = dbContext.Sessions.Find(context.Message.SessionId);
                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var bot = (Bot) session.BaseUser;

                var workerships = dbContext.Workerships.Where(w => w.BotId == bot.BaseUserId)
                    .Include(w => w.Room).ThenInclude(r => r.Complex).ThenInclude(c => c.Members)
                    .ThenInclude(m => m.User).ToList();

                await context.RespondAsync(new BotGetWorkershipsResponse()
                {
                    Packet = new Packet() {Status = "success", Workerships = workerships}
                });
            }
        }
        
        public async Task Consume(ConsumeContext<GetBotStoreContentRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var botStoreHeader = dbContext.BotStoreHeader
                    .Include(bsh => bsh.Banners)
                    .ThenInclude(b => b.Bot)
                    .FirstOrDefault();
                var botStoreSection = new BotStoreSection();
                var botStoreBots = dbContext.Bots.Select(bot => new BotStoreBot()
                    {
                        Bot = bot,
                        BotStoreSection = botStoreSection
                    })
                    .ToList();
                botStoreSection.BotStoreBots = botStoreBots;

                await context.RespondAsync(new GetBotStoreContentResponse()
                {
                    Packet = new Packet
                    {
                        Status = "success",
                        BotStoreHeader = botStoreHeader,
                        BotStoreSections = new List<BotStoreSection>() { botStoreSection }
                    }
                });
            }
        }
        
        public async Task Consume(ConsumeContext<GetWorkershipsRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var session = dbContext.Sessions.Find(context.Message.SessionId);
                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var user = (User) session.BaseUser;
                dbContext.Entry(user).Collection(u => u.Memberships).Load();
                var membership = user.Memberships.Find(m => m.ComplexId == packet.Complex.ComplexId);
                if (membership == null)
                {
                    await context.RespondAsync(new GetWorkershipsResponse()
                    {
                        Packet = new Packet {Status = "error_1"}
                    });
                    return;
                }
                dbContext.Entry(membership).Reference(m => m.Complex).Load();
                var complex = membership.Complex;
                dbContext.Entry(complex).Collection(c => c.Rooms).Load();
                var room = complex.Rooms.Find(r => r.RoomId == packet.Room.RoomId);
                if (room == null)
                {
                    await context.RespondAsync(new GetWorkershipsResponse()
                    {
                        Packet = new Packet {Status = "error_2"}
                    });
                    return;
                }
                dbContext.Entry(room).Collection(r => r.Workers).Load();
                var workers = room.Workers.ToList();
                await context.RespondAsync(new GetWorkershipsResponse()
                {
                    Packet = new Packet {Status = "success", Workerships = workers}
                });
            }
        }
        
        public async Task Consume(ConsumeContext<AddBotToRoomRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var session = dbContext.Sessions.Find(context.Message.SessionId);

                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var user = (User) session.BaseUser;
                dbContext.Entry(user).Collection(u => u.Memberships).Load();
                var membership = user.Memberships.Find(m => m.ComplexId == packet.Complex.ComplexId);
                if (membership == null)
                {
                    await context.RespondAsync(new AddBotToRoomResponse()
                    {
                        Packet = new Packet {Status = "error_2"}
                    });
                    return;
                }
                dbContext.Entry(membership).Reference(m => m.MemberAccess).Load();
                if (!membership.MemberAccess.CanModifyWorkers)
                {
                    await context.RespondAsync(new AddBotToRoomResponse()
                    {
                        Packet = new Packet() {Status = "error_3"}
                    });
                    return;
                }
                
                dbContext.Entry(membership).Reference(m => m.Complex).Load();
                var complex = membership.Complex;
                dbContext.Entry(complex).Collection(c => c.Rooms).Load();
                var room = complex.Rooms.Find(r => r.RoomId == packet.Room.RoomId);
                if (room == null)
                {
                    await context.RespondAsync(new AddBotToRoomResponse()
                    {
                        Packet = new Packet {Status = "error_4"}
                    });
                    return;
                }
                dbContext.Entry(room).Collection(r => r.Workers).Load();
                var workership = room.Workers.Find(w => w.BotId == packet.Bot.BaseUserId);
                if (workership != null)
                {
                    await context.RespondAsync(new AddBotToRoomResponse()
                    {
                        Packet = new Packet {Status = "error_1"}
                    });
                    return;
                }
                var bot = dbContext.Bots.Find(packet.Bot.BaseUserId);
                if (bot == null)
                {
                    await context.RespondAsync(new AddBotToRoomResponse()
                    {
                        Packet = new Packet {Status = "error_0"}
                    });
                    return;
                }
                workership = new Workership()
                {
                    BotId = bot.BaseUserId,
                    Room = room,
                    PosX = packet.Workership.PosX,
                    PosY = packet.Workership.PosY,
                    Width = packet.Workership.Width,
                    Height = packet.Workership.Height
                };
                dbContext.AddRange(workership);
                dbContext.SaveChanges();
                
                SharedArea.Transport.NotifyService<WorkershipCreatedNotif>(
                    Program.Bus,
                    new Packet() {Workership = workership},
                    new string[]
                    {
                        
                    });
                
                Workership finalWorkership;
                using (var finalContext = new DatabaseContext())
                {
                    finalWorkership = finalContext.Workerships.Find(workership.WorkershipId);
                    finalContext.Entry(finalWorkership).Reference(w => w.Room).Query().Include(r => r.Complex)
                        .ThenInclude(c => c.Members).ThenInclude(m => m.User).Load();
                }
                
                dbContext.Entry(bot).Collection(b => b.Sessions).Load();
                var botSess = bot.Sessions.FirstOrDefault();
                var addition = new BotAdditionToRoomNotification()
                {
                    Workership = finalWorkership,
                    Bot = bot,
                    Session = botSess
                };
                if (botSess != null)
                    SharedArea.Transport.Push<BotAdditionToRoomPush>(
                        Program.Bus,
                        new BotAdditionToRoomPush()
                        {
                            Notif = addition,
                            SessionIds = new[] {botSess.SessionId}.ToList()
                        });

                Bot finalBot;
                using (var finalContext = new DatabaseContext())
                {
                    finalBot = (Bot) finalContext.BaseUsers.Find(bot.BaseUserId);
                }

                var addition2 = new BotAdditionToRoomNotification()
                {
                    Bot = finalBot,
                    Workership = finalWorkership
                };

                dbContext.Entry(complex).Collection(c => c.Members).Query().Include(m => m.User)
                    .ThenInclude(u => u.Sessions).Load();

                var sessionIds = (from m in complex.Members
                    where m.User.BaseUserId != user.BaseUserId
                    from s in m.User.Sessions
                    select s.SessionId).ToList();
                
                SharedArea.Transport.Push<BotAdditionToRoomPush>(
                    Program.Bus,
                    new BotAdditionToRoomPush()
                    {
                        Notif = addition2,
                        SessionIds = sessionIds
                    });
                
                await context.RespondAsync(new AddBotToRoomResponse()
                {
                    Packet = new Packet
                    {
                        Status = "success",
                        Workership = workership
                    }
                });
            }
        }

        public async Task Consume(ConsumeContext<UpdateWorkershipRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var session = dbContext.Sessions.Find(context.Message.SessionId);
                
                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var user = (User) session.BaseUser;
                dbContext.Entry(user).Collection(u => u.Memberships).Load();
                var membership = user.Memberships.Find(m => m.ComplexId == packet.Complex.ComplexId);
                if (membership == null)
                {
                    await context.RespondAsync(new UpdateWorkershipResponse()
                    {
                        Packet = new Packet {Status = "error_0"}
                    });
                    return;
                }
                dbContext.Entry(membership).Reference(m => m.Complex).Load();
                var complex = membership.Complex;
                dbContext.Entry(complex).Collection(c => c.Rooms).Load();
                var room = complex.Rooms.Find(r => r.RoomId == packet.Room.RoomId);
                if (room == null)
                {
                    await context.RespondAsync(new UpdateWorkershipResponse()
                    {
                        Packet = new Packet {Status = "error_2"}
                    });
                    return;
                }
                dbContext.Entry(room).Collection(r => r.Workers).Load();
                var workership = room.Workers.Find(w => w.BotId == packet.Bot.BaseUserId);
                if (workership == null)
                {
                    await context.RespondAsync(new UpdateWorkershipResponse()
                    {
                        Packet = new Packet {Status = "error_3"}
                    });
                    return;
                }
                workership.PosX = packet.Workership.PosX;
                workership.PosY = packet.Workership.PosY;
                workership.Width = packet.Workership.Width;
                workership.Height = packet.Workership.Height;
                dbContext.SaveChanges();
                
                SharedArea.Transport.NotifyService<WorkershipUpdatedNotif>(
                    Program.Bus,
                    new Packet() {Workership = workership},
                    new string[]
                    {
                        
                    });

                var notif = new BotPropertiesChangedNotification()
                {
                    Workership = workership
                };

                dbContext.Entry(complex).Collection(c => c.Members).Query().Include(m => m.User)
                    .ThenInclude(u => u.Sessions).Load();

                var sessionIds = (from m in complex.Members
                    where m.User.BaseUserId != user.BaseUserId
                    from s in m.User.Sessions
                    select s.SessionId).ToList();
                
                SharedArea.Transport.Push<BotPropertiesChangedPush>(
                    Program.Bus,
                    new BotPropertiesChangedPush()
                    {
                        Notif = notif,
                        SessionIds = sessionIds
                    });
                
                await context.RespondAsync(new UpdateWorkershipResponse()
                {
                    Packet = new Packet {Status = "success"}
                });
            }
        }

        public async Task Consume(ConsumeContext<RemoveBotFromRoomRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var session = dbContext.Sessions.Find(context.Message.SessionId);

                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var user = (User) session.BaseUser;
                dbContext.Entry(user).Collection(u => u.Memberships).Load();
                var membership = user.Memberships.Find(m => m.ComplexId == packet.Complex.ComplexId);
                if (membership == null)
                {
                    await context.RespondAsync(new RemoveBotFromRoomResponse()
                    {
                        Packet = new Packet {Status = "error_1"}
                    });
                    return;
                }
                dbContext.Entry(membership).Reference(m => m.Complex).Load();
                var complex = membership.Complex;
                dbContext.Entry(complex).Collection(c => c.Rooms).Load();
                var room = complex.Rooms.Find(r => r.RoomId == packet.Room.RoomId);
                if (room == null)
                {
                    await context.RespondAsync(new RemoveBotFromRoomResponse()
                    {
                        Packet = new Packet {Status = "error_3"}
                    });
                    return;
                }
                dbContext.Entry(room).Collection(r => r.Workers).Load();
                var workership = room.Workers.Find(w => w.BotId == packet.Bot.BaseUserId);
                if (workership == null)
                {
                    await context.RespondAsync(new RemoveBotFromRoomResponse()
                    {
                        Packet = new Packet {Status = "error_0"}
                    });
                    return;
                }
                dbContext.Entry(workership).Reference(w => w.Room).Query().Include(r => r.Complex).Load();
                room.Workers.Remove(workership);
                dbContext.Workerships.Remove(workership);
                dbContext.SaveChanges();
                
                SharedArea.Transport.NotifyService<WorkershipDeletedNotif>(
                    Program.Bus,
                    new Packet() {Workership = workership},
                    new string[]
                    {
                        
                    });
                
                var bot = dbContext.Bots.Find(workership.BotId);
                dbContext.Entry(bot).Collection(b => b.Sessions).Load();
                var botSess = bot.Sessions.FirstOrDefault();
                
                var removation = new BotRemovationFromRoomNotification()
                {
                    Workership = workership
                };
                
                if (botSess != null)
                    SharedArea.Transport.Push<BotRemovationFromRoomPush>(
                        Program.Bus,
                        new BotRemovationFromRoomPush()
                        {
                            Notif = removation,
                            SessionIds = new[] {botSess.SessionId}.ToList()
                        });

                dbContext.Entry(complex).Collection(c => c.Members).Query().Include(m => m.User)
                    .ThenInclude(u => u.Sessions).Load();
               
                var sessionIds = (from m in complex.Members
                    where m.User.BaseUserId != user.BaseUserId
                    from s in m.User.Sessions
                    select s.SessionId).ToList();
                
                SharedArea.Transport.Push<BotRemovationFromRoomPush>(
                    Program.Bus,
                    new BotRemovationFromRoomPush()
                    {
                        Notif = removation,
                        SessionIds = sessionIds
                    });

                await context.RespondAsync(new RemoveBotFromRoomResponse()
                {
                    Packet = new Packet {Status = "success"}
                });
            }
        }
        
        public async Task Consume(ConsumeContext<GetComplexWorkersRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var complex = dbContext.Complexes.Find(context.Message.Packet.Complex.ComplexId);

                dbContext.Entry(complex).Collection(c => c.Rooms).Query().Include(r => r.Workers).Load();

                var workers = new List<Workership>();
                foreach (var room in complex.Rooms)
                {
                    workers.AddRange(room.Workers.ToList());
                }

                await context.RespondAsync(new GetComplexWorkersResponse
                {
                    Packet = new Packet() {Workerships = workers}
                });
            }
        }
        
        public async Task Consume(ConsumeContext<GetBotsRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var session = dbContext.Sessions.Find(context.Message.SessionId);

                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var bots = dbContext.Bots.Include(b => b.BotSecret).ToList();
                var finalBots = new List<Bot>();
                var bannedAccessIds = new List<long>();
                foreach (var bot in bots)
                {
                    if (bot.BotSecret.CreatorId != session.BaseUserId)
                    {
                        bannedAccessIds.Add(bot.BaseUserId);
                    }
                    else
                    {
                        finalBots.Add(bot);
                    }
                }

                using (var nextContext = new DatabaseContext())
                {
                    foreach (var id in bannedAccessIds)
                    {
                        var finalBot = nextContext.Bots.Find(id);
                        finalBots.Add(finalBot);
                    }
                }

                await context.RespondAsync(new GetBotsResponse()
                {
                    Packet = new Packet {Status = "success", Bots = finalBots}
                });
            }
        }
        
        public async Task Consume(ConsumeContext<GetCreatedBotsRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var session = dbContext.Sessions.Find(context.Message.SessionId);

                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var user = (User) session.BaseUser;
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
        
        public async Task Consume(ConsumeContext<GetSubscribedBotsRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var session = dbContext.Sessions.Find(context.Message.SessionId);
                if (session == null)
                {
                    await context.RespondAsync(new GetSubscribedBotsResponse()
                    {
                        Packet = new Packet {Status = "error_0"}
                    });
                    return;
                }

                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var user = (User) session.BaseUser;
                var bots = new List<Bot>();
                dbContext.Entry(user).Collection(u => u.SubscribedBots).Load();
                var subscriptions = user.SubscribedBots.ToList();
                var noAccessBotIds = new List<long>();
                foreach (var botSubscription in user.SubscribedBots)
                {
                    dbContext.Entry(botSubscription).Reference(bc => bc.Bot).Load();
                    dbContext.Entry(botSubscription.Bot).Reference(b => b.BotSecret).Load();
                    if (botSubscription.Bot.BotSecret.CreatorId == user.BaseUserId)
                    {
                        bots.Add(botSubscription.Bot);
                    }
                    else
                    {
                        noAccessBotIds.Add(botSubscription.Bot.BaseUserId);
                    }
                }

                using (var nextContext = new DatabaseContext())
                {
                    foreach (var id in noAccessBotIds)
                    {
                        var bot = nextContext.Bots.Find(id);
                        bots.Add(bot);
                    }
                }

                await context.RespondAsync(new GetSubscribedBotsResponse()
                {
                    Packet = new Packet {Status = "success", Bots = bots, BotSubscriptions = subscriptions}
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
                var session = dbContext.Sessions.Find(context.Message.SessionId);

                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var user = (User) session.BaseUser;
                dbContext.Entry(user).Collection(u => u.CreatedBots).Load();
                var botCreation = user.CreatedBots.Find(bc => bc.BotId == packet.Bot.BaseUserId);
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
                bot.Avatar = packet.Bot.Avatar;
                bot.Description = packet.Bot.Description;
                bot.ViewURL = packet.Bot.ViewURL;
                dbContext.SaveChanges();

                await context.RespondAsync(new UpdateBotProfileResponse()
                {
                    Packet = new Packet {Status = "success"}
                });
            }
        }

        public async Task Consume(ConsumeContext<GetBotRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var session = dbContext.Sessions.Find(context.Message.SessionId);

                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var robot = (Bot) dbContext.BaseUsers.Find(packet.Bot.BaseUserId);
                if (robot == null)
                {
                    await context.RespondAsync(new GetBotResponse()
                    {
                        Packet = new Packet {Status = "error_080"}
                    });
                    return;
                }

                dbContext.Entry(robot).Reference(r => r.BotSecret).Load();
                if (robot.BotSecret.CreatorId == session.BaseUserId)
                {
                    await context.RespondAsync(new GetBotResponse()
                    {
                        Packet = new Packet {Status = "success", Bot = robot}
                    });
                    return;
                }

                using (var nextContext = new DatabaseContext())
                {
                    var nextBot = nextContext.Bots.Find(packet.Bot.BaseUserId);
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
                var session = dbContext.Sessions.Find(context.Message.SessionId);

                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var user = (User) session.BaseUser;
                var bot = new Bot();

                var token = "+" + Security.MakeKey64();

                var botSess = new Session()
                {
                    Token = token
                };
                
                var result = await SharedArea.Transport.RequestService<BotCreatedWithBackRequest, BotCreatedWithBackResponse>(
                    Program.Bus,
                    SharedArea.GlobalVariables.CITY_QUEUE_NAME,
                    new Packet() {Bot = bot, Session = botSess});

                bot.BaseUserId = result.Packet.Bot.BaseUserId;
                bot.Title = packet.Bot.Title;
                bot.Avatar = packet.Bot.Avatar > 0 ? packet.Bot.Avatar : 0;
                bot.Description = packet.Bot.Description;

                botSess.SessionId = result.Packet.Bot.Sessions[0].SessionId;
                botSess.BaseUser = bot;
                
                var botSecret = new BotSecret()
                {
                    Bot = bot,
                    Creator = user,
                    Token = token
                };
                bot.BotSecret = botSecret;

                var botCreation = new BotCreation()
                {
                    Bot = bot,
                    Creator = user
                };
                var subscription = new BotSubscription()
                {
                    Bot = bot,
                    Subscriber = user
                };
                dbContext.AddRange(bot, botSecret, botSess, botCreation, subscription);
                dbContext.SaveChanges();

                var versions = new List<Version>()
                {
                    new Version()
                    {
                        VersionId = "BaseUser_" + bot.BaseUserId + "_MessengerService",
                        Number = bot.Version
                    },
                    new Version()
                    {
                        VersionId = "Session_" + botSess.SessionId + "_MessengerService",
                        Number = bot.Version
                    }
                };
                
                SharedArea.Transport.NotifyPacketRouter<EntitiesVersionUpdatedNotif>(
                    Program.Bus,
                    new Packet()
                    {
                        Versions = versions
                    });

                await context.RespondAsync(new CreateBotResponse()
                {
                    Packet = new Packet
                    {
                        Status = "success",
                        Bot = bot,
                        BotCreation = botCreation,
                        BotSubscription = subscription,
                        Versions = versions
                    }
                });
            }
        }

        public async Task Consume(ConsumeContext<SubscribeBotRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var session = dbContext.Sessions.Find(context.Message.SessionId);

                var bot = dbContext.Bots.Find(packet.Bot.BaseUserId);
                if (bot == null)
                {
                    await context.RespondAsync(new SubscribeBotResponse()
                    {
                        Packet = new Packet {Status = "error_1"}
                    });
                    return;
                }

                dbContext.Entry(session).Reference(s => s.BaseUser).Load();
                var user = (User) session.BaseUser;
                dbContext.Entry(user).Collection(u => u.SubscribedBots).Load();
                if (user.SubscribedBots.Any(b => b.BotId == packet.Bot.BaseUserId))
                {
                    await context.RespondAsync(new SubscribeBotResponse()
                    {
                        Packet = new Packet {Status = "error_0"}
                    });
                    return;
                }

                var subscription = new BotSubscription()
                {
                    Bot = bot,
                    Subscriber = user
                };
                dbContext.AddRange(subscription);
                dbContext.SaveChanges();

                await context.RespondAsync(new SubscribeBotResponse()
                {
                    Packet = new Packet {Status = "success", BotSubscription = subscription}
                });
            }
        }
    }
}