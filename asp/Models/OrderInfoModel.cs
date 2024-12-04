using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

public class OrderInfoModel
{
    public string FullName { get; set; }
    public string OrderId { get; set; }
    public string OrderInfo { get; set; }
    public string Amount { get; set; }
    
}

