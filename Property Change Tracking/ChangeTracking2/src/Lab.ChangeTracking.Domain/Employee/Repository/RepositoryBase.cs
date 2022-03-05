using ChangeTracking;
using Lab.ChangeTracking.Abstract;
using Microsoft.EntityFrameworkCore;

namespace Lab.ChangeTracking.Domain;

public class RepositoryBase
{
    protected void ApplyChange<TSource, TTarget>(DbContext dbContext,
                                                 TSource source,
                                                 TTarget target,
                                                 IEnumerable<string> excludeProperties = null)
        where TSource : class
        where TTarget : class
    {
        var sourceTrackable = source.CastToIChangeTrackable();
        var changedProperties = sourceTrackable.ChangedProperties;
        dbContext.Set<TTarget>().Attach(target);
        foreach (var property in changedProperties)
        {
            if (excludeProperties != null
                && excludeProperties.Any(p => p == property))
            {
                continue;
            }

            dbContext.Entry(target).Property(property).IsModified = true;
        }
    }

    protected void ApplyChanges<TSource, TTarget>(DbContext dbContext,
                                                  IList<TSource> sources,
                                                  IList<TTarget> targets,
                                                  IEnumerable<string> excludeProperties = null)
        where TSource : class, IEntity
        where TTarget : class, IEntity

    {
        var targetsTrackable = sources.CastToIChangeTrackableCollection();
        if (targetsTrackable == null)
        {
            return;
        }

        var addedItems = targetsTrackable.AddedItems;
        var deletedItems = targetsTrackable.DeletedItems;
        foreach (var source in targetsTrackable.ChangedItems)
        {
            var target = targets.FirstOrDefault(p => p.Id == source.Id);
            if (target == null)
            {
                continue;
            }

            this.ApplyChange(dbContext, source, target, excludeProperties);
        }

        foreach (var addedItem in addedItems)
        {
            
            // dbContext.Add(addedItem as TTarget);
        }

        foreach (var source in deletedItems)
        {
            var target = Activator.CreateInstance<TTarget>();
            target.Id = source.Id;
            dbContext.Set<TTarget>().Attach(target);
            dbContext.Entry(target).State = EntityState.Deleted;
        }
    }
}