using System;
using System.Collections.Generic;
using System.Data.Entity;
using DAL.EntityModel;
using Faker;

namespace DAL
{
    public class CreateTestDatabaseIfNotExists : CreateDatabaseIfNotExists<TestDbContext>
    {
        protected override void Seed(TestDbContext context)
        {
            var results = new List<Member>();
            for (var i = 0; i < 100; i++)
            {
                var member = new Member
                {
                    Id = Guid.NewGuid(),
                    Birthday = Date.Birthday(1, 120),
                    Name = Name.FullName(),
                    Age = Number.RandomNumber(1, 120),
                    UserId = User.Username()
                };

                var detailCount = Number.RandomNumber(1, 20);
                if (member.MemberLogs == null)
                {
                    member.MemberLogs = new List<MemberLog>();
                }
                for (var j = 0; j < detailCount; j++)
                {
                    member.MemberLogs.Add(new MemberLog
                    {
                        Id = Guid.NewGuid(),
                        Member = member,
                        UserId = member.UserId,
                        Birthday = Date.Birthday(1, 120),
                        Name = Name.FullName(),
                        Age = Number.RandomNumber(1, 120),
                    });
                }

                results.Add(member);
            }

            context.Members.AddRange(results);
            context.SaveChanges();
        }
    }
}