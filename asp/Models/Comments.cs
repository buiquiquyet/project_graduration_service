using asp.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

public class Comments
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string? content { get; set; } // Tên dự án
    public string? userId { get; set; } // id nguoi dung
    public string? projectFundId { get; set; } // id nguoi dung

    [BsonElement("createdAt")]
    public DateTime createdAt { get; set; } = DateTime.Now; // Tạo lúc

    [BsonElement("updatedAt")]
    public DateTime updatedAt { get; set; } = DateTime.Now; // Cập nhật lúc

    // Thêm thuộc tính cho thông tin người dùng
    [BsonIgnore]
    public string? userName { get; set; }

    [BsonIgnore]
    public string? userAvatar { get; set; }
}
