namespace Lab.Context.Trace.WebAPI.Models;

internal class Member
{
    public string UserId { get; set; }

    public int Age { get; set; }

    public string Name { get; set; }

    public static IEnumerable<Member> GetFakeMembers()
    {
        return new List<Member>()
        {
            new()
            {
                UserId = "yao",
                Age = 19,
                Name = "小章"
            },
            new()
            {
                UserId = "yao1",
                Age = 21,
                Name = "小章1"
            },
 
        };
    }
}