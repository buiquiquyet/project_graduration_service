using asp.Constants.User;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace asp.Models.User
{
    // thông tin người dùng hiển thị ở detail 1 quỹ
    public class UserDetailFund
    {
        
        public string? Avatar { get; set; } = ""; // ảnh người dùng
        public string? FullName { get; set; } = ""; // tên người dùng
        public decimal? TotalDonate { get; set; } = 0; // tổng donate




    }
}
