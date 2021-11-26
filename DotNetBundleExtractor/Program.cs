using System.IO.Compression;

namespace DotNetBundleExtractor;

class Program
{
    static readonly byte[] bundleSignature = {
        // 32 bytes represent the bundle signature: SHA-256 for ".net core bundle"
        0x8b, 0x12, 0x02, 0xb9, 0x6a, 0x61, 0x20, 0x38,
        0x72, 0x7b, 0x93, 0x02, 0x14, 0xd7, 0xa0, 0x32,
        0x13, 0xf5, 0xb9, 0xe6, 0xef, 0xae, 0x33, 0x18,
        0xee, 0x3b, 0x2d, 0xce, 0x24, 0xb3, 0x6a, 0xae
    };

    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: DotNetBundleExtractor <path_to_dotnet_bundle>");
            return;
        }

        byte[] bundleBytes = File.ReadAllBytes(args[0]);

        int[] bundleSigOffsets = bundleBytes.Locate(bundleSignature);

        if (bundleSigOffsets.Length == 0)
        {
            Console.WriteLine("Not a bundle?");
            return;
        }

        if (bundleSigOffsets.Length > 1)
        {
            Console.WriteLine("More than one signature?");
            return;
        }

        int bundleSigOffset = bundleSigOffsets[0];

        Console.WriteLine($"Bundle signature found at 0x{bundleSigOffset:X8}");

        int bundleHeaderPosOffset = bundleSigOffset - 8;

        long bundleHeaderOffset = BitConverter.ToInt64(bundleBytes, bundleHeaderPosOffset);

        Console.WriteLine($"Bundle header at 0x{bundleHeaderOffset:X8}");

        using MemoryStream memoryStream = new(bundleBytes);
        memoryStream.Position = bundleHeaderOffset;
        using BinaryReader reader = new(memoryStream);

        Manifest manifest = new Manifest();
        manifest.Read(reader);

        foreach (var file in manifest.Files)
        {
            Console.WriteLine(file);

            memoryStream.Position = file.Offset;

            byte[] fileBytes;
            if (file.CompressedSize > 0)
            {
                byte[] fileBytesCompressed = reader.ReadBytes((int)file.CompressedSize);
                using MemoryStream memoryStreamCompressed = new(fileBytesCompressed);
                using DeflateStream deflate = new(memoryStreamCompressed, CompressionMode.Decompress, true);
                using MemoryStream memoryStreamDecompressed = new((int)file.Size);
                deflate.CopyTo(memoryStreamDecompressed);
                fileBytes = memoryStreamDecompressed.ToArray();
            }
            else
            {
                fileBytes = reader.ReadBytes((int)file.Size);
            }
            File.WriteAllBytes(file.RelativePath, fileBytes);

            Console.WriteLine($"Extracted file {file.RelativePath}");
        }

        Console.ReadKey();
    }
}
