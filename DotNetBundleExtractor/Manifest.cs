namespace DotNetBundleExtractor;

internal class Manifest
{
    [Flags]
    private enum HeaderFlags : ulong
    {
        None = 0,
        NetcoreApp3CompatMode = 1
    }

    public uint BundleMajorVersion;
    // The Minor version is currently unused, and is always zero
    public uint BundleMinorVersion;
    private FileEntry DepsJsonEntry;
    private FileEntry RuntimeConfigJsonEntry;
    private HeaderFlags Flags;
    public List<FileEntry> Files;
    public string BundleID;

    public void Read(BinaryReader reader)
    {
        // Write the bundle header
        BundleMajorVersion = reader.ReadUInt32();
        BundleMinorVersion = reader.ReadUInt32();
        int filesCount = reader.ReadInt32();
        BundleID = reader.ReadString();

        if (BundleMajorVersion >= 2)
        {
            DepsJsonEntry = new FileEntry(BundleMajorVersion);
            DepsJsonEntry.Offset = reader.ReadInt64();
            DepsJsonEntry.Size = reader.ReadInt64();

            RuntimeConfigJsonEntry = new FileEntry(BundleMajorVersion);
            RuntimeConfigJsonEntry.Offset = reader.ReadInt64();
            RuntimeConfigJsonEntry.Size = reader.ReadInt64();

            Flags = (HeaderFlags)reader.ReadUInt64();
        }

        Files = new List<FileEntry>(filesCount);

        // Read the manifest entries
        for (int i = 0; i < filesCount; i++)
        {
            var fileEntry = new FileEntry(BundleMajorVersion);
            fileEntry.Read(reader);
            Files.Add(fileEntry);
        }
    }
}
