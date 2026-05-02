using BroChat.Domain.Entities;
using BroChat.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace BroChat.Infrastructure.Data;

public class MongoDbContext
{
    private readonly IMongoClient _client;
    private readonly IMongoDatabase _sharedDatabase;
    private readonly ITenantService _tenantService;

    static MongoDbContext()
    {
        // Configure MongoDB to store Guids as standard UUIDs
        // Try registering, ignore if already registered
        try
        {
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        }
        catch (BsonSerializationException)
        {
            // Already registered
        }
    }

    public MongoDbContext(IConfiguration configuration, ITenantService tenantService)
    {
        _tenantService = tenantService;
        var connectionString = configuration["MongoDb:ConnectionString"] ?? throw new ArgumentNullException("MongoDb:ConnectionString");
        var sharedDatabaseName = configuration["MongoDb:DatabaseName"] ?? "brochat";
        
        _client = new MongoClient(connectionString);
        _sharedDatabase = _client.GetDatabase(sharedDatabaseName);

        CreateSharedIndexes();
        
        // If we have a tenant, ensure their indexes are created too
        if (_tenantService.GetTenantId() != null)
        {
            CreateTenantIndexes();
        }
    }

    private void CreateSharedIndexes()
    {
        // User Indexes
        try 
        { 
            Users.Indexes.CreateOne(new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = false, Name = "Email_NonUnique" }));
        } 
        catch (Exception ex) { Console.WriteLine($"User index warning: {ex.Message}"); }

        // GuestUsage Indexes
        try
        {
            GuestUsages.Indexes.CreateOne(new CreateIndexModel<GuestUsage>(
                Builders<GuestUsage>.IndexKeys.Ascending(gu => gu.GuestId),
                new CreateIndexOptions { Unique = true }));
        }
        catch (Exception ex) { Console.WriteLine($"GuestUsage index warning: {ex.Message}"); }
    }

    private void CreateTenantIndexes()
    {
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
    }

    private IMongoDatabase GetTenantDatabase()
    {
        var dbName = _tenantService.GetDatabaseName();
        return _client.GetDatabase(dbName);
    }

    public IMongoCollection<User> Users => _sharedDatabase.GetCollection<User>("Users");
    public IMongoCollection<Conversation> Conversations => GetTenantDatabase().GetCollection<Conversation>("Conversations");
    public IMongoCollection<Message> Messages => GetTenantDatabase().GetCollection<Message>("Messages");
    public IMongoCollection<RefreshToken> RefreshTokens => _sharedDatabase.GetCollection<RefreshToken>("RefreshTokens");
    public IMongoCollection<GuestUsage> GuestUsages => _sharedDatabase.GetCollection<GuestUsage>("GuestUsages");
}

