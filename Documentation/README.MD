# Documentação 
Neste documento, explico sobre as decisões que tomei na construção de cada recurso, estrutura e arquivo do projeto.

## Documentação por App
1. [GrpcContracts](GrpcContractsDoc.MD)
2. [PayIntentAPI](PayIntentAPI.MD)
3. [PayProcessor](PayProcessor.MD)
4. [PayTests](PayTests.MD)

## Estrutura

1. **Solution**: Nas pesquisas que realizei, vi que é muito comum em .NET, quando um sistema tem vários serviços, ser criada uma "Solution" (sln), que fica na pasta raiz e cada APP que vai sendo criado vai sendo adicionado à solução. Entendi como uma boa prática, então utilizei nesse projeto.

2. **GrpcContracts**: Aqui apliquei uma "Class Library", que pelo que estudei é um conceito de app shared, onde podem ser criados vários recursos que vão ser utilizados em vários serviços. Nesse caso, utilizei para fazer o contrato gRPC dentro dele e depois apenas fiz a referência dentro dos outros serviços.

3. **PayIntentAPI e PayProcessor**: Esses são os serviços principais.

4. **PayTests**: Este é o aplicativo de testes.

## Estrutura do serviço
1. **GrpcContracts**: 

    1. __Proto__: Pasta onde ficam os arquivos de contrato. Aqui só temos "payment.proto".

2. **PayIntentAPI**:
    
    1. **Controller**: Criei uma pasta controller para organizar os controladores de API.
    2. **DTOs**: Criei para organizar os DTOs.
    3. **Models**: Criei para organizar os models. Só temos o modelo de Payment.
    4. **Settings**: Essa pasta eu criei para organizar classes de configurações. Aqui estamos usando com a Amazon.

3. **PayProcessor**: 
    1. **Controllers**: Organiza controles de API.
    2. **DTOs**: Organiza DTOs.
    3. **Models**: Organiza models.
    4. **Repositories**: Organiza repositories e interfaces que se comunicam com o MongoDB.
    5. **Services**: Organiza as services. Temos o PayProcessorService e um Listener.
    6. **Settings**: Organiza classes de configurações AWS e MongoDB.

4. **PayTests**:
    1. **Controllers**: Organiza classes de controle de testes.
