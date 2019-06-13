namespace Lab.NoMagicNumeric.DAL
{
    public class ApproveStatus
    {
        public static Status Approve { get; set; } = new Status {Code = "99", Description = "已核准"};

        public static Status Open { get; set; } = new Status {Code = "10", Description = "已開立"};
    }
}