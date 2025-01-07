using Moq;
using PayIntentAPI.Controllers;
using PayIntentAPI.Models;
using PayIntentAPI.Settings;
using PayIntentAPI.DTOs;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Grpc.Net.Client;
using PaymentGrpcContracts;
using FluentAssertions;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace PayIntentAPI.Tests
{
    public class PaymentControllerTests
    {
        private readonly Mock<IAmazonSQS> _mockSqsClient;
        private readonly Mock<GrpcChannel> _mockGrpcChannel;
        private readonly Mock<PaymentService.PaymentServiceClient> _mockGrpcClient;
        private readonly Mock<IOptions<AwsSettings>> _mockAwsSettings;
        private readonly PaymentController _controller;

        public PaymentControllerTests()
        {
            _mockSqsClient = new Mock<IAmazonSQS>();
            _mockAwsSettings = new Mock<IOptions<AwsSettings>>();
            _mockAwsSettings.Setup(x => x.Value).Returns(new AwsSettings { SqsUrl = "https://example.com/sqs" });

            _mockGrpcChannel = new Mock<GrpcChannel>();
            _mockGrpcClient = new Mock<PaymentService.PaymentServiceClient>(_mockGrpcChannel.Object);

            _controller = new PaymentController(_mockSqsClient.Object, _mockAwsSettings.Object);
        }

        [Fact]
        public async Task CreatePaymentIntent_ShouldReturnOk_WhenMessageSentToSQSAndGrpcCallIsSuccessful()
        {
            // Arrange
            var payment = new PaymentModel
            {
                Uuid = Guid.NewGuid(),
                Amount = 100,
                Currency = "USD",
                Status = "Pending"
            };

            var sqsResponse = new SendMessageResponse
            {
                HttpStatusCode = HttpStatusCode.OK
            };

            _mockSqsClient.Setup(s => s.SendMessageAsync(It.IsAny<SendMessageRequest>(), default))
                .ReturnsAsync(sqsResponse);

            var grpcResponse = new PaymentDetails
            {
                Uuid = payment.Uuid.ToString(),
                Amount = payment.Amount.ToString(),
                Status = "Processed"
            };

            _mockGrpcClient.Setup(c => c.GetPaymentDetailsAsync(It.IsAny<PaymentRequest>(), default))
                .ReturnsAsync(grpcResponse);

            // Act
            var result = await _controller.CreatePaymentIntent(payment);

            // Assert
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);

            var responseValue = okResult.Value as JsonElement?;
            responseValue.HasValue.Should().BeTrue();
            responseValue.Value.GetProperty("uuid").GetString().Should().Be(payment.Uuid.ToString());
        }

        [Fact]
        public async Task CreatePaymentIntent_ShouldReturnBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            var payment = new PaymentModel(); // Invalid model, missing required properties
            _controller.ModelState.AddModelError("Amount", "Amount is required");

            // Act
            var result = await _controller.CreatePaymentIntent(payment);

            // Assert
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task CreatePaymentIntent_ShouldReturnInternalServerError_WhenExceptionOccurs()
        {
            // Arrange
            var payment = new PaymentModel
            {
                Uuid = Guid.NewGuid(),
                Amount = 100,
                Currency = "USD",
                Status = "Pending"
            };

            _mockSqsClient.Setup(s => s.SendMessageAsync(It.IsAny<SendMessageRequest>(), default))
                .ThrowsAsync(new Exception("SQS service error"));

            // Act
            var result = await _controller.CreatePaymentIntent(payment);

            // Assert
            var internalServerErrorResult = result as ObjectResult;
            internalServerErrorResult.Should().NotBeNull();
            internalServerErrorResult.StatusCode.Should().Be(500);
            internalServerErrorResult.Value.ToString().Should().Contain("SQS service error");
        }

        [Fact]
        public async Task CreatePaymentIntent_ShouldReturnStatusCode_WhenSQSMessageFails()
        {
            // Arrange
            var payment = new PaymentModel
            {
                Uuid = Guid.NewGuid(),
                Amount = 100,
                Currency = "USD",
                Status = "Pending"
            };

            var sqsResponse = new SendMessageResponse
            {
                HttpStatusCode = HttpStatusCode.BadRequest // Simulating failure to send message to SQS
            };

            _mockSqsClient.Setup(s => s.SendMessageAsync(It.IsAny<SendMessageRequest>(), default))
                .ReturnsAsync(sqsResponse);

            // Act
            var result = await _controller.CreatePaymentIntent(payment);

            // Assert
            var statusCodeResult = result as StatusCodeResult;
            statusCodeResult.Should().NotBeNull();
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        }
    }
}
