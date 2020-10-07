using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tako.Collections.Generic
{
    public class SortableBindingList<T> : BindingList<T>
    {
        private readonly Dictionary<string, PropertyComparer<T>> _comparerList = new Dictionary<string, PropertyComparer<T>>();

        private ListSortDirection _sortDirection;
        private PropertyDescriptor _property;

        public SortableBindingList()
            : base(new List<T>())
        {
        }

        public SortableBindingList(IList<T> List)
            : base(List)
        {
        }

        public SortableBindingList(IEnumerable<T> Enumerable)
            : base(new List<T>(Enumerable))
        {
        }

        protected override bool SupportsSortingCore
        {
            get { return true; }
        }

        protected override bool IsSortedCore
        {
            get { return true; }
        }

        protected override PropertyDescriptor SortPropertyCore
        {
            get { return this._property; }
        }

        protected override ListSortDirection SortDirectionCore
        {
            get { return this._sortDirection; }
        }

        //protected override void ApplySortCore(PropertyDescriptor property, ListSortDirection sortDirection)
        //{
        //    List<T> list = (List<T>)this.Items;
        //    var name = property.Name;
        //    PropertyComparer<T> comparer;

        //    if (!this._comparerList.TryGetValue(name, out comparer))
        //    {
        //        comparer = new PropertyComparer<T>(property, sortDirection);
        //        this._comparerList.Add(name, comparer);
        //    }

        //    comparer.SetDirection(sortDirection);
        //    list.Sort(comparer);

        //    this._property = property;
        //    this._sortDirection = sortDirection;
        //    this.OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        //}

        protected override void ApplySortCore(PropertyDescriptor property, ListSortDirection direction)
        {
            List<T> itemsList = (List<T>)this.Items;
            if (property.PropertyType.GetInterface("IComparable") != null)
            {
                itemsList.Sort((x, y) =>
                {
                    if (property.GetValue(x) != null)
                        return ((IComparable)property.GetValue(x)).CompareTo(property.GetValue(y)) *
                               (direction == ListSortDirection.Descending ? -1 : 1);
                    else if (property.GetValue(y) != null)
                        return ((IComparable)property.GetValue(y)).CompareTo(property.GetValue(x)) *
                               (direction == ListSortDirection.Descending ? 1 : -1);
                    else
                        return 0;
                });
            }
            this._property = property;
            this._sortDirection = direction;
        }
    }
}