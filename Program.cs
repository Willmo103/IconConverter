using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PngToIcoConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            string inputFilePath = null;
            string outputFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "PngToIcon");

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-i":
                        if (i + 1 < args.Length) inputFilePath = args[++i];
                        break;
                    case "-o":
                        if (i + 1 < args.Length) outputFolderPath = args[++i];
                        break;
                    case "--help":
                        PrintManPage();
                        return;
                    default:
                        Console.WriteLine($"Unknown argument: {args[i]}");
                        PrintManPage();
                        return;
                }
            }

            if (string.IsNullOrEmpty(inputFilePath))
            {
                Console.WriteLine("Error: Input file must be specified with -i.");
                return;
            }

            if (!File.Exists(inputFilePath))
            {
                Console.WriteLine($"Error: The file '{inputFilePath}' does not exist.");
                return;
            }

            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
            }

            try
            {
                ConvertPngToIco(inputFilePath, outputFolderPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void ConvertPngToIco(string inputFilePath, string outputFolderPath)
        {
            try
            {
                using (Image<Rgba32> pngImage = Image.Load<Rgba32>(inputFilePath))
                {
                    // Define the different sizes for the ICO files
                    List<int> iconSizes = new List<int> { 16, 32, 48, 64, 128, 256 };
                    string outputFilePath = Path.Combine(outputFolderPath, $"{Path.GetFileNameWithoutExtension(inputFilePath)}.ico");

                    using (FileStream icoStream = new FileStream(outputFilePath, FileMode.Create))
                    {
                        using (BinaryWriter writer = new BinaryWriter(icoStream))
                        {
                            // Write the ICO header
                            writer.Write((short)0); // Reserved
                            writer.Write((short)1); // ICO type
                            writer.Write((short)iconSizes.Count); // Number of images

                            long imageDataOffset = 6 + (iconSizes.Count * 16);
                            long currentOffset = imageDataOffset;

                            foreach (int size in iconSizes)
                            {
                                using (Image<Rgba32> resizedImage = pngImage.Clone(x => x.Resize(size, size)))
                                {
                                    using (MemoryStream memoryStream = new MemoryStream())
                                    {
                                        resizedImage.Save(memoryStream, new PngEncoder());

                                        // Write the directory entry
                                        writer.Write((byte)size); // Width
                                        writer.Write((byte)size); // Height
                                        writer.Write((byte)0); // Number of colors (0 if not a palette)
                                        writer.Write((byte)0); // Reserved
                                        writer.Write((short)1); // Color planes
                                        writer.Write((short)32); // Bits per pixel
                                        writer.Write((int)memoryStream.Length); // Size of the image data
                                        writer.Write((int)currentOffset); // Offset of the image data

                                        currentOffset += memoryStream.Length;
                                    }
                                }
                            }

                            foreach (int size in iconSizes)
                            {
                                using (Image<Rgba32> resizedImage = pngImage.Clone(x => x.Resize(size, size)))
                                {
                                    using (MemoryStream memoryStream = new MemoryStream())
                                    {
                                        resizedImage.Save(memoryStream, new PngEncoder());
                                        writer.Write(memoryStream.ToArray());
                                    }
                                }
                            }
                        }
                    }

                    Console.WriteLine($"Successfully converted '{inputFilePath}' to ICO file '{outputFilePath}'.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void PrintManPage()
        {
            Console.WriteLine("PngToIcoConverter - A tool to convert PNG images to ICO files.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  PngToIcoConverter -i <input_png_file> [-o <output_folder>] [--help]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -i <input_png_file>   Specify the input PNG file to be converted.");
            Console.WriteLine("  -o <output_folder>    Specify the output folder for the ICO file (default: ~/Pictures/PngToIcon).");
            Console.WriteLine("  --help                Display this help message.");
        }
    }
}
