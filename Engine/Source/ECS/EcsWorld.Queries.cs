namespace Engine;

public sealed partial class EcsWorld
{
    public IEnumerable<(int Entity, T1 C1, T2 C2)> Query<T1, T2>()
    {
        var s1 = GetStore<T1>(create: false);
        var s2 = GetStore<T2>(create: false);
        if (s1 == null || s2 == null) yield break;
        if (s1.Count <= s2.Count)
        {
            foreach (var (e, c1) in s1.Enumerate())
                if (s2.TryGet(e, out var c2))
                    yield return (e, c1, c2);
        }
        else
        {
            foreach (var (e, c2) in s2.Enumerate())
                if (s1.TryGet(e, out var c1))
                    yield return (e, c1, c2);
        }
    }

    public IEnumerable<(int Entity, T1 C1, T2 C2, T3 C3)> Query<T1, T2, T3>()
    {
        var s1 = GetStore<T1>(create: false);
        var s2 = GetStore<T2>(create: false);
        var s3 = GetStore<T3>(create: false);
        if (s1 == null || s2 == null || s3 == null) yield break;
        var c1 = s1.Count;
        var c2 = s2.Count;
        var c3 = s3.Count;
        int smallestCase = (c1 <= c2 && c1 <= c3) ? 1 : (c2 <= c1 && c2 <= c3) ? 2 : 3;
        if (smallestCase == 1)
        {
            foreach (var (e, comp1) in s1.Enumerate())
            {
                if (!s2.TryGet(e, out var comp2) || !s3.TryGet(e, out var comp3)) continue;
                yield return (e, comp1, comp2, comp3);
            }
        }
        else if (smallestCase == 2)
        {
            foreach (var (e, comp2) in s2.Enumerate())
            {
                if (!s1.TryGet(e, out var comp1) || !s3.TryGet(e, out var comp3)) continue;
                yield return (e, comp1, comp2, comp3);
            }
        }
        else
        {
            foreach (var (e, comp3) in s3.Enumerate())
            {
                if (!s1.TryGet(e, out var comp1) || !s2.TryGet(e, out var comp2)) continue;
                yield return (e, comp1, comp2, comp3);
            }
        }
    }

    public IEnumerable<(int Entity, T Component)> QueryWhere<T>(Func<T, bool> predicate)
    {
        foreach (var (entity, comp) in Query<T>())
            if (predicate(comp))
                yield return (entity, comp);
    }
}