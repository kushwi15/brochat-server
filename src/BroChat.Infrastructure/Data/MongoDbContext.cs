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
        // User Indexes
        try 
        { 
            // Try to create the non-unique index. If it fails because of a name conflict or existing index, we just log it.
            Users.Indexes.CreateOne(new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = false, Name = "Email_NonUnique" }));
        } 
        catch (Exception ex) { Console.WriteLine($"User index warning: {ex.Message}"); }

        // Conversation Indexes
        try 
        {
            Conversations.Indexes.CreateOne(new CreateIndexModel<Conversation>(
                Builders<Conversation>.IndexKeys.Ascending(c => c.UserId)));
        }
        catch (Exception ex) { Console.WriteLine($"Conversation index warning: {ex.Message}"); }

        // Message Indexes
        try
        {
            Messages.Indexes.CreateOne(new CreateIndexModel<Message>(
                Builders<Message>.IndexKeys.Ascending(m => m.ConversationId)));
        }
        catch (Exception ex) { Console.WriteLine($"Message index warning: {ex.Message}"); }

        // GuestUsage Indexes
        try
        {
            GuestUsages.Indexes.CreateOne(new CreateIndexModel<GuestUsage>(
                Builders<GuestUsage>.IndexKeys.Ascending(gu => gu.GuestId),
                new CreateIndexOptions { Unique = true }));
        }
        catch (Exception ex) { Console.WriteLine($"GuestUsage index warning: {ex.Message}"); }
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
    public IMongoCollection<Conversation> Conversations => _database.GetCollection<Conversation>("Conversations");
    public IMongoCollection<Message> Messages => _database.GetCollection<Message>("Messages");
    public IMongoCollection<RefreshToken> RefreshTokens => _database.GetCollection<RefreshToken>("RefreshTokens");
    public IMongoCollection<GuestUsage> GuestUsages => _database.GetCollection<GuestUsage>("GuestUsages");
}
