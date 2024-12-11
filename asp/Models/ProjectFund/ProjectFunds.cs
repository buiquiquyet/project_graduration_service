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
    public long? numberOfDonate { get; set; } // số người donate

    public int? evaluate { get; set; } // lượt like
    public string? idProjectFundProcessing { get; set; } // id của bản ghi sứ giả được duyệt thành công
    public List<string>? likedByUsers { get; set; } = new List<string>(); // Danh sách userId thả tim
    public string? userId { get; set; } // id của sứ giả người dùng
    public string? userName { get; set; } // tên của sứ giả người dùng

    //public string? images { get; set; } // Ảnh của dự án
    //public IFormFile? imagesIFormFile { get; set; } // Ảnh của dự án
    public List<string>? images { get; set; } // Cập nhật để lưu danh sách tên ảnh
    public List<string>? video { get; set; } // Cập nhật để lưu danh sách video
    [NotMapped]
    public List<IFormFile>? imagesIFormFile { get; set; } // Danh sách các file ảnh gửi lên từ client
    public List<IFormFile>? videoIFormFile { get; set; } // Danh sách các file video gửi lên từ client
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
