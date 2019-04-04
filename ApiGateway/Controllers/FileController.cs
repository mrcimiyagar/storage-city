using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ApiGateway.DbContexts;
using ApiGateway.Models.Forms;
using ApiGateway.Utils;
using MassTransit;
using Microsoft.AspNetCore.Http.Features;
using SharedArea.Middles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using SharedArea.Commands.File;
using SharedArea.Commands.Internal.Requests;
using SharedArea.Commands.Internal.Responses;
using SharedArea.Entities;
using SharedArea.Utils;
using File = SharedArea.Entities.File;

namespace ApiGateway.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : Controller
    {
        private static readonly FormOptions DefaultFormOptions = new FormOptions();

        [Route("~/api/file/write_to_file")]
        [RequestSizeLimit(bytes: 4294967296)]
        [HttpPost]
        [DisableFormValueModelBinding]
        public async Task<ActionResult<Packet>> WriteToFile()
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                return new Packet {Status = "error_0"};
            }

            using (var dbContext = new DatabaseContext())
            {
                var app = Security.AuthenticateApp(dbContext, Request.Headers[AuthExtracter.AppKeyHeader]);
                if (app == null) return new Packet() {Status = "error_100"};
                
                var session = Security.Authenticate<UserSession>(dbContext, Request.Headers[AuthExtracter.AK]);
                if (session == null) return new Packet() {Status = "error_1"};

                var formParts = new Dictionary<string, string>();

                var boundary = MultipartRequestHelper.GetBoundary(
                    MediaTypeHeaderValue.Parse(Request.ContentType),
                    DefaultFormOptions.MultipartBoundaryLengthLimit);
                var reader = new MultipartReader(boundary, HttpContext.Request.Body);

                var section = await reader.ReadNextSectionAsync();
                while (section != null)
                {
                    var hasContentDispositionHeader =
                        ContentDispositionHeaderValue.TryParse(section.ContentDisposition,
                            out var contentDisposition);

                    if (hasContentDispositionHeader)
                    {
                        if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                        {
                            var guid = Guid.NewGuid().ToString();

                            StreamRepo.FileStreams.Add(guid, section.Body);

                            var address = new Uri(SharedArea.GlobalVariables.RABBITMQ_SERVER_URL + "/" +
                                                  SharedArea.GlobalVariables.MESSENGER_QUEUE_NAME +
                                                  SharedArea.GlobalVariables.RABBITMQ_SERVER_URL_EXTENSIONS);

                            var requestTimeout = TimeSpan.FromDays(35);
                            IRequestClient<WriteToFileRequest, WriteToFileResponse> client =
                                new MessageRequestClient<WriteToFileRequest, WriteToFileResponse>(
                                    Startup.Bus, address, requestTimeout);

                            var result = await SharedArea.Transport.RequestService<WriteToFileRequest, WriteToFileResponse>(
                                Startup.Bus,
                                SharedArea.GlobalVariables.MESSENGER_QUEUE_NAME,
                                session,
                                Request.Headers.ToDictionary(a => a.Key, a => a.Value.ToString()),
                                new Packet()
                                {
                                    File = new File() {FileId = Convert.ToInt64(formParts["FileId"])},
                                    StreamCode = guid
                                });

                            StreamRepo.FileStreams.Remove(guid);

                            return result.Packet;
                        }
                        else if (MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition))
                        {
                            var key = HeaderUtilities.RemoveQuotes(contentDisposition.Name);
                            var encoding = GetEncoding(section);
                            using (var streamReader = new StreamReader(
                                section.Body,
                                encoding,
                                detectEncodingFromByteOrderMarks: true,
                                bufferSize: 1024,
                                leaveOpen: true))
                            {
                                var value = await streamReader.ReadToEndAsync();

                                formParts[key.ToString()] = value;

                                if (formParts.Count > DefaultFormOptions.ValueCountLimit)
                                {
                                    throw new InvalidDataException(
                                        $"Form key count limit {DefaultFormOptions.ValueCountLimit} exceeded.");
                                }
                            }
                        }
                    }

                    section = await reader.ReadNextSectionAsync();
                }
            }

            return new Packet() {Status = "error_2"};
        }

        private static Encoding GetEncoding(MultipartSection section)
        {
            var hasMediaTypeHeader = MediaTypeHeaderValue.TryParse(section.ContentType, out var mediaType);
            if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding))
            {
                return Encoding.UTF8;
            }

            return mediaType.Encoding;
        }

        [Route("~/api/file/get_file_size")]
        [HttpPost]
        public async Task<ActionResult<Packet>> GetFileSize([FromBody] Packet packet)
        {
            using (var dbContext = new DatabaseContext())
            {
                var app = Security.AuthenticateApp(dbContext, Request.Headers[AuthExtracter.AppKeyHeader]);
                if (app == null) return new Packet() {Status = "error_100"};
                
                var session = Security.Authenticate<UserSession>(dbContext, Request.Headers[AuthExtracter.AK]);
                if (session == null) return new Packet() {Status = "error_0"};

                var result = await SharedArea.Transport.RequestService<GetFileSizeRequest, GetFileSizeResponse>(
                    Startup.Bus,
                    SharedArea.GlobalVariables.MESSENGER_QUEUE_NAME,
                    session,
                    Request.Headers.ToDictionary(a => a.Key, a => a.Value.ToString()),
                    packet);

                return result.Packet;
            }
        }

        [Route("~/api/file/get_file_upload_stream")]
        [HttpPost]
        public ActionResult GetFileUploadStream([FromBody] Packet packet)
        {
            if (packet.Username == SharedArea.GlobalVariables.FILE_TRANSFER_USERNAME
                && packet.Password == SharedArea.GlobalVariables.FILE_TRANSFER_PASSWORD)
            {
                return File(StreamRepo.FileStreams[packet.StreamCode], "application/octet-stream");
            }
            else
            {
                return NotFound();
            }
        }

        [Route("~/api/file/take_file_download_stream")]
        [RequestSizeLimit(bytes: 4294967296)]
        [HttpPost]
        public ActionResult TakeFileDownloadStream([FromForm] TakeFileDSF form)
        {
            if (form.Username != SharedArea.GlobalVariables.FILE_TRANSFER_USERNAME ||
                form.Password != SharedArea.GlobalVariables.FILE_TRANSFER_PASSWORD) return Forbid();
            
            var file = form.File;
            var streamCode = form.StreamCode;

            StreamRepo.FileStreams.Add(streamCode, file.OpenReadStream());

            var lockObj = StreamRepo.FileStreamLocks[streamCode];

            lock (lockObj)
            {
                Monitor.Pulse(lockObj);
            }

            lock (lockObj)
            {
                Monitor.Wait(lockObj);
            }

            return Ok();
        }

        [Route("~/api/file/download_file")]
        [HttpGet]
        public ActionResult DownloadFile(long fileId, long offset)
        {
            using (var context = new DatabaseContext())
            {
                var app = Security.AuthenticateApp(context, Request.Headers[AuthExtracter.AppKeyHeader]);
                if (app == null) return Json(new Packet() {Status = "error_100"});
                
                var session = Security.Authenticate<UserSession>(context, Request.Headers[AuthExtracter.AK]);
                if (session == null) return NotFound();

                var guid = Guid.NewGuid().ToString();

                var lockObj = new object();

                StreamRepo.FileStreamLocks.Add(guid, lockObj);

                lock (lockObj)
                {
                    var result = SharedArea.Transport.RequestService<DownloadFileRequest, DownloadFileResponse>(
                        Startup.Bus,
                        SharedArea.GlobalVariables.MESSENGER_QUEUE_NAME,
                        session,
                        Request.Headers.ToDictionary(a => a.Key, a => a.Value.ToString()),
                        new Packet()
                        {
                            StreamCode = guid,
                            Offset = offset,
                            File = new File() {FileId = fileId}
                        });
                    
                    Monitor.Wait(lockObj);
                }

                var stream = StreamRepo.FileStreams[guid];

                Response.OnCompleted(() =>
                {
                    StreamRepo.FileStreams.Remove(guid);
                    StreamRepo.FileStreamLocks.Remove(guid);

                    lock (lockObj)
                    {
                        Monitor.Pulse(lockObj);
                    }

                    return Task.CompletedTask;
                });

                Response.ContentLength = stream.Length;

                return File(stream, "application/octet-stream");
            }
        }

        [Route("~/api/file/bot_create_document_file")]
        [HttpPost]
        public async Task<ActionResult<Packet>> BotCreateDocumentFile([FromBody] Packet packet)
        {
            using (var dbContext = new DatabaseContext())
            {
                var app = Security.AuthenticateApp(dbContext, Request.Headers[AuthExtracter.AppKeyHeader]);
                if (app == null) return new Packet() {Status = "error_100"};
                
                var session = Security.Authenticate<BotSession>(dbContext, Request.Headers[AuthExtracter.AK]);
                if (session == null) return new Packet() {Status = "error_0"};

                var result = await SharedArea.Transport
                    .RequestService<CreateFileRequest, CreateFileResponse>(
                        Startup.Bus,
                        SharedArea.GlobalVariables.MESSENGER_QUEUE_NAME,
                        session,
                        Request.Headers.ToDictionary(a => a.Key, a => a.Value.ToString()),
                        packet);
                
                return result.Packet;
            }
        }
    }
}