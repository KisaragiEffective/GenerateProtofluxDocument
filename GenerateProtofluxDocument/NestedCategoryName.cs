namespace GenerateProtoFluxDocument;

internal class NestedCategoryName : IComparable<NestedCategoryName>, IEquatable<NestedCategoryName>, IEqualityComparer<NestedCategoryName>
{
    internal NestedCategoryName(string raw)
    {
        if (raw == "")
        {
            throw new ArgumentException("must not be null", nameof(raw));
        }
        
        this.Name = raw;
    }

    private string Name { get; }
    public int CompareTo(NestedCategoryName? other)
    {
        // null
        // a
        // a/b
        // a/b/c
        // ad
        // ad/e
        // ad/e/f
        // ad/g
        // b
        if (other == null)
        {
            return 1;
        }

        var l = this.Name.Split('/').ToArray();
        var r = other.Name.Split('/').ToArray();

        // invariant: Name is not empty, so split does not yield empty
        var l0 = l[0];
        var r0 = r[0];

        {
            var tmp = String.Compare(l0, r0, StringComparison.Ordinal);
            if (tmp != 0)
            {
                return tmp;
            }
        }

        {
            var tmp = l.Length.CompareTo(r.Length);
            if (tmp != 0)
            {
                return tmp;
            }
        }
        
        // now, l.Length and r.Length is equal

        for (var i = 1; i < l.Length; i++)
        {
            var l1 = l[i];
            var r1 = r[i];

            var tmp = String.Compare(l1, r1, StringComparison.Ordinal);
            if (tmp != 0)
            {
                return tmp;
            }
        }

        return 0;
    }

    public bool Equals(NestedCategoryName? other)
    {
        return other != null && this.Name == other.Name;
    }

    public bool Equals(NestedCategoryName? x, NestedCategoryName? y)
    {
        return ReferenceEquals(x, y) || (x != null && x.Equals(y));
    }

    public int GetHashCode(NestedCategoryName obj)
    {
        return obj.Name.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as NestedCategoryName);
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    public override string ToString()
    {
        return Name;
    }
}