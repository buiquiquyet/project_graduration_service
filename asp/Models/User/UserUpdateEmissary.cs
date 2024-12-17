namespace asp.Models.User
{
    public class UserUpdateEmissary
    {
        public List<string> userIds { get; set; }  // Đảm bảo đây là List<string>
        public string newApprovalStatus { get; set; }  // Đảm bảo đây là string
    }
}
