using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpringRabbit.NET;
using SpringRabbit.NET.Demo;

var builder = Host.CreateApplicationBuilder(args);

// Configure RabbitMQ connection
builder.Services.AddSpringRabbit(options =>
{
    // Option 1: Use connection string
    // options.ConnectionString = "amqp://guest:guest@localhost:5672/";
    
    // Option 2: Use individual settings
    options.HostName = "localhost";
    options.Port = 5672;
    options.UserName = "guest";
    options.Password = "guest";
});

// Register RabbitTemplate for sending messages
builder.Services.AddSingleton<RabbitTemplate>();

// Register your consumer services
builder.Services.AddScoped<DemoConsumer>();

// Register message publisher (optional - for demo purposes)
builder.Services.AddHostedService<MessagePublisher>();

// Discover and register all RabbitListener methods
builder.Services.AddRabbitListeners(typeof(Program).Assembly);

var host = builder.Build();

Console.WriteLine("🚀 SpringRabbit.NET Demo Application");
Console.WriteLine("📥 Listening for messages on queues:");
Console.WriteLine("   - demo.queue (concurrency: 3-10)");
Console.WriteLine("   - priority.queue (concurrency: 2-5, max priority: 10)");
Console.WriteLine();
Console.WriteLine("Press Ctrl+C to stop...");

await host.RunAsync();
