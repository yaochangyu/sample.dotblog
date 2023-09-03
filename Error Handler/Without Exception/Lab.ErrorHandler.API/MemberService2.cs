using System.Data;
using FluentValidation;
using Lab.ErrorHandler.API.Extensions;
using Lab.ErrorHandler.API.Models;

namespace Lab.ErrorHandler.API;

public class MemberService2
{
    private MemberWorkflow _workflow;

    public MemberService2(MemberWorkflow workflow)
    {
        this._workflow = workflow;
    }

    //一個方法有多種可能的 Failure
    public async Task<(Failure Failure, bool Data)> BindCellphoneAsync(BindCellphoneRequest request,
        CancellationToken cancel = default)
    {
        var result = await _workflow.ValidateModelAsync(request, cancel)
                .ThenAnyAsync(p => _workflow.GetMemberAsync(request.MemberId, cancel))
                .ThenAnyAsync(p => _workflow.ValidateCellphone(request.Cellphone, cancel))
                .ThenAnyAsync(p => _workflow.SaveChangeAsync(request, cancel))
            ;
        if (result.Failure != null)
        {
            return (result.Failure, false);
        }

        return (null, true);
    }

    public class MemberWorkflow
    {
        private readonly IValidator<BindCellphoneRequest> _validator;

        public MemberWorkflow(IValidator<BindCellphoneRequest> validator)
        {
            this._validator = validator;
        }

        public Failure Failure { get; private set; }

        public async Task<MemberWorkflow> ValidateModelAsync(BindCellphoneRequest request,
            CancellationToken cancel = default)
        {
            var validationResult = await this._validator.ValidateAsync(request, cancel);
            if (validationResult.IsValid == false)
            {
                this.Failure = validationResult.ToFailure();
            }

            return this;
        }

        public async Task<MemberWorkflow> SaveChangeAsync(BindCellphoneRequest request,
            CancellationToken cancel = default)
        {
            if (this.Failure != null)
            {
                return this;
            }

            try
            {
                throw new DBConcurrencyException("insert data row concurrency error.");
            }
            catch (Exception e)
            {
                this.Failure = new Failure
                {
                    Code = FailureCode.DataConcurrency,
                    Message = e.Message,
                    Exception = e,
                    Data = request
                };
            }

            return this;
        }

        public async Task<MemberWorkflow> ValidateCellphone(string cellphone,
            CancellationToken cancel = default)
        {
            if (this.Failure != null)
            {
                return this;
            }

            this.Failure = new Failure
            {
                Code = FailureCode.CellphoneFormatInvalid,
                Message = "Cellphone format invalid.",
                Data = cellphone
            };
            return this;
        }

        public async Task<MemberWorkflow> GetMemberAsync(int memberId,
            CancellationToken cancel = default)
        {
            if (this.Failure != null)
            {
                return this;
            }

            try
            {
                this.Failure = new Failure
                {
                    Code = FailureCode.MemberNotFound,
                    Message = "Member not found.",
                    Data = memberId
                };
            }
            catch (Exception e)
            {
                this.Failure = new Failure
                {
                    Code = FailureCode.DbError,
                    Message = e.Message,
                    Data = memberId,
                    Exception = e,
                };
            }

            return this;
        }
    }
}