# PayIntentAPI
Serviço responsável por receber a intenção de pagamento, gerar um Uuid (Guid) e registrar no SQS da AWS.

# Model
Usei um model para ajudar na serialização dos valores e nas validações de dados.

No construtor da class inicio o Uuid com 
um Guid, e o status com "pending".
```cs
public PaymentModel()
{
    Uuid = Guid.NewGuid();
    Status = "Pending";
}
```

Utilizo `DataAnnotations` para fazer as validações dos campos, assim a validação já fica diretamente no modelo.
```cs
[Required(ErrorMessage = "Amount is required")]
[Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public double Amount { get; set; }

[Required(ErrorMessage = "Currency is required")]
public string? Currency { get; set; }
```

# Program.cs
Precisamos fazer algumas configurações aqui vamos ver.

Importando `Amazon SQS` e também os `Settings` onde temos nossas classes de configuração.
```cs
using Amazon.SQS;
using PaymentIntentAPI.Settings;
```

Registrando os seviços de `Grpc`, `Controllers`, `Explorer` e `Swagger`.
```cs
builder.Services.AddGrpc();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
```

Configurando a `Amazon SQS`.
```cs
var awsOptions = builder.Configuration.GetAWSOptions();
builder.Services.AddDefaultAWSOptions(awsOptions);
builder.Services.AddAWSService<IAmazonSQS>();
```

Aqui estamos pegando de nosso arquivo "appsettings" as configurações do SQS e registrando em AwsSettings.
```cs
builder.Services.Configure<AwsSettings>(builder.Configuration.GetSection("AWS"));
```

## AppSettings
Dentro de appsettings adicionamos as configurações da nossa AWS.

Esses dados são consumidos no Program.cs para a variável AwsSettings.
```json
"AWS": {
    "Region": "us-east-2",
    "SqsUrl": "https://Queue"
}
```

# Contoller
Aqui na controller que acontece toda a nossa lógica, recebimento da request, envio para o SQS e comunicação com o serviço de consumer por gRPC.

Configuramos aqui nossa APIController, adicionamos a rota.
```cs
[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    // criamos aqui nossa variavel de sqs e de configuração aws
    private readonly IAmazonSQS _sqsClient;
    private readonly AwsSettings _awsSettings;

    public PaymentController(IAmazonSQS sqsClient, IOptions<AwsSettings> awsSettings)
    {
        // no construtor inicializamos
        _sqsClient = sqsClient;
        // pegando da nossa variável que awsSettings que foi inicializada em Program.cs
        _awsSettings = awsSettings.Value;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePaymentIntent([FromBody] PaymentModel payment)
    {
        // como estamos tipando nosso FromBody com PaymentModel, temos acesso a validação do model que usa as configurações DataAnnotations do modelo que fizemos
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // criamos um DTO
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

            // registramos na fila do SQS
            var sendMessageResponse = await _sqsClient.SendMessageAsync(sendMessageRequest);

            if (sendMessageResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                // se der ok iniciamos uma comunicação GRPC
                // init grpc client
                using var channel = GrpcChannel.ForAddress("http://localhost:5232");
                var client = new PaymentService.PaymentServiceClient(channel);

                var grcpRequest = new PaymentRequest
                {
                    Uuid = payment.Uuid.ToString()
                };

                # enviamos o Uuid para o nosso Consumer
                var grpcResponse = await client.GetPaymentDetailsAsync(grcpRequest);
                // end grpc client

                # retornamos a resposta para o cliente http
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
```