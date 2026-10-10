using Lottery.Messaging;
using Lottery.Messaging.Email;
using Lottery.NotificationWorker;

var builder = Host.CreateApplicationBuilder(args);

// Runs as a Windows Service when installed with `sc create`; a no-op on Linux/containers
builder.Services.AddWindowsService(options => options.ServiceName = "Lottery Notification Worker");

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddSingleton<WinnerNotificationHandler>();
builder.Services.AddHostedService<WinnerNotificationConsumer>();

await builder.Build().RunAsync();
