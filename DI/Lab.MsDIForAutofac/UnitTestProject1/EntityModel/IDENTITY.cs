using LinqToDB.Mapping;

namespace UnitTestProject1.EntityModel
{
    /// <summary>
    /// </summary>
    [Table("IDENTITY")]
    public class Identity
    {
        /// <summary>
        ///     MEMBER_ID
        /// </summary>
        [Column]
        [PrimaryKey]
        public int MEMBER_ID { get; set; }

        /// <summary>
        ///     ACCOUNT
        /// </summary>
        [Column]
        public string ACCOUNT { get; set; }

        /// <summary>
        ///     PASSWORD
        /// </summary>
        [Column]
        public string PASSWORD { get; set; }

        /// <summary>
        ///     REMARK
        /// </summary>
        [Column]
        [Nullable]
        public string REMARK { get; set; }

        [Association(ThisKey = "MEMBER_ID", OtherKey = "ID", CanBeNull = false)]
        public Member MEMBER { get; set; }
    }
}