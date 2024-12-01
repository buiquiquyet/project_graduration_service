using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

public class CharityFundsv2
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string? name { get; set; } // Tên dự án

  
}
