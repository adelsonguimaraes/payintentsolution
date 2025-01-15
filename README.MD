# Payment Intent Solution

Projeto de micro serviços feito em ASP.NET Core simulando um Gateway de pagamento.

## Documentação
Acesse à [Documentação](/Documentation/README.MD) clicando aqui.

## Recursos
Recursos utilizados para realização desse desafio.

1. Tecnologias e versões
    * Dotnet               8.0.404
    * Google.Protobuf      3.19.1
    * Grpc                 2.39.1
    * Grpc.Tools           2.68.1

2. IDE
    * Visual Studio Code

3. Browser
    * Chrome

4. Versionador
    * Git CLI
    * Serviço GitHub

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

    __Obs__: Importante configurar no servidor do MongoDB o `DataBaseName` e o `CollectionName` como está abaixo, ou atualizar para como está em seu MongoDB.
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
    dotnet restore PayIntentSolution.sln
    ```

3. Build os projetos:

    ```bash
    dotnet build PayIntentSolution.sln
    ```

4. Inicie os serviços:

    __Obs__: Cada serviço deve ser rodado em um terminal distinto.
    ```bash
    dotnet run --project ./PayIntentAPI/

    dotnet run --project ./PayProcessor/
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
    __Obs__: Acompanhe os logs no temrinal, o serviço PayIntentAPI, irá autenticar com a AWS, depois enviará a msg gRPC para o serviço PayProcessor que irá autenticar com a AWS, simular falhas de leitura (caso falhe irá consultar novamente o SQS respeitando o tempo de visualização configurado) e sucesso, registrar no MongoDB e por fim responder a mensagem gRPC.

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


