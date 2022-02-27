using Lab.ChangeTracking.Domain.Entity;

namespace Lab.ChangeTracking.Domain;

public class EmployeeAggregate : AggregationRoot<EmployeeEntity>
{
    public EmployeeAggregate(EmployeeEntity instance) : base(instance)
    {
    }

    public EmployeeAggregate SetName(string name)
    {
        var instance = this.GetInstance();
        if (instance.Name != name)
        {
            this.Track(p => p.Name = name);
        }

        return this;
    }
}