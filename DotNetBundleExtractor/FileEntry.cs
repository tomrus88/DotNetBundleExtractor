namespace DotNetBundleExtractor;

internal class FileEntry
{
    public readonly uint BundleMajorVersion;

    public long Offset;
    public long Size;
    public long CompressedSize;
    public FileType Type;
    public string RelativePath; // Path of an embedded file, relative to the Bundle source-directory.

    public FileEntry(uint bundleMajorVersion)
    {
        BundleMajorVersion = bundleMajorVersion;
    }

    public void Read(BinaryReader reader)
    {
        Offset = reader.ReadInt64();
        Size = reader.ReadInt64();

        // compression is used only in version 6.0+
        if (BundleMajorVersion >= 6)
        {
            CompressedSize = reader.ReadInt64();
        }
        Type = (FileType)reader.ReadByte();
        RelativePath = reader.ReadString();
    }

    public override string ToString() => $"{RelativePath} [{Type}] @{Offset} Sz={Size} CompressedSz={CompressedSize}";
}
