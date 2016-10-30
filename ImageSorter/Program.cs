using System;
using System.IO;
using System.Linq;
using ExifLib;

namespace ImageSorter
{
    internal static class Program
    {
        //   private const string OnedriveFolder = @"F:\OneDrive\Bilder";
        private const string OnedriveFolder = @"F:\test\original";
        //   private const string ResultFolder = @"F:\sorted";
        private const string ResultFolder = @"F:\test\sorted";

        private const string FolderFormat = "{0}-{1}";

        private static void Main()
        {
            var files = Directory.EnumerateFiles(OnedriveFolder, "*.*", SearchOption.AllDirectories).ToArray();
            //   .Where(s => s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".mov", StringComparison.OrdinalIgnoreCase))
            //  .ToArray();
            var i = 0;
            var total = files.Length;
            foreach (var file in files)
            {
                if (i % 100 == 0)
                {
                    Console.WriteLine($"Copying file {i + 1}/{total} - {file}");
                }
                var extension = Path.GetExtension(file)?.ToLower();
                if (string.IsNullOrEmpty(extension))
                {
                    continue;
                }
                DateTime dateTaken;
                switch (extension)
                {
                    case ".jpg":
                    case ".jpeg":
                        using (var exifReader = new ExifReader(file))
                        {
                            if (!exifReader.GetTagValue(ExifTags.DateTimeDigitized, out dateTaken))
                            {
                                Console.WriteLine($"Can not read EXIF from file {file}");
                                dateTaken = File.GetLastWriteTime(file);
                            }
                        }
                        break;
                    default:
                        dateTaken = File.GetLastWriteTime(file);
                        break;
                }
                var folderName = GetFolderName(dateTaken);
                var formattableString = $"{ResultFolder}\\{folderName}";
                Directory.CreateDirectory(formattableString);
                var newFileName = $"{Guid.NewGuid():N}{extension}";
                var destination = Path.Combine(formattableString, newFileName);
                File.Copy(file, destination);
                i++;
            }
        }

        private static string GetFolderName(DateTime dateTime)
        {
            return string.Format(FolderFormat, dateTime.Year, dateTime.ToString("MM"));
        }
    }
}