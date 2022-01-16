using Lab.NETMiniProfiler.Infrastructure.EFCore5.EntityModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Profiling;

namespace Lab.NETMiniProfiler.ASPNetCore5.Controllers
{
    /// <summary>
    ///     Value Controller
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly IDbContextFactory<EmployeeDbContext> _employeeDbContextFactory;

        public ValuesController(IDbContextFactory<EmployeeDbContext> employeeDbContextFactory)
        {
            this._employeeDbContextFactory = employeeDbContextFactory;
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        /// <summary>
        ///     Get Api
        /// </summary>
        /// <returns></returns>

        // GET api/values
        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancel = default)
        {
            using (MiniProfiler.Current.Step("查詢資料庫"))
            {
                await using var db = this._employeeDbContextFactory.CreateDbContext();
                return this.Ok(await db.Employees.AsTracking().ToListAsync(cancel));
            }
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public async Task<IActionResult> Post(CancellationToken cancellationToken = default)
        {
            using (MiniProfiler.Current.Step("異動資料庫"))
            {
                await using var db = this._employeeDbContextFactory.CreateDbContext();

                var toDb = new Employee
                {
                    Id = Guid.NewGuid(),
                    CreateAt = DateTimeOffset.Now,
                    CreateBy = Faker.Name.FullName(),
                    Age = Faker.RandomNumber.Next(1, 100),
                    Name = Faker.Name.Suffix(),
                };
                db.Employees.Add(toDb);
                await db.SaveChangesAsync(cancellationToken);
                return this.Ok(toDb);
            }
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }
    }
}