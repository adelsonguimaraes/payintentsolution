# Payment Intent Solution

Projeto de micro serviços feito em ASP.NET Core simulando um Gateway de pagamento.

## Documentação
Acesse à [Documentação](/Documentation/README.MD) clicando aqui.

## Serviços:

### PayIntentAPI (Producer):
1. Recebe intenção de pagamento por API Post.
2. Gera Uuid para a intenção.
3. Envia para uma lista SQS da AWS.
4. Comunica via gRPC o serviço PayProcessor (Consumer) sobre a intenção e aguarda a resposta.
5. Responde a requisição com a resposta do Consumer.

### PayProcessor (Consumer):
1. Recebe a mensagem gRPC do Producer.
2. Consulta o SQS da AWS.
3. Verifica se o Uuid informado está na fila.
4. Simula falha de processamento.
5. Simula sucesso, alterando Status para "Processed" ou "Failed".
6. Salva no MongoDb.
7. Responde a mensagem gRPC do Producer.
8. Possui endpoint GET para consultar por Uuid o status do Pagamento.

## Como executar

1. Inserir chaves dos serviços AWS e MongoDB
    Atualizar o arquivo `PayIntentAPI/appsettings.json` a chave de acesso do SQS Aws em "SqsUrl".

    ```json
    "AWS": {
        "Region": "us-east-2",
        "SqsUrl": "https://sqs.."
    },
    ```

    Atualizar também o arquivo `PayProcessor/appsettings.json`, a chave SQS e MongoDB, nos campos "SqsUrl" e em "ConnectionString".

    ```json
    "AWS": {
        "Region": "us-east-2",
        "SqsUrl": "https://sqs.."
    },
    "MongoDB": {
        "ConnectionString": "mongodb+srv://..",
        "DatabaseName": "PaymentDB",
        "CollectionName": "Payments"
    },
    ```

2. Restore as dependências dos projetos:

    ```bash
    dotnet restore PayItentAPI/

    dotnet restore PayProcessor/
    ```

3. Build os projetos:

    ```bash
    dotnet build PayItentAPI/

    dotnet build PayIntentSolution/
    ```

4. Inicie os serviços:

    ```bash
    dotnet run --project PayIntentSolution/

    dotnet run --project PayIntentSolution/
    ```

## Acesso
1. Criar intenção de pagamento

    ```bash
    [POST] http://localhost:5041/api/payment
    ```

    Payload para enviar no body:
    ```json
    {"amount": 10, "currency": "USD"}
    ```

2. Consulta de Status por Uuid
    ```bash
    [GET] http://localhost:5233/api/PayProcessor/{uuid}
    ```

3. Swagger
    ```bash
    [PayIntentAPI] http://localhost:5041/swagger
    ```

    ```bash
    [PayProcessor] http://localhost:5233/swagger
    ```


