namespace Lab.NoMagicNumeric.DAL
{
    public class TransferStatus
    {
        public static Define Transform { get; set; } = new Define {Code = "Y", Description = "已轉換"};

        public static Define NoTransform { get; set; } = new Define {Code = "N", Description = "未轉換"};
    }
}