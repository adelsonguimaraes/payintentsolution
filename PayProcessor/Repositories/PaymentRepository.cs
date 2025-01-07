using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using PayProcessor.Models;
using PayProcessor.Settings;

namespace PayProcessor.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly IMongoCollection<Payment> _paymentCollection;

        public PaymentRepository(IOptions<MongoDbSettings> mongoDbSettings)
        {
            BsonSerializer.RegisterSerializer(new GuidSerializer(MongoDB.Bson.GuidRepresentation.CSharpLegacy));

            var mongoClient = new MongoClient(mongoDbSettings.Value.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(mongoDbSettings.Value.DatabaseName);
            _paymentCollection = mongoDatabase.GetCollection<Payment>(mongoDbSettings.Value.CollectionName);
        }

        public async Task<Payment> GetPaymentByIdAsync(Guid uuid)
            => await _paymentCollection.Find(x => x.Uuid == uuid).FirstOrDefaultAsync();

        public Task CreatePaymentAsync(Payment payment)
            => _paymentCollection.InsertOneAsync(payment);
    }
}