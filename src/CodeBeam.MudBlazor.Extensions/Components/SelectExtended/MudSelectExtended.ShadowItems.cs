using System.Collections.Generic;
using System.Linq;

namespace MudExtensions
{
    public partial class MudSelectExtended<T>
    {
        /// <summary>
        /// Returns only selected values that are present in the current item collection.
        /// The hidden list exists to materialize selected item components for presentation;
        /// it must not materialize the entire collection when the visible list is virtualized.
        /// </summary>
        protected ICollection<T?>? GetShadowItemCollection()
        {
            if (ItemCollection == null)
            {
                return null;
            }

            var selectedValues = SelectedValues?.ToHashSet(_comparer) ?? new HashSet<T?>(_comparer);
            return [.. ItemCollection.Where(selectedValues.Contains)];
        }
    }
}
