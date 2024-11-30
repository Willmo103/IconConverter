using System.Drawing;
using System.Drawing.Imaging;

namespace PngToIcoConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: PngToIcoConverter <input_png_file> <output_folder>");
                return;
            }

            string inputFilePath = args[0];
            string outputFolderPath = args[1];

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
                using (Bitmap pngImage = (Bitmap)Image.FromFile(inputFilePath))
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
                }

                Console.WriteLine($"Successfully converted '{inputFilePath}' to ICO file '{outputFolderPath}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
