// using Lab.ChangeTracking.Infrastructure.DB.EntityModel;
//
// namespace Lab.ChangeTracking.Domain;
//
// public static class TypeConverterExtensions
// {
//     public static Employee To(this EmployeeValueObject srcEmployee)
//     {
//         if (srcEmployee == null)
//         {
//             return null;
//         }
//
//         return new Employee
//         {
//             Id = srcEmployee.Id,
//             Name = srcEmployee.Name,
//             Age = srcEmployee.Age,
//             Version = srcEmployee.Version,
//             Remark = srcEmployee.Remark,
//             Addresses = srcEmployee.Addresses != null ? srcEmployee.Addresses.To().ToList() : null,
//             Identity = srcEmployee.Identity.To(),
//             CreatedAt = srcEmployee.CreatedAt,
//             CreatedBy = srcEmployee.CreatedBy,
//             ModifiedAt = srcEmployee.ModifiedAt,
//             ModifiedBy = srcEmployee.ModifiedBy
//         };
//     }
//
//     public static EmployeeValueObject To(this Employee srcEmployee)
//     {
//         if (srcEmployee == null)
//         {
//             return null;
//         }
//
//         return new EmployeeValueObject
//         {
//             Id = srcEmployee.Id,
//             Name = srcEmployee.Name,
//             Age = srcEmployee.Age,
//             Version = srcEmployee.Version,
//             Remark = srcEmployee.Remark,
//             Addresses = srcEmployee.Addresses.To()?.ToList(),
//             Identity = srcEmployee.Identity.To(),
//             CreatedAt = srcEmployee.CreatedAt,
//             CreatedBy = srcEmployee.CreatedBy,
//             ModifiedAt = srcEmployee.ModifiedAt,
//             ModifiedBy = srcEmployee.ModifiedBy
//         };
//     }
//
//     public static Identity To(this IdentityEntity srcIdentity)
//     {
//         if (srcIdentity == null)
//         {
//             return null;
//         }
//
//         return new Identity
//         {
//             Employee_Id = srcIdentity.Employee_Id,
//             Account = srcIdentity.Account,
//             Password = srcIdentity.Password,
//             Remark = srcIdentity.Remark,
//             CreatedAt = srcIdentity.CreatedAt,
//             CreatedBy = srcIdentity.CreatedBy,
//             ModifiedAt = srcIdentity.ModifiedAt,
//             ModifiedBy = srcIdentity.ModifiedBy
//         };
//     }
//
//     public static IdentityEntity To(this Identity srcIdentity)
//     {
//         if (srcIdentity == null)
//         {
//             return null;
//         }
//
//         return new IdentityEntity
//         {
//             Employee_Id = srcIdentity.Employee_Id,
//             Account = srcIdentity.Account,
//             Password = srcIdentity.Password,
//             Remark = srcIdentity.Remark,
//             CreatedAt = srcIdentity.CreatedAt,
//             CreatedBy = srcIdentity.CreatedBy,
//             ModifiedAt = srcIdentity.ModifiedAt,
//             ModifiedBy = srcIdentity.ModifiedBy
//         };
//     }
//
//     public static Address To(this AddressEntity srcAddress)
//     {
//         if (srcAddress == null)
//         {
//             return null;
//         }
//
//         return new Address
//         {
//             Id = srcAddress.Id,
//             Employee_Id = srcAddress.Employee_Id,
//             Country = srcAddress.Country,
//             Street = srcAddress.Street,
//             CreatedAt = srcAddress.CreatedAt,
//             CreatedBy = srcAddress.CreatedBy,
//             ModifiedAt = srcAddress.ModifiedAt,
//             ModifiedBy = srcAddress.ModifiedBy,
//             Remark = srcAddress.Remark
//         };
//     }
//
//     public static AddressEntity To(this Address srcAddress)
//     {
//         if (srcAddress == null)
//         {
//             return null;
//         }
//
//         return new AddressEntity
//         {
//             Id = srcAddress.Id,
//             Employee_Id = srcAddress.Employee_Id,
//             Country = srcAddress.Country,
//             Street = srcAddress.Street,
//             CreatedAt = srcAddress.CreatedAt,
//             CreatedBy = srcAddress.CreatedBy,
//             ModifiedAt = srcAddress.ModifiedAt,
//             ModifiedBy = srcAddress.ModifiedBy,
//             Remark = srcAddress.Remark
//         };
//     }
//
//     public static IEnumerable<Address> To(this IEnumerable<AddressEntity> srcProfiles)
//     {
//         return srcProfiles?.Select(p => p?.To());
//     }
//
//     public static IEnumerable<AddressEntity> To(this IEnumerable<Address> srcProfiles)
//     {
//         return srcProfiles?.Select(p => p?.To());
//     }
// }

