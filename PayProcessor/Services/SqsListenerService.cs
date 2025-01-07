using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using PayProcessor.Models;
using PayProcessor.Settings;
using PayProcessor.Repositories;

namespace PayProcessor.Services
{
    public class SqsListenerService : BackgroundService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly ILogger<SqsListenerService> _logger;
        private readonly AwsSettings _awsSettings;
        private readonly Random _random;
        private readonly IPaymentRepository _paymentRepository;

        public SqsListenerService(
            IAmazonSQS sqsClient,
            ILogger<SqsListenerService> logger,
            IOptions<AwsSettings> awsSettings,
            IPaymentRepository paymentRepository
        )
        {
            _sqsClient = sqsClient;
            _logger = logger;
            _awsSettings = awsSettings.Value;
            _random = new Random();
            _paymentRepository = paymentRepository;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var request = new ReceiveMessageRequest
                {
                    QueueUrl = _awsSettings.SqsUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 5
                };

                var response = await _sqsClient.ReceiveMessageAsync(request, stoppingToken);

                foreach (var message in response.Messages)
                {
                    try
                    {
                        // sumulating random processing error
                        if (_random.NextDouble() < 0.3)
                            throw new Exception("Error processing message");

                        var payment = JsonSerializer.Deserialize<Payment>(message.Body);

                        _logger.LogInformation("Received message: {Message}", message.Body);

                        payment!.Status = (_random.NextDouble() < 0.3) ? payment.Status = "Failed" : "Processed";

                        await _paymentRepository.CreatePaymentAsync(payment!);

                        _logger.LogInformation("Payment processed In MongoDB: {PaymentUuid}", payment!.Uuid);

                        var deleteRequest = new DeleteMessageRequest
                        {
                            QueueUrl = _awsSettings.SqsUrl,
                            ReceiptHandle = message.ReceiptHandle
                        };

                        await _sqsClient.DeleteMessageAsync(deleteRequest, stoppingToken);
                        _logger.LogInformation("Message deleted SQS: {Message}", message.Body);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message: {Message}", message.Body);
                    }
                }

                await Task.Delay(10000, stoppingToken);
            }
        }
    }
}