namespace asp.Models.GetApiCommon
{
    public class FundsStatisticsDTO
    {
        public long? ProjectFundsCount { get; set; } // SỐ DỰ ÁN
        public long? CharityFundsCount { get; set; } // SỐ TỔ CHỨC
        public long? ProjectFundsWithUserIdCount { get; set; } // SỐ SỨ GIẢ
        public decimal? TotalMomoAmount { get; set; } // TỔNG SỐ TIỀN
    }
}
