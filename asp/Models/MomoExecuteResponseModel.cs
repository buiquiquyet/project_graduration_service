using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

public class MomoExecuteResponseModel
{
    public string Amount { get; set; }
    public string UserId { get; set; }
    public string ProjectFundId { get; set; }

    public DateTime CreatedAt { get; set; }

}


