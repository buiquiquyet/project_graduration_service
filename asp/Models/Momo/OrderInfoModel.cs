using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

public class OrderInfoModel
{
    public string FullName { get; set; }
    public string? OrderId { get; set; } 
    public string OrderInfo { get; set; } // id của bản từ thiện
    public string Amount { get; set; }
    public string UserId { get; set; } // id của người dùng


}

