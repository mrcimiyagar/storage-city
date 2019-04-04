using System.Linq;

namespace SharedArea
{
    public class GlobalVariables
    {
        public static readonly ushort PIPE_COUNT = 8;
        public static readonly string RABBITMQ_SERVER_URL_EXTENSIONS = "?prefetch=" + PIPE_COUNT;
        
        //public static readonly string RABBITMQ_SERVER_URL = "rabbitmq://localhost";
        public static readonly string RABBITMQ_SERVER_URL = "rabbitmq://134.209.50.254";
        
        public static readonly string RABBITMQ_SERVER_PATH = RABBITMQ_SERVER_URL + RABBITMQ_SERVER_URL_EXTENSIONS;
        public static readonly string RABBITMQ_USERNAME = "aseman";
        public static readonly string RABBITMQ_PASSWORD = "s635Dghs6rY4kjs6rt75tk84t7K5154l651jz32135yrRj5";
        public static readonly string FILE_TRANSFER_USERNAME = "guest", FILE_TRANSFER_PASSWORD = "guest";

        //public static readonly string SERVER_URL = "http://localhost:8080/";
        public static readonly string SERVER_URL = "http://134.209.50.254:8080/";
        public static readonly string SERVER2_URL = "";

        public static readonly string FILE_TRANSFER_GET_UPLOAD_STREAM_URL = "api/file/get_file_upload_stream";
        public static readonly string FILE_TRANSFER_NOTIFY_GET_UPLOAD_STREAM_FINISHED_URL = "api/file/notify_file_transffered";
        public static readonly string FILE_TRANSFER_TAKE_DOWNLOAD_STREAM_URL = "api/file/take_file_download_stream";

        public static readonly string PACKET_ROUTER_QUEUE_NAME = "PacketRouterQueue";
        public static readonly string API_GATEWAY_QUEUE_NAME = "ApiGatewayQueue";
        public static readonly string CITY_QUEUE_NAME = "CityServiceQueue";
        public static readonly string MESSENGER_QUEUE_NAME = "MessengerServiceQueue";
        public static readonly string STORAGE_QUEUE_NAME = "StorageModuleQueue";
        
        public static readonly int RABBITMQ_REQUEST_TIMEOUT = 60;

        public static string[] AllQueuesExcept(string[] queueNames)
        {
            var queues = new string[]
            {
                CITY_QUEUE_NAME, MESSENGER_QUEUE_NAME
            }.ToList();
            foreach (var queueName in queueNames)
            {
                queues.Remove(queueName);
            }
            return queues.ToArray();
        }
    }
}