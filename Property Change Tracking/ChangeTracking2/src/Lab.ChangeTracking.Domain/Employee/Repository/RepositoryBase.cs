using ChangeTracking;
using Microsoft.EntityFrameworkCore;

namespace Lab.ChangeTracking.Domain;

public class RepositoryBase
{
    protected void ApplyAdd<TSource, TTarget>(DbContext dbContext,
                                              TSource sourceInstance,
                                              IEnumerable<string> excludeProperties = null)
        where TSource : class
        where TTarget : class
    {
        var targetInstance = CreateNewInstance<TSource, TTarget>(sourceInstance, excludeProperties);
        dbContext.Entry(targetInstance).State = EntityState.Added;
    }

    protected void ApplyAdd<TSource, TTarget>(DbContext dbContext, TSource source)
        where TSource : class where TTarget : class
    {
        var targetInstance = CreateDeleteInstance<TSource, TTarget>(source, "Id");
        dbContext.Set<TTarget>().Attach(targetInstance);
        dbContext.Entry(targetInstance).State = EntityState.Deleted;
    }

    protected void ApplyChanges<TSource, TTarget>(DbContext dbContext,
                                                  IList<TSource> sources,
                                                  IEnumerable<string> excludeProperties = null)
        where TSource : class
        where TTarget : class

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
            this.ApplyModify<TSource, TTarget>(dbContext, source, excludeProperties);
        }

        foreach (var addedItem in addedItems)
        {
            this.ApplyAdd<TSource, TTarget>(dbContext, addedItem, excludeProperties);
        }

        foreach (var source in deletedItems)
        {
            this.ApplyAdd<TSource, TTarget>(dbContext, source);
        }
    }

    protected void ApplyModify<TSource, TTarget>(DbContext dbContext,
                                                 TSource source,
                                                 IEnumerable<string> excludeProperties = null)
        where TSource : class
        where TTarget : class
    {
        var sourceTrackable = source.CastToIChangeTrackable();
        var targetInstance = CreateNewInstance<TSource, TTarget>(source, excludeProperties);
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

    private static TTarget CreateDeleteInstance<TSource, TTarget>(TSource sourceInstance, string propertyName)
        where TSource : class
        where TTarget : class
    {
        var targetType = typeof(TTarget);
        var targetInstance = (TTarget)Activator.CreateInstance(targetType);
        var targetProperty = targetType.GetProperty(propertyName);
        var sourceType = sourceInstance.GetType();
        var sourceProperty = sourceType.GetProperty(propertyName);
        var value = sourceProperty.GetValue(sourceInstance, null);
        targetProperty.SetValue(targetInstance, value, null);

        return targetInstance;
    }

    private static TTarget CreateNewInstance<TSource, TTarget>(TSource sourceInstance,
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
                    & sourceProperty.PropertyType == targetProperty.PropertyType)
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