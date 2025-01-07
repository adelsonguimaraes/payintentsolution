# PayTests
App para fazer testes nos serviços.

# Observação
Infelizmente não consegui aprender e implementar os testes dentro do tempo permitido.

Cheguei a criar uma controller para testar a Controller de PayIntentAPI, mas não tive sucesso.

Segue abaixo o código que tentei implementar com ajuda do Copilot, porém não tive sucesso ele não reconhece os namespaces de PayIntentAPI, mesmo depois de criar uma referência.
```cs
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
```

Vou estudar mais sobre as formas de testar a aplicação.
