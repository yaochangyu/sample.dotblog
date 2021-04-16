using System.Threading;
using System.Threading.Tasks;
using Lab.DAL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ServiceModel = Lab.WebApi.ServiceModel;
using DomainModel = Lab.DAL.DomainModel;

namespace Lab.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DefaultController : ControllerBase
    {
        private readonly ILogger<DefaultController> _logger;
        private readonly EmployeeRepository         _repository;

        public DefaultController(ILogger<DefaultController> logger, EmployeeRepository repository)
        {
            this._logger     = logger;
            this._repository = repository;
        }

        [HttpGet]
        [Produces(typeof(ServiceModel.Employee.FilterResponse))]
        public async Task<IActionResult> Get(CancellationToken cancel = default)
        {
            var repository = this._repository;
            var record     = await repository.GetAllAsync(cancel);
            return this.Ok(record);
        }

        [HttpPost]
        public async Task<IActionResult> Post(ServiceModel.Employee.NewRequest request,
                                              string                           accessId,
                                              CancellationToken                cancel = default)
        {
            var repository = this._repository;
            var count = await repository.NewAsync(new DomainModel.Employee.NewRequest()
            {
                Account  = request.Account,
                Age      = request.Age,
                Name     = request.Name,
                Password = request.Password,
                Remark   = request.Remark
            }, accessId, cancel);
            if (count == 2)
            {
                return this.Ok();
            }
            else
            {
                return this.NoContent();
            }
        }
    }
}