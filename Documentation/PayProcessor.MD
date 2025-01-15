# PayProcessor
Esse é o serviço responsável por fazer o processamento da intenção de pagamento.

## Funcionalidades
1. Receber a mensagem via gRPC sobre a intenção de pagamento.
2. Consultar as intenções de pagamento na fila SQS AWS.
3. Processar simulando falha de leitura (no knowledge), de forma a processar novamente.
4. Simular falha de sucesso, resultando em dois possíveis status "Failed" e "Processed".
5. Salvar no MongoDB.
6. Remover da fila SQS.
7. Responder a mensagem gRPC.

## Observações
Fiz duas abordagens para este serviço, a primeira abordagem fiz um serviço Backgound que ficava ativamente consultando a fila SQS na AWS e quando encontrava intenções processava, este código está no diretório `Services/SqsListenerService.cs`.

Alterei essa abordagem para utilizar a mensagem via gRPC e também por ser um alto custo, mas ela funcionava bem o código pode ser consultado no arquivo citado.

Nesse ponto tive dúvidas em como aplicar o gRPC mas fiz da forma que apresento aqui, sendo um sinalizador entre o Processor e Consumer quando uma intenção de pagamento é inserida na fila.

Importante dizer que a primeira abordagem `Listener` não vai ser explicada nessa documentação, pois o que está ativa no projeto é a abordagem `gRPC`.

## Models
Esse modelo é mais simples que o de PayIntentAPI, não tem as validações.

A novidade aqui é que usamos a anotação `[BsonIgnoreExtraElement]` do MongoDB sinalizando que não aceitamos elementos extras, isso evita erros na hora da consulta de o Mongo solicitar o campo _id.
```cs
[BsonIgnoreExtraElements]
public class Payment{..}
```

## Program.cs
Algumas configurações que são interessantes de citar.

Aqui nessa linha comentada temos a chamada do Listener que faz o papel de consumir a fila SQS, se descomentar essa linha o listener irá consultar a fila de 10 em 10 segundos.
```cs
// background listener
// builder.Services.AddHostedService<SqsListenerService>();
```

Aqui assim como fizemos com a AWS, estamos incializando nosso "MongoDbSettings" com as configurações, seguindo a variável "MongoDB" presente no "appsettings".
```cs
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDB"));
builder.Services.AddSingleton<IPaymentRepository, PaymentRepository>();
```

## AppSettings
No AppSettings também fizemos algumas configurações.

Configuração SQS AWS
```json
{
  "AWS": {
    "Region": "us-east-2",
    "SqsUrl": "https://PaymentQueue.."
},
```

Configuração para o MongoDB, com um database chamado "PaymentDB" e a collection chamada "Payments".
```json
  "MongoDB": {
    "ConnectionString": "mongodb+srv://..",
    "DatabaseName": "PaymentDB",
    "CollectionName": "Payments"
  },
```

Configuração do Kestrel para que tenhamos disponibilidade de Http1 e Http2.
```json
  "Kestrel": {
    "Endpoints": {
      "Http1": {
        "Url": "http://localhost:5233",
        "Protocols": "Http1"
      },
      "Http2": {
        "Url": "http://localhost:5232",
        "Protocols": "Http2"
      }
    }
  }
}
```

## Settings
Esse diretório contém classes de configuração para AWS e MongoDB, foi criado por organização.

## Repositories
Nesse diretório temos o Repository e a Interface, reponsáveis por comunicação e processamento de dados com o MongoDB, seguindo o padrão repository.

### 1. IPaymentRepository
Nossa interface que contém dois métodos que utilizamos para manipular os dados com o MongoDB, GetPaymentByIdAsync e CreatePaymentAsync.

```cs
public interface IPaymentRepository
{
    Task<Payment> GetPaymentByIdAsync(Guid uuid);
    Task CreatePaymentAsync(Payment payment);

}
```

### 2. PaymentRepository
Nossa class Repository.


Aqui fazemos criamos nossa class implementando a Interface.
```cs
public class PaymentRepository : IPaymentRepository{..}
```

Criauma variável para receber a interface da collection do Mongo tipando pelo nosso Modelo "Payment"
```cs
private readonly IMongoCollection<Payment> _paymentCollection;
```

No construtor ingetamos IOptions tipando pela nossa class "MongoDbSettings".
```cs
public PaymentRepository(IOptions<MongoDbSettings> mongoDbSettings){..}
```

Aqui configuramos a forma como o Serializador do Guid deve ser comportar no MongoDB.
```cs
BsonSerializer.RegisterSerializer(new GuidSerializer(MongoDB.Bson.GuidRepresentation.Standard));
```
Configurações do Mongo usando a nossa instancia de "mongoDbSettings".
```cs
var mongoClient = new MongoClien(mongoDbSettings.Value.ConnectionString);
var mongoDatabase = mongoClient.GetDatabase(mongoDbSettings.Value.DatabaseName);
        _paymentCollection = mongoDatabase.GetCollection<Payment>(mongoDbSettings.Value.CollectionName);
```

Por fim nossos dois métodos que serão usados para ações no banco, `GetPaymentByIdAsync` que recebe um uuid e é responsável por buscar a Intenção de pagamento no MongoDB por uuid.

E `CratePaymentAsync` responsável por inserir nossa intenção de pagamento no MongoDB.
```cs
public async Task<Payment> GetPaymentByIdAsync(Guid uuid)
    => await _paymentCollection.Find(x => x.Uuid == uuid).FirstOrDefaultAsync();

public Task CreatePaymentAsync(Payment payment)
    => _paymentCollection.InsertOneAsync(payment);
```

## Services
Nosso diretório de services que são responsáveis por fazer as movimentações.

Como citado anteriormente aqui foram feitas duas abordagens uma com Background Service no arquivo `SqsListenerService` e outra ativada por gRPC no arquivo `PayProcessorServiceImpl` que é a utilizada no código na execução atualmente, para usar `SqsListenerService` é necessár descomentar a linha do `Program.cs` citada anteriormente.

## PayProcessorServiceImpl
Esse serviço espera uma mensagem gRPC enviada pelo Producer (PayIntentAPI), em seguida simula uma falha de leitura randômica, depois faz uma consulta na fila SQS AWS, faz uma nova simulação randômica para o sucesso, salva no MongoDB e responde a mensagem gRPC.

Nossa class injeta `PaymentServiceBase`, essa classe é gerada pelo nosso `proto` lá na nossa classlib `GrpcContracts`, aqui nesse serviço adicionamos a referência, isso pode ser verificado no arquivo `csproj`, por isso podemos utilizar aqui nosso `PaymentService`.
```cs
public class PaymentServiceImpl : PaymentService.PaymentServiceBase {..}
```

No nosso construtor injetamos e alimentamos nosso `sqs` da AWS, o `logger` pra registrar mensagens no console, nossa instance `awsSettings`, e um `random` que vamos usar pra simular as falhas.
```cs
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
```

Sobreescrevemos o método GetPaymentDetails que também foi configurado e gerado pelo nosso `proto` esse método serve para receber a comunicação gRPC.
```cs
public override async Task<PaymentResponse> GetPaymentDetails(PaymentRequest request, ServerCallContext context) {..}
```

Aqui fazemos nossa configuração e requisição para a fila SQS, verificamos se não retorna nenhum item na fila, caso positivo ele refaz a chamada da função.
```cs
var sqsRequest = new ReceiveMessageRequest
{
    QueueUrl = _awsSettings.SqsUrl,
    MaxNumberOfMessages = 10,
    WaitTimeSeconds = 5
};

var response = await _sqsClient.ReceiveMessageAsync(sqsRequest, context.CancellationToken);

 if (response.Messages.Count == 0)
{
    _logger.LogError("No messages received from SQS, resending request.");
    return await GetPaymentDetails(request, context); // Return immediately
}
```

Em seguida temos a nossa simulação de falha de leitura da mensagem, se for positivo chama novamente o métodos para uma nova consulta na fila.
```cs
if (_random.NextDouble() < 0.3)
{
    _logger.LogError("[simulation] Error processing message, resending request.");
    return await GetPaymentDetails(request, context); // Return immediately
}
```
Aqui temos um laço que percorre nossas mensagens que chegaram, fazemos a descerialização e verificamos se é a mensagem que enviamos no gRPC.
```cs
foreach (var message in response.Messages)
{

    var payment = JsonSerializer.Deserialize<Payment>(message.Body);

    if (payment?.Uuid == Guid.Parse(request.Uuid)) {..}
}
```

Agora temos a nossa simulação de sucesso, colocamos a mesma lógica da simulação anterior então a possibilidade de dar falha é 30%, essa informação alimenta o status do nosso payment, e chamamos o método Create do Repository para adicionar o dado ao MongoDB.
```cs
 _logger.LogInformation("Received message: {Message}", message.Body);

payment.Status = (_random.NextDouble() < 0.3) ? "Failed" : "Processed";
await _paymentRepository.CreatePaymentAsync(payment);
```

Agora enviamos o comando de remover a mensagem da fila SQS.
```cs
_logger.LogInformation("Payment processed in MongoDB: {PaymentUuid}", payment.Uuid);

var deleteRequest = new DeleteMessageRequest
{
    QueueUrl = _awsSettings.SqsUrl,
    ReceiptHandle = message.ReceiptHandle
};
```
Por fim, repondemos a mensagem gRPC com os dados do Payment.
```cs
return new PaymentResponse
{
    Uuid = payment.Uuid.ToString(),
    Amount = payment.Amount,
    Currency = payment.Currency,
    Status = payment.Status
};
```