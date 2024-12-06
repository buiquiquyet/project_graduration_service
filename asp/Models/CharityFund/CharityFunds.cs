using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

public class CharityFunds
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string? name { get; set; } // Tên dự án

    public string? images { get; set; } // Ảnh của quỹ
    public IFormFile? imagesIFormFile { get; set; } // Ảnh của quỹ
    public string? description { get; set; } // Mô tả của quỹ
    public string? address { get; set; } // địa chỉ của quỹ
    public string? phone { get; set; } // phone của quỹ
    public string? email { get; set; } // email của quỹ


    [BsonElement("createdAt")]
    public DateTime createdAt { get; set; } = DateTime.Now; // Tạo lúc

    [BsonElement("updatedAt")]
    public DateTime updatedAt { get; set; } = DateTime.Now; // Cập nhật lúc
}
