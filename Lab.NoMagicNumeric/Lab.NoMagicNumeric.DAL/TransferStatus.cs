namespace Lab.NoMagicNumeric.DAL
{
    public class TransferStatus
    {
        public static Status Transform { get; set; } = new Status {Code = "Y", Description = "已轉換"};

        public static Status NoTransform { get; set; } = new Status {Code = "N", Description = "未轉換"};
    }
}