using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestProject2.Repository
{
    class SqlEmployeeText
    {
        public static readonly string AllEmployee = @"
SELECT
	[p].[Id],
	[p].[Name],
	[p].[Age],
	[p].[SequenceId]
FROM
	[dbo].[Employee] [p]
WHERE
	[p].[SequenceId] > 0
ORDER BY
	[p].[SequenceId]
";

        public static readonly string EmployeeCount = "SELECT COUNT(1) FROM Employee";
    }
}
