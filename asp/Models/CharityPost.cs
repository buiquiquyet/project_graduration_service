using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

public class CharityPost
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? fundId { get; set; } // Id của quỹ từ thiện

    public string? name { get; set; } // Tên dự án

    public string? images { get; set; } // Ảnh dự án

    public string? description { get; set; } // Mô tả dự án

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal? targetAmount { get; set; } // Số tiền cần quyên góp

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal? currentAmount { get; set; } // Số tiền đang có quyên góp

    public DateTime? startDate { get; set; } // Ngày bắt đầu chiến dịch

    public DateTime? endDate { get; set; } // Ngày kết thúc chiến dịch

    [BsonElement("createdAt")]
    public DateTime createdAt { get; set; } = DateTime.Now; // Tạo lúc

    [BsonElement("updatedAt")]
    public DateTime updatedAt { get; set; } = DateTime.Now; // Cập nhật lúc
}
