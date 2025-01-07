using Moq;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;

namespace PayTests.Controllers
{
    public class PayControllerTests
    {
        private readonly Mock<IAmazonSQS> _mockSqsClient;
        private Mock<IOptions<AwsSettings>> _mockSettings;

        public PayControllerTests()
        {
            _mockSqsClient = new Mock<IAmazonSQS>();
            _mockAwsSettings = new Mock<IOptions<AwsSettings>>();

            _mockAwsSettings.Setup(x => x.Value).Returns(new AwsSettings
            {
                SqsUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/MyQueue"
            });
        }

        [Fact]
        public async Task Post_ReturnsOk_WhenModelIsValid()
        {
            var payment = new PaymentModel
            {
                Amount = 10.0,
                Currency = "USD",
                Status = "Pending"
            };

            var sendMessageRequest = new SendMessageRequest
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK
            };

            _mockSqsClient
                .Setup(sqs => sqs.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>(), default))
                .ReturnsAsync(sendMessageRequest);

            var controller = new PaymentController(_mockSqsClient.Object, _mockAwsSettings.Object);

            var result = await controller.Post(payment);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var retunedPayment = Assert.IsType<PaymentModel>(okResult.Value);

            Assert.Equal(payment.Amount, retunedPayment.Amount);
            Assert.Equal(payment.Currency, retunedPayment.Currency);
            Assert.Equal(payment.Status, retunedPayment.Status);
        }

        [Fact]
        public async Task Post_ReturnBadRequest_WhenModelIsInvalid()
        {
            var payment = new PaymentModel
            {
                Amount = -10.0,
                Currency = "USD",
                Status = "Pending"
            };

            var controller = new PaymentController(_mockSqsClient.Object, _mockAwsSettings.Object);
            controller.ModelState.AddModelError("Amount", "Amount must be greater than 0");

            var result = await controller.Post(payment);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}