using Amazon.Extensions.NETCore.Setup;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using PayProcessor.Settings;
using PayProcessor.Services;
using PayProcessor.Repositories;
using PaymentGrpcContracts;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddGrpc();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var awsOption = builder.Configuration.GetAWSOptions();
builder.Services.AddDefaultAWSOptions(awsOption);
builder.Services.AddAWSService<IAmazonSQS>();
builder.Services.Configure<AwsSettings>(builder.Configuration.GetSection("AWS"));
// builder.Services.AddHostedService<SqsListenerService>();

builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDB"));
builder.Services.AddSingleton<IPaymentRepository, PaymentRepository>();

// http 2 grpc
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5232, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGrpcService<PaymentServiceImpl>();

app.Run();