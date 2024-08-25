using Consul;
using mshop.gateway.api;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.ServiceDiscovery;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging(b => b.AddConsole());
builder.Services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
{
    consulConfig.Address = new Uri("http://consul:8500");
}));

//builder.Services.AddScoped<IDestinationResolver, ConsulDestinationResolver>();

builder.Services
    .AddScoped<IForwarderHttpClientFactory, MshopForwarderHttpClientFactory>()
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddCors(options =>
    options.AddPolicy(name: "CORS",
            policy =>
            {
                policy.WithOrigins("*")
                 .AllowAnyHeader()
                 .AllowAnyMethod()
                 .AllowAnyOrigin();
            }));

var app = builder.Build();
app.UseCors("CORS");


// Configure the HTTP request pipeline.
    app.UseSwagger();
    app.UseSwaggerUI();

//app.UseHttpsRedirection();
app.MapReverseProxy();

app.Run();
