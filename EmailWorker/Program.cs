using EmailWorker.Applications.Interfaces;
using EmailWorker.Infrastructures.Email;
using EmailWorker.Infrastructures.Messaging;
using EmailWorker.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Config
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMQ"));

// Services
builder.Services.AddSingleton<IEmailService, SmtpEmailService>();

// Worker
builder.Services.AddHostedService<SendOrderEmailWorker>();

//builder.Configuration
//    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
//    .AddEnvironmentVariables();

var host = builder.Build();
host.Run();
