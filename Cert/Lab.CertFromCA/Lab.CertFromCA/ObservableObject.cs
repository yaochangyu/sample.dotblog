using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Lab.CertFromCA
{
    public class ObservableObject : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the property changed
        /// </summary>
        /// <param name="propertyExpression">property</param>
        protected void NotifyPropertyChanged<T>(Expression<Func<T>> propertyExpression)
        {
            if (this.PropertyChanged == null)
            {
                return;
            }

            var body = propertyExpression.Body as MemberExpression;
            if (body != null)
            {
                var property = body.Member as PropertyInfo;
                if (property != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs(property.Name));
                }
            }
        }

    }
}