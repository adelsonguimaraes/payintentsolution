using Amazon.Extensions.NETCore.Setup;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using PayProcessor.Settings;
using PayProcessor.Services;
using PayProcessor.Repositories;
using PaymentGrpcContracts;
using Microsoft.AspNetCore.Server.Kestrel.Core;


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

// background listener
// builder.Services.AddHostedService<SqsListenerService>();

builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDB"));
builder.Services.AddSingleton<IPaymentRepository, PaymentRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.MapGrpcService<PaymentServiceImpl>();

app.Run();