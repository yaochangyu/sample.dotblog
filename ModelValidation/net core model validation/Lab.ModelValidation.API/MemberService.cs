using Lab.ModelValidation.API.Models;

namespace Lab.ModelValidation.API;

public class MemberService
{
    //一個方法有多種可能的 Failure
    public (Failure Failure, bool Data) GetMember(int id)
    {
        if (id == 1)
        {
            return (new Failure
            {
                Code = FailureCode.MemberNotFound,
                Message = "Member not found.",
            }, true);
        }

        if (id == 2)
        {
            return (new Failure
            {
                Code = FailureCode.MemberAlreadyExist,
                Message = "Member already exist.",
            }, true);
        }

        if (id == 3)
        {
            return (new Failure
            {
                Code = FailureCode.DataConcurrency,
                Message = "Data concurrency error.",
            }, true);
        }

        return (null, false);
    }

    //具有多個 Detail 的 Failure
    public (Failure Failure, bool Data) GetMember1()
    {
        var failure = new Failure()
        {
            Code = FailureCode.InputValid,
            Message = "view detail errors",
            Details = new List<Failure>()
            {
                new(code: FailureCode.MemberNotFound, message: "Member not found."),
                new(code: FailureCode.MemberAlreadyExist, message: "Member already exist.")
            }
        };
        return (failure, false);
    }
}