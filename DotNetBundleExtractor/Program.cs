using CommandLine;
using CommandLine.Text;
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

    public class Options
    {
        [Value(0, Required = true, HelpText = "Input file.", MetaName = "InputFile")]
        public string InputFile { get; set; }

        [Option('o', "out", Required = false, HelpText = "Output folder.", Default = "extracted")]
        public string OutputPath { get; set; }

        [Usage(ApplicationAlias = "DotNetBundleExtractor")]
        public static IEnumerable<Example> Examples => new List<Example>
        {
            new Example("Extract dotnet bundle to default folder", new Options { InputFile = "file.exe" }),
            new Example("Extract dotnet bundle to specific folder", new Options { InputFile = "file.exe", OutputPath = "folder" })
        };
    }

    static void ExtractBundle(Options options)
    {
        byte[] bundleBytes = File.ReadAllBytes(options.InputFile);

        int[] bundleSigOffsets = bundleBytes.Locate(bundleSignature);

        if (bundleSigOffsets.Length == 0)
        {
            Console.WriteLine("Error: not a bundle?");
            return;
        }

        if (bundleSigOffsets.Length > 1)
        {
            Console.WriteLine("Error: more than one signature?");
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
                using DeflateStream deflate = new(memoryStreamCompressed, CompressionMode.Decompress);
                using MemoryStream memoryStreamDecompressed = new((int)file.Size);
                deflate.CopyTo(memoryStreamDecompressed);
                fileBytes = memoryStreamDecompressed.ToArray();
            }
            else
            {
                fileBytes = reader.ReadBytes((int)file.Size);
            }

            string filePath = Path.Combine(options.OutputPath, file.RelativePath);
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }
            File.WriteAllBytes(filePath, fileBytes);

            Console.WriteLine($"Extracted file {file.RelativePath}");
        }
    }

    static void Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed(ExtractBundle);
    }
}
