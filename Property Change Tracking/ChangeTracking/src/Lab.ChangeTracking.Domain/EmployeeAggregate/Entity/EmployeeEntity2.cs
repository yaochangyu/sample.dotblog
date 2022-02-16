using System.ComponentModel;
using System.Runtime.CompilerServices;
using ChangeTracking;
using Lab.ChangeTracking.Domain.Annotations;

namespace Lab.ChangeTracking.Domain.EmployeeAggregate.Entity;

public record EmployeeEntity2 : BaseEntity
{
    private string _name;
    private int? _age;

    public virtual Guid Id { get; init; }

    public virtual string Name
    {
        get => this._name;
        init => this._name = value;
    }

    public virtual int? Age
    {
        get => this._age;
        init => this._age = value;
    }

    public virtual string Remark { get; set; }

    public virtual DateTimeOffset CreateAt { get; set; }

    public virtual string CreateBy { get; set; }

    public EmployeeEntity2 SetName(string name)
    {
        this._name = name;
        return this;
    }

    public EmployeeEntity2 SetAge(int age)
    {
        if (this._age != age)
        {
            this._age = age;
            this.Track(nameof(this.Age), age);
        }

        return this;
    }
}