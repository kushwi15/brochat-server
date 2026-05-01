using BroChat.Domain.Entities;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace BroChat.Infrastructure.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    static MongoDbContext()
    {
        // Configure MongoDB to store Guids as standard UUIDs
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
    }

    public MongoDbContext(IConfiguration configuration)
    {
        var connectionString = configuration["MongoDb:ConnectionString"] ?? throw new ArgumentNullException("MongoDb:ConnectionString");
        var databaseName = configuration["MongoDb:DatabaseName"] ?? "brochat";
        
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);

        CreateIndexes();
    }

    private void CreateIndexes()
    {
        // User Indexes - Dropping old unique index if it exists to allow duplicates
        try { Users.Indexes.DropOne("Email_1"); } catch { }
        Users.Indexes.CreateOne(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions { Unique = true }));

        // Conversation Indexes
        Conversations.Indexes.CreateOne(new CreateIndexModel<Conversation>(
            Builders<Conversation>.IndexKeys.Ascending(c => c.UserId)));

        // Message Indexes
        Messages.Indexes.CreateOne(new CreateIndexModel<Message>(
            Builders<Message>.IndexKeys.Ascending(m => m.ConversationId)));
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
    public IMongoCollection<Conversation> Conversations => _database.GetCollection<Conversation>("Conversations");
    public IMongoCollection<Message> Messages => _database.GetCollection<Message>("Messages");
    public IMongoCollection<RefreshToken> RefreshTokens => _database.GetCollection<RefreshToken>("RefreshTokens");
}
