using EmailWorker.Applications.Interfaces;
using EmailWorker.Infrastructures;
using EmailWorker.Infrastructures.Email;
using EmailWorker.Infrastructures.Messaging;
using EmailWorker.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);


builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMQ"));

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});


builder.Services.AddSingleton<IEmailService, SmtpEmailService>();


builder.Services.AddHostedService<SendOrderEmailWorker>();

var host = builder.Build();
host.Run();
