using System;
using System.Collections.Generic;
using System.Linq;

namespace Adliance.QmDoc.Extensions;

/// <summary>
/// Finds chapters, table columns or table rows by only a part of their text, eg. "Projektmeldung" for "Projektmeldung (Forschungsregister)".
/// </summary>
public static class LenientMatching
{
    /// <summary>
    /// Returns the indexes of all names that start with <paramref name="search"/> or, if there are none, that contain it (ignoring upper/lower case).
    /// Callers should check for an exact match first, and treat more than one result as ambiguous.
    /// </summary>
    public static List<int> FindPartial(IReadOnlyList<string> names, string search)
    {
        search = search.Trim();
        if (search.Length == 0) return [];

        var startsWith = IndexesWhere(names, n => n.StartsWith(search, StringComparison.OrdinalIgnoreCase));
        return startsWith.Count > 0 ? startsWith : IndexesWhere(names, n => n.Contains(search, StringComparison.OrdinalIgnoreCase));
    }

    private static List<int> IndexesWhere(IReadOnlyList<string> names, Func<string, bool> predicate)
    {
        return names.Select((name, index) => (Name: name, Index: index)).Where(x => predicate(x.Name)).Select(x => x.Index).ToList();
    }
}
