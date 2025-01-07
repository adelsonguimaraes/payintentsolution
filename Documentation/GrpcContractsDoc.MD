# GrpcContracts
ClassLib que serve para centralizar funcionalidades que podem ser compartilhadas com todos os serviços.

Nesse caso foi utilizada para manter os contratos Grpc.

# Proto
Abaixo o arquivo que configura o contrato de payment, coloquei o namespace como "PaymentGrpcContracts", esse nome vai ser usado como referência em outros serviços.

```proto
syntax = "proto3";

option csharp_namespace = "PaymentGrpcContracts";
```
A service se chama "PaymentService"

```proto
service PaymentService {
  rpc GetPaymentDetails(PaymentRequest) returns (PaymentResponse);
}
```

Temos dois métodos o "PaymentRequest" que envia um Uuid e o "PaymentResponse" que recebe o objeto inteiro.

```proto
message PaymentRequest {
  string uuid = 1;
}

message PaymentResponse {
  string uuid = 1;
  double amount = 2;
  string currency = 3;
  string status = 4;
}
```