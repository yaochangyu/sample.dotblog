using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestProject2.Repository
{
    class SqlIdentityText
    {
        public static readonly string InnerJoinEmployee = @"
SELECT
	[a_Employee].[Id],
	[a_Employee].[Name],
	[a_Employee].[Age],
	[a_Employee].[SequenceId],
	[p].[Account],
	[p].[Password]
FROM
	[dbo].[Identity] [p]
		INNER JOIN [dbo].[Employee] [a_Employee] ON [p].[Employee_Id] = [a_Employee].[Id]
ORDER BY
	[a_Employee].[SequenceId]
";

        public static readonly string Count = @"
SELECT
	Count(*)
FROM
	[dbo].[Identity] [p]
";
    }
}
