namespace asp.Models.ProjectFundProcessing
{
    public class UpdateApprovalStatusDTO
    {
        public List<string> Ids { get; set; } = new List<string>();
        public string? isApproved { get; set; }
    }
}
