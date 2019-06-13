namespace Lab.NoMagicNumeric.DAL
{
    public class ApproveStatus
    {
        public static Define Approve { get; set; } = new Define {Code = "99", Description = "已核准"};

        public static Define Open { get; set; } = new Define {Code = "10", Description = "開立"};
    }
}