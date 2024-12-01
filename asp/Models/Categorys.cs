using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

public class Categorys
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string? name { get; set; } // Tên dự án
  
    [BsonElement("createdAt")]
    public DateTime createdAt { get; set; } = DateTime.Now; // Tạo lúc

    [BsonElement("updatedAt")]
    public DateTime updatedAt { get; set; } = DateTime.Now; // Cập nhật lúc
}
