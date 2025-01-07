using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using PayProcessor.Models;
using PayProcessor.Settings;
using PayProcessor.Repositories;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using PaymentGrpcContracts;

namespace PayProcessor.Services
{
    public class PaymentServiceImpl : PaymentService.PaymentServiceBase
    {
        private readonly Random _random;
        private readonly IAmazonSQS _sqsClient;
        private readonly AwsSettings _awsSettings;
        private readonly ILogger<PaymentServiceImpl> _logger;
        private readonly IPaymentRepository _paymentRepository;

        public PaymentServiceImpl
        (
            IAmazonSQS sqsClient,
            ILogger<PaymentServiceImpl> logger,
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

        public override async Task<PaymentResponse> GetPaymentDetails(PaymentRequest request, ServerCallContext context)
        {
            try
            {
                var sqsRequest = new ReceiveMessageRequest
                {
                    QueueUrl = _awsSettings.SqsUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 5
                };

                var response = await _sqsClient.ReceiveMessageAsync(sqsRequest, context.CancellationToken);

                if (response.Messages.Count == 0)
                {
                    throw new Exception("No messages received from SQS.");
                }

                foreach (var message in response.Messages)
                {
                    // Simulating random processing error
                    if (_random.NextDouble() < 0.3)
                    {
                        throw new Exception("Simulated random processing failure.");
                    }

                    var payment = JsonSerializer.Deserialize<Payment>(message.Body);

                    if (payment?.Uuid == Guid.Parse(request.Uuid))
                    {
                        _logger.LogInformation("Received message: {Message}", message.Body);

                        // Simulating random payment status
                        payment.Status = (_random.NextDouble() < 0.3) ? "Failed" : "Processed";

                        await _paymentRepository.CreatePaymentAsync(payment);

                        _logger.LogInformation("Payment processed in MongoDB: {PaymentUuid}", payment.Uuid);

                        var deleteRequest = new DeleteMessageRequest
                        {
                            QueueUrl = _awsSettings.SqsUrl,
                            ReceiptHandle = message.ReceiptHandle
                        };

                        await _sqsClient.DeleteMessageAsync(deleteRequest, context.CancellationToken);
                        _logger.LogInformation("Message deleted from SQS: {Message}", message.Body);

                        return new PaymentResponse
                        {
                            Uuid = payment.Uuid.ToString(),
                            Amount = payment.Amount,
                            Currency = payment.Currency,
                            Status = payment.Status
                        };
                    }
                    else
                    {
                        throw new Exception("Message UUID does not match the requested UUID.");
                    }
                }

                throw new Exception("No matching messages found for the provided UUID.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment request: {PaymentUuid}", request.Uuid);

                return new PaymentResponse
                {
                    Uuid = request.Uuid,
                    Amount = 0,
                    Currency = "USD",
                    Status = "Failed"
                };
            }
        }
    }
}
