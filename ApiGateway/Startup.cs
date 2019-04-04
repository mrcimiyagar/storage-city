using System;
using ApiGateway.Consumers;
using Bugsnag.AspNet.Core;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SharedArea.Utils;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace ApiGateway
{
    public class Startup
    {
        public static IBusControl Bus { get; set; }
        
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddJsonOptions(options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore)
                .AddJsonOptions(options => options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore);
            services.AddBugsnag(configuration => {
                configuration.ApiKey = "a8a5a1181aab99f935abded5836a0dfe";
            });
            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = 4294967296;
            });
            services.AddSignalR(hubOptions =>
            {
                hubOptions.EnableDetailedErrors = true;
            }).AddJsonProtocol((options) =>
            {
                options.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.PayloadSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app, 
            IHostingEnvironment env, 
            ILoggerFactory loggerFactory, 
            IServiceProvider serviceProvider,
            Bugsnag.IClient bugsnag)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            
            app.UseHttpsRedirection();
            app.UseMvc();
            
            Logger.Setup();
            
            Startup.Bus = MassTransit.Bus.Factory.CreateUsingRabbitMq(sbc =>
            {
                var host = sbc.Host(new Uri(SharedArea.GlobalVariables.RABBITMQ_SERVER_PATH), h =>
                {
                    h.Username(SharedArea.GlobalVariables.RABBITMQ_USERNAME);
                    h.Password(SharedArea.GlobalVariables.RABBITMQ_PASSWORD);
                });
                sbc.UseJsonSerializer();
                sbc.ConfigureJsonSerializer(options =>
                {
                    options.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    options.NullValueHandling = NullValueHandling.Ignore;
                    options.Converters.Add(new SessionConverter());
                    return options;
                });
                sbc.ConfigureJsonDeserializer(options =>
                {
                    options.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    options.NullValueHandling = NullValueHandling.Ignore;
                    options.Converters.Add(new SessionConverter());
                    return options;
                });
                sbc.ReceiveEndpoint(host, SharedArea.GlobalVariables.API_GATEWAY_QUEUE_NAME, ep =>
                {
                    EndpointConfigurator.ConfigEndpoint(ep);
                    ep.Consumer<ApiGatewayConsumer>(EndpointConfigurator.ConfigConsumer);
                });
            });

            Startup.Bus.ConnectSendObserver(new SendObserver(bugsnag));
            Startup.Bus.ConnectConsumeObserver(new ConsumeObserver(bugsnag));
            Startup.Bus.ConnectReceiveObserver(new ReceiveObserver(bugsnag));
            
            Startup.Bus.Start();
            
            Console.WriteLine("Bus loaded");
        }
    }
}