using System;
using System.Data.Entity;
using Lab.Db.TestCase.Infrastructure;

namespace Lab.Db.TestCase.DAL
{
    public class MemberRepository
    {
        private string    _connectionString;
        private Guid?     _id;
        private DateTime? _now;

        public DateTime? Now
        {
            get
            {
                if (this._now.HasValue == false)
                {
                    return DateTime.Now;
                }

                return this._now;
            }
            internal set => this._now = value;
        }

        public Guid? Id
        {
            get
            {
                if (this._id.HasValue == false)
                {
                    return Guid.NewGuid();
                }

                return this._id;
            }
            internal set => this._id = value;
        }

        public string ConnectionString
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this._connectionString))
                {
                    return "LabDbContext";
                }

                return this._connectionString;
            }
            internal set => this._connectionString = value;
        }

        public virtual int Delete(DeleteMemberRequest fromUI, string accessUserId)
        {
            var result     = 0;
            var memberToDb = new Member {Id = fromUI.Id};
            using (var dbContext = LabDbContext.Create(this.ConnectionString))
            {
                dbContext.Configuration.ValidateOnSaveEnabled = false;

                dbContext.Members.Attach(memberToDb);
                var memberEntry = dbContext.Entry(memberToDb);
                memberEntry.State = EntityState.Deleted;
                result            = dbContext.SaveChanges();
            }

            return result;
        }

        public virtual int Insert(InsertMemberRequest fromUI, string accessUserId)
        {
            var result = 0;
            var memberToDb = new Member
            {
                Id       = this.Id.Value,
                Name     = fromUI.Name,
                Age      = fromUI.Age,
                CreateAt = this.Now.Value,
                CreateBy = accessUserId
            };

            using (var dbContext = LabDbContext.Create(this.ConnectionString))
            {
                dbContext.Members.Add(memberToDb);
                result = dbContext.SaveChanges();
            }

            return result;
        }

        public virtual int Update(UpdateMemberRequest fromUI, string accessUserId)
        {
            var result = 0;
            var memberToDb = new Member
            {
                Id       = fromUI.Id,
                Name     = fromUI.Name,
                Age      = fromUI.Age,
                ModifyAt = this.Now.Value,
                ModifyBy = accessUserId
            };

            using (var dbContext = LabDbContext.Create(this.ConnectionString))
            {
                dbContext.Configuration.ValidateOnSaveEnabled = false;

                dbContext.Members.Attach(memberToDb);
                var memberEntry = dbContext.Entry(memberToDb);
                memberEntry.State                                = EntityState.Modified;
                memberEntry.Property(p => p.CreateAt).IsModified = false;
                memberEntry.Property(p => p.CreateBy).IsModified = false;
                result                                           = dbContext.SaveChanges();
            }

            return result;
        }
    }
}