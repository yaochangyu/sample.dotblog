namespace Lab.XmlSerializer.ConsoleApp
{
    // 測試用的資料類別
    [Serializable]
    public class TestPerson
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string Email { get; set; }
        public List<string> Hobbies { get; set; } = new List<string>();
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

    [Serializable]
    public class TestOrder
    {
        public string OrderId { get; set; }
        public decimal Amount { get; set; }
        public List<TestOrderItem> Items { get; set; } = new List<TestOrderItem>();
        public DateTime OrderDate { get; set; } = DateTime.Now;
    }

    [Serializable]
    public class TestOrderItem
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
