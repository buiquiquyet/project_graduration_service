using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations.Schema;

public class ProjectFunds
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string? name { get; set; } // Tên dự án

    [BsonRepresentation(BsonType.ObjectId)]
    public string? idFund { get; set; } // id của quỹ
    public string? nameFund { get; set; } // Tên quỹ
    public string? imageFund { get; set; } // ảnh của quỹ
    public string? idCategory { get; set; } // id của danh mục
    public string? nameCategory{ get; set; } // Tên danh mục
    public string? percent { get; set; } // phần trăm đang đạt được


    //public string? images { get; set; } // Ảnh của dự án
    //public IFormFile? imagesIFormFile { get; set; } // Ảnh của dự án
    public List<string>? images { get; set; } // Cập nhật để lưu danh sách tên ảnh
                                             //public IFormFileCollection imagesIFormFile { get; set; } // Danh sách các tệp ảnh
    [NotMapped]
    public List<IFormFile>? imagesIFormFile { get; set; } // Danh sách các file ảnh gửi lên từ client
    public string? description { get; set; } // Mô tả của dự án
    public string? targetAmount { get; set; } // số tiền mục tiêu
    public string? currentAmount { get; set; } // số tiền hiện tại
    public string? startDate { get; set; } // ngày bắt đầu của dự án
    public string? endDate { get; set; } // ngày kết thúc của dự án


    [BsonElement("createdAt")]
    public DateTime createdAt { get; set; } = DateTime.Now; // Tạo lúc

    [BsonElement("updatedAt")]
    public DateTime updatedAt { get; set; } = DateTime.Now; // Cập nhật lúc
}
