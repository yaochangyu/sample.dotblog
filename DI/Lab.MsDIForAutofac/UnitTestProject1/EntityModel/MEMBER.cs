using LinqToDB.Mapping;

namespace UnitTestProject1.EntityModel
{
    /// <summary>
    /// </summary>
    [Table("MEMBER")]
    public class Member
    {
        /// <summary>
        ///     ID
        /// </summary>
        [PrimaryKey]
        [Column]
        public int ID { get; set; }

        /// <summary>
        ///     NAME
        /// </summary>
        [Column]
        public string NAME { get; set; }

        /// <summary>
        ///     AGE
        /// </summary>
        [Column]
        public int AGE { get; set; }

        /// <summary>
        ///     REMARK
        /// </summary>
        [Column]
        [Nullable]
        public string REMARK { get; set; }

        [Association(ThisKey = "ID", OtherKey = "MEMBER_ID", CanBeNull = true)]
        public Identity IDENTITY { get; set; }
    }
}