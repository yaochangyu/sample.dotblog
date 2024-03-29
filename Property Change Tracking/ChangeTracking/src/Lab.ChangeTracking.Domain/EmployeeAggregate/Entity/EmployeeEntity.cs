﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ChangeTracking;
using Lab.ChangeTracking.Domain.Annotations;

namespace Lab.ChangeTracking.Domain.EmployeeAggregate.Entity;

public record EmployeeEntity
{
    public virtual Guid Id { get; init; }

    public virtual string Name { get; set; }

    public virtual int? Age { get; set; }

    public virtual string Remark { get; set; }

    public virtual IList<ProfileEntity> Profiles { get; init; }

    public virtual DateTimeOffset CreateAt { get; set; }

    public virtual string CreateBy { get; set; }

    public virtual IdentityEntity Identity { get; init; }
}