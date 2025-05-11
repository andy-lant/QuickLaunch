using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QuickLaunch.Core.Utils
{
    /// <summary>
    ///  Extends ICollection methods.
    /// </summary>
    public interface IExtendedCollection<T> : ICollection<T>
    {
        public void AddRange(IEnumerable<T> collection)
        {
            foreach (T item in collection)
            {
                Add(item);
            }
        }
    }

    /// <summary>An ObservableCollection extended by IExtendedCollection.</summary>
    public class ExtendedObservableCollection<T> : ObservableCollection<T>
    {
        public ExtendedObservableCollection() : base()
        {

        }

        public ExtendedObservableCollection(IEnumerable<T> collection) : base(collection)
        {
        }
    }
}
