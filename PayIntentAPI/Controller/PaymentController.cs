

using Microsoft.AspNetCore.Mvc;
using PayIntentAPI.Models;
using PaymentIntentAPI.Settings;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using PaymentIntentAPI.DTOs;
using Grpc.Net.Client;
using PaymentGrpcContracts;

namespace PaymentIntentAPI.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly AwsSettings _awsSettings;

        public PaymentController(IAmazonSQS sqsClient, IOptions<AwsSettings> awsSettings)
        {
            _sqsClient = sqsClient;
            _awsSettings = awsSettings.Value;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] PaymentModel payment)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var paymentDto = new PaymentDto
                {
                    Uuid = payment.Uuid,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    Status = payment.Status
                };

                var messageBody = JsonSerializer.Serialize(paymentDto);
                var sendMessageRequest = new SendMessageRequest
                {
                    QueueUrl = _awsSettings.SqsUrl,
                    MessageBody = messageBody
                };

                var sendMessageResponse = await _sqsClient.SendMessageAsync(sendMessageRequest);

                if (sendMessageResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    using var channel = GrpcChannel.ForAddress("http://localhost:5232");
                    var client = new PaymentService.PaymentServiceClient(channel);

                    var grcpRequest = new PaymentRequest
                    {
                        Uuid = payment.Uuid.ToString()
                    };

                    var grpcResponse = await client.GetPaymentDetailsAsync(grcpRequest);

                    Console.WriteLine($"Grpc response: {JsonSerializer.Serialize(grpcResponse)}");

                    return Ok(new { uuid = payment.Uuid, status = grpcResponse.Status });
                }
                else
                {
                    return StatusCode((int)sendMessageResponse.HttpStatusCode, "Failed to send message to SQS");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error {ex.Message}");
            }

        }
    }

}