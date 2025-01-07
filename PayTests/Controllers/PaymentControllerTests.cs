using Moq;
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
using PayIntentAPI.Settings;

namespace PayTests
{
    public class PaymentControllerTests
    {
        private readonly Mock<IAmazonSQS> _mockSqsClient;
        private readonly IOptions<AwsSettings> _awsSettings;

        public PaymentControllerTests()
        {
            _mockSqsClient = new Mock<IAmazonSQS>();
            _awsSettings = Options.Create(new AwsSettings { SqsUrl = "https://sqs.fakeurl.com" });
        }

        [Fact]
        public async Task CreatePaymentIntent_ReturnsOk()
        {
            // Arrange
            var payment = new PaymentModel
            {
                Uuid = Guid.NewGuid(),
                Amount = 100,
                Currency = "USD",
                Status = "Pending"
            };

            _mockSqsClient.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), default))
                          .ReturnsAsync(new SendMessageResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

            var controller = new PaymentController(_mockSqsClient.Object, _awsSettings);

            // Act
            var result = await controller.CreatePaymentIntent(payment);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<dynamic>(okResult.Value);
            Assert.Equal(payment.Uuid, returnValue.uuid);
        }
    }

}