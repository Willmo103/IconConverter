using System.Drawing;
using System.Drawing.Imaging;


namespace PngToIcoConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            string inputFilePath = null;
            string outputFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "PngToIcon");
            bool unpack = false;

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
                    case "--unpack":
                        unpack = true;
                        if (i + 1 < args.Length) inputFilePath = args[++i];
                        break;
                    case "--help":
                        PrintManPage();
                        return;
                    default:
                        Console.WriteLine($"Unknown argument: {args[i]}");
                        return;
                }
            }

            if (string.IsNullOrEmpty(inputFilePath))
            {
                Console.WriteLine("Error: Input file must be specified with -i or --unpack <input_ico_file>.");
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
                if (unpack)
                {
                    UnpackIcoFile(inputFilePath, outputFolderPath);
                }
                else
                {
                    ConvertPngToIco(inputFilePath, outputFolderPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void ConvertPngToIco(string inputFilePath, string outputFolderPath)
        {
            using (var pngImage = new Bitmap(inputFilePath))
            {
                if (pngImage == null)
                {
                    Console.WriteLine("Error: Failed to load the input PNG image.");
                    return;
                }

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
                            using (Bitmap resizedImage = new Bitmap(pngImage, new Size(size, size)))
                            {
                                using (MemoryStream memoryStream = new MemoryStream())
                                {
                                    resizedImage.Save(memoryStream, ImageFormat.Png);

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
                            using (Bitmap resizedImage = new Bitmap(pngImage, new Size(size, size)))
                            {
                                using (MemoryStream memoryStream = new MemoryStream())
                                {
                                    resizedImage.Save(memoryStream, ImageFormat.Png);
                                    writer.Write(memoryStream.ToArray());
                                }
                            }
                        }
                    }
                }

                Console.WriteLine($"Successfully converted '{inputFilePath}' to ICO file '{outputFilePath}'.");
            }
        }

        static void UnpackIcoFile(string inputFilePath, string outputFolderPath)
        {
            string outputDir = Path.Combine(outputFolderPath, $"{Path.GetFileNameWithoutExtension(inputFilePath)}_sizes");
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            using (FileStream icoStream = new FileStream(inputFilePath, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(icoStream))
                {
                    reader.BaseStream.Seek(4, SeekOrigin.Begin);
                    short imageCount = reader.ReadInt16();

                    for (int i = 0; i < imageCount; i++)
                    {
                        reader.BaseStream.Seek(6 + i * 16, SeekOrigin.Begin);
                        int width = reader.ReadByte();
                        int height = reader.ReadByte();
                        reader.BaseStream.Seek(8, SeekOrigin.Current);
                        int imageSize = reader.ReadInt32();
                        int imageOffset = reader.ReadInt32();

                        reader.BaseStream.Seek(imageOffset, SeekOrigin.Begin);
                        byte[] imageData = reader.ReadBytes(imageSize);

                        // Use MemoryStream to read PNG properly
                        using (MemoryStream memoryStream = new MemoryStream(imageData))
                        {
                            using (Bitmap bmp = new Bitmap(memoryStream))
                            {
                                string outputFilePath = Path.Combine(outputDir, $"{Path.GetFileNameWithoutExtension(inputFilePath)}_{width}x{height}.png");
                                bmp.Save(outputFilePath, ImageFormat.Png);
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"Successfully unpacked ICO file '{inputFilePath}' to '{outputDir}'.");
        }

        static void PrintManPage()
        {
            Console.WriteLine("PngToIcoConverter - A tool to convert PNG images to ICO files and unpack ICO files.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  PngToIcoConverter -i <input_png_file> [-o <output_folder>] [--help]");
            Console.WriteLine("  PngToIcoConverter --unpack <input_ico_file> [-o <output_folder>] [--help]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -i <input_png_file>   Specify the input PNG file to be converted.");
            Console.WriteLine("  -o <output_folder>    Specify the output folder for the ICO file or unpacked images (default: ~/Pictures/PngToIcon).");
            Console.WriteLine("  --unpack <input_ico_file>  Unpack an ICO file into its constituent image sizes.");
            Console.WriteLine("  --help                Display this help message.");
        }
    }
}
