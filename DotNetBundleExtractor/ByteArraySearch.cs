namespace DotNetBundleExtractor;

static class ByteArrayRocks
{
    public static int[] Locate(this byte[] self, byte[] candidate)
    {
        if (IsEmptyLocate(self, candidate))
            return Array.Empty<int>();

        var list = new List<int>();

        for (int i = 0; i < self.Length; i++)
        {
            if (!IsMatch(self, i, candidate))
                continue;

            list.Add(i);
        }

        return list.Count == 0 ? Array.Empty<int>() : list.ToArray();
    }

    static bool IsMatch(byte[] array, int position, byte[] candidate)
    {
        if (candidate.Length > (array.Length - position))
            return false;

        for (int i = 0; i < candidate.Length; i++)
            if (array[position + i] != candidate[i])
                return false;

        return true;
    }

    static bool IsEmptyLocate(byte[] array, byte[] candidate)
    {
        return array == null
            || candidate == null
            || array.Length == 0
            || candidate.Length == 0
            || candidate.Length > array.Length;
    }
}
