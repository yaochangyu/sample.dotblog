using Lab.ChangeTracking.Infrastructure.DB.EntityModel;

namespace Lab.ChangeTracking.Domain;

public static class TypeConverterExtension
{
    public static EmployeeEntity To(this Employee source)
    {
        return new EmployeeEntity
        {
            Id = source.Id,
            Version = source.Version,
            Name = source.Name,
            Age = source.Age,
            Addresses = source.Addresses.To().ToList(),
            Identity = source.Identity?.To(),
            CreatedAt = source.CreatedAt,
            CreatedBy = source.CreatedBy,
            ModifiedAt = source.ModifiedAt,
            ModifiedBy = source.ModifiedBy,
            Remark = source.Remark
        };
    }

    public static Employee To(this EmployeeEntity source)
    {
        return new Employee
        {
            Id = source.Id,
            Version = source.Version,
            Name = source.Name,
            Age = source.Age,
            Addresses = source.Addresses.To().ToList(),
            Identity = source.Identity?.To(),
            CreatedAt = source.CreatedAt,
            CreatedBy = source.CreatedBy,
            ModifiedAt = source.ModifiedAt,
            ModifiedBy = source.ModifiedBy,
            Remark = source.Remark
        };
    }

    public static IdentityEntity To(this Identity source)
    {
        return new IdentityEntity
        {
            Employee_Id = source.Employee_Id,
            Account = source.Account,
            Password = source.Password,
            Remark = source.Remark,
            CreatedAt = source.CreatedAt,
            CreatedBy = source.CreatedBy,
            ModifiedAt = source.ModifiedAt,
            ModifiedBy = source.ModifiedBy
        };
    }

    public static Identity To(this IdentityEntity source)
    {
        return new Identity
        {
            Employee_Id = source.Employee_Id,
            Account = source.Account,
            Password = source.Password,
            Remark = source.Remark,
            CreatedAt = source.CreatedAt,
            CreatedBy = source.CreatedBy,
            ModifiedAt = source.ModifiedAt,
            ModifiedBy = source.ModifiedBy
        };
    }

    public static Address To(this AddressEntity source)
    {
        return new Address
        {
            Id = source.Id,
            Employee_Id = source.Employee_Id,
            Country = source.Country,
            Street = source.Street,
            CreatedAt = source.CreatedAt,
            CreatedBy = source.CreatedBy,
            ModifiedAt = source.ModifiedAt,
            ModifiedBy = source.ModifiedBy,
            Remark = source.Remark,
        };
    }

    public static AddressEntity To(this Address source)
    {
        return new AddressEntity
        {
            Id = source.Id,
            Employee_Id = source.Employee_Id,
            Country = source.Country,
            Street = source.Street,
            CreatedAt = source.CreatedAt,
            CreatedBy = source.CreatedBy,
            ModifiedAt = source.ModifiedAt,
            ModifiedBy = source.ModifiedBy,
            Remark = source.Remark,
        };
    }

    public static IEnumerable<AddressEntity> To(this IEnumerable<Address> sources)
    {
        return sources.Select(p => p.To());
    }

    public static IEnumerable<Address> To(this IEnumerable<AddressEntity> sources)
    {
        return sources.Select(p => p.To());
    }
}