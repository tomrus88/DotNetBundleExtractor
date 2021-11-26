using System.Runtime.CompilerServices;

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

        long bundleHeaderOffset = Unsafe.As<byte, long>(ref bundleBytes[bundleHeaderPosOffset]);

        Console.WriteLine($"Bundle header at 0x{bundleHeaderOffset:X8}");

        byte[] bundleHeaderBytes = bundleBytes[(int)bundleHeaderOffset..];

        Manifest manifest = new Manifest();
        using MemoryStream memoryStream = new MemoryStream(bundleHeaderBytes);
        using BinaryReader reader = new BinaryReader(memoryStream);
        manifest.Read(reader);

        foreach (var file in manifest.Files)
        {
            Console.WriteLine(file);

            // TODO: support compressed files as well
            if (file.CompressedSize > 0)
            {
                Console.WriteLine($"Skipped compressed file {file.RelativePath}");
                continue;
            }

            byte[] fileBytes = bundleBytes[(int)file.Offset..(int)(file.Offset + file.Size)];
            File.WriteAllBytes(file.RelativePath, fileBytes);

            Console.WriteLine($"Extracted file {file.RelativePath}");
        }

        Console.ReadKey();
    }
}
