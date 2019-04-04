using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Models
{
    public class NotifierStreamResult : FileStreamResult
    {
        public NotifierStreamResult(Stream stream) : base(stream, "application/octet-stream") { } 
        public NotifierStreamResult(Stream stream, Action onComplete) : base(stream
            , "application/octet-stream") 
        { 
            this.OnComplete = onComplete; 
        }

        public Action OnComplete { get; set; }

        public override void ExecuteResult(ActionContext context)
        {
            base.ExecuteResult(context);
            
            this.OnComplete();
        }
    }
}