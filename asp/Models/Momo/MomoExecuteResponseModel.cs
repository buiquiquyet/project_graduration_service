using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

public class MomoExecuteResponseModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string Amount { get; set; }

    public string? UserId { get; set; }
    public string ProjectFundId { get; set; }
    public string? FullName { get; set; }
    public string? Avatar { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? ProjectName { get; set; } = ""; // tên dự án
    public string? ProjectNameFund { get; set; } = ""; // tên quỹ
    public string? ProjectNameCategory { get; set; } = ""; // tổng danh mục
    public string? ProjectCurrentAmount { get; set; } = ""; // số tiền hiện tại
    public string? ProjectTargetAmount { get; set; } = ""; // số tiền cần đạt
    public string? ProjectStartDate { get; set; } = ""; // ngày bắt đầu
    public string? ProjectEndDate { get; set; } = ""; // ngày bắt đầu

}


