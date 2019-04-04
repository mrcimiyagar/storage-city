using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SharedArea.Commands.Bot;
using SharedArea.Commands.File;
using SharedArea.Commands.Internal.Requests;
using SharedArea.Commands.Internal.Responses;
using SharedArea.Commands.User;
using SharedArea.Entities;
using SharedArea.Middles;
using File = System.IO.File;

namespace StorageService
{
    public class MessengerConsumer : IConsumer<GetFileSizeRequest>, IConsumer<WriteToFileRequest>, IConsumer<DownloadFileRequest>
        , IConsumer<CreateFileRequest>
    {
        private readonly string _dirPath;

        public MessengerConsumer()
        {
            _dirPath = Path.Combine(Path.GetFullPath(Directory.GetCurrentDirectory()), "Aseman");
            Directory.CreateDirectory(_dirPath);
        }
        
        public async Task Consume(ConsumeContext<CreateFileRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var packet = context.Message.Packet;
                var session = dbContext.Sessions.Find(context.Message.SessionId);
                
                SharedArea.Entities.File file;

                var storage = dbContext.Storages.Find(packet.Storage.StorageId);

                StorageAgent storageAgent = null;
                
                if (session is UserSession us)
                {
                    dbContext.Entry(us).Reference(s => s.User).Load();
                    var user = us.User;
                    dbContext.Entry(storage).Reference(s => s.CreatorEnt).Load();
                    if (storage.CreatorEnt is StorageAgentUser sau)
                    {
                        if (sau.UserId != user.UserId)
                        {
                            await context.RespondAsync(new CreateFileResponse()
                            {
                                Packet = new Packet() {Status = "error_2"}
                            });
                            return;
                        }
                        else
                        {
                            storageAgent = sau;
                        }
                    }
                }
                else if (session is BotSession bs)
                {
                    dbContext.Entry(bs).Reference(s => s.Bot).Load();
                    var bot = bs.Bot;
                    dbContext.Entry(storage).Reference(s => s.CreatorEnt).Load();
                    if (storage.CreatorEnt is StorageAgentBot sab)
                    {
                        if (sab.BotId != bot.BotId)
                        {
                            await context.RespondAsync(new CreateFileResponse()
                            {
                                Packet = new Packet() {Status = "error_2"}
                            });
                            return;
                        }
                        else
                        {
                            storageAgent = sab;
                        }
                    }
                }
                
                file = new SharedArea.Entities.File()
                {
                    IsPublic = packet.File.IsPublic,
                    Uploader = storageAgent,
                    FileName = packet.File.FileName
                };
                dbContext.Files.Add(file);

                dbContext.SaveChanges();

                var filePath = Path.Combine(_dirPath, file.FileId.ToString());
                File.Create(filePath).Close();

                await context.RespondAsync(new CreateFileResponse()
                {
                    Packet = new Packet {Status = "success", File = file}
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

                if (session is UserSession us)
                {
                    if (file.UploaderId != us.UserId)
                    {
                        await context.RespondAsync(new WriteToFileResponse()
                        {
                            Packet = new Packet() {Status = "error_2"}
                        });
                        return;
                    }
                }
                else if (session is BotSession bs)
                {
                    if (file.UploaderId != bs.BotId)
                    {
                        await context.RespondAsync(new WriteToFileResponse()
                        {
                            Packet = new Packet() {Status = "error_2"}
                        });
                        return;
                    }
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
                }

                await context.RespondAsync(new WriteToFileResponse()
                {
                    Packet = new Packet() {Status = "success"}
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

                    if (file.IsPublic)
                    {
                        await UploadFileToApiGateWay(streamCode, file.FileId, offset.Value);
                        await context.RespondAsync(new DownloadFileResponse()
                        {
                            Packet = new Packet {Status = "success"}
                        });
                        return;
                    }

                    await UploadFileToApiGateWay(streamCode, file.FileId, offset.Value);
                    await context.RespondAsync(new DownloadFileResponse()
                    {
                        Packet = new Packet {Status = "success"}
                    });
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
    }
}