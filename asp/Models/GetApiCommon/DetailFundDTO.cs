using asp.Models.User;

namespace asp.Models.GetApiCommon
{
    public class DetailFundDTO
    {
        public long ProjectFundsCount { get; set; } // số lượng dự án của quỹ
        public long ProjectFundsWithUserIdCount { get; set; } // số lượng sứ giả của quỹ
        public decimal TotalMomoAmount { get; set; } // số tiền donate của quỹ
        public long TotalSupportersCount { get; set; } // số người donate
        public List<UserDetailFund> Users { get; set; } // list user của quỹ
    }
}
