using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

public class MomoExecuteResponseModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string Amount { get; set; }
    public string UserId { get; set; }
    public string ProjectFundId { get; set; }
    public string? FullName { get; set; }
    public string? Avatar { get; set; }

    public DateTime CreatedAt { get; set; }

}


