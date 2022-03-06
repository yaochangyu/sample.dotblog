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
        var targetInstance = CreateTargetInstance<TSource, TTarget>(source, excludeProperties);
        dbContext.Set<TTarget>().Attach(targetInstance);

        var changedProperties = sourceTrackable.ChangedProperties;
        foreach (var changedProperty in changedProperties)
        {
            if (excludeProperties != null
                && excludeProperties.Any(p => p == changedProperty))
            {
                continue;
            }

            dbContext.Entry(targetInstance).Property(changedProperty).IsModified = true;
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

        var modifyItems = targetsTrackable.ChangedItems;
        var addedItems = targetsTrackable.AddedItems;
        var deletedItems = targetsTrackable.DeletedItems;
        foreach (var source in modifyItems)
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
            var targetInstance = CreateTargetInstance<TSource, TTarget>(addedItem,excludeProperties);
            dbContext.Entry(targetInstance).State = EntityState.Added;
        }

        foreach (var source in deletedItems)
        {
            var target = Activator.CreateInstance<TTarget>();
            target.Id = source.Id;
            dbContext.Set<TTarget>().Attach(target);
            dbContext.Entry(target).State = EntityState.Deleted;
        }
    }

    private static TTarget CreateTargetInstance<TSource, TTarget>(TSource sourceInstance,
                                                                  IEnumerable<string> excludeProperties = null)
        where TSource : class
        where TTarget : class
    {
        var targetType = typeof(TTarget);
        var targetInstance = (TTarget)Activator.CreateInstance(targetType);
        var targetProperties = targetInstance.GetType().GetProperties();
        var sourceType = typeof(TSource);
        var sourceProperties = sourceType.GetProperties();
        foreach (var sourceProperty in sourceProperties)
        {
            if (excludeProperties != null &&
                excludeProperties.Contains(sourceProperty.Name))
            {
                continue;
            }

            foreach (var targetProperty in targetProperties)
            {
                if (sourceProperty.Name == targetProperty.Name
                    & sourceProperty.PropertyType == targetProperty.PropertyType
                   )
                {
                    var value = sourceProperty.GetValue(sourceInstance, null);
                    targetProperty.SetValue(targetInstance, value, null);
                    break;
                }
            }
        }

        return targetInstance;
    }
}