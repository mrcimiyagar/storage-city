namespace SharedArea.Commands.File
{
    public class DownloadFileRequest : Request
    {
        public long FileId { get; set; }
        public long Offset { get; set; }
        public string StreamCode { get; set; }
    }
}