using System;
using System.IO;
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
            var files = Directory.GetFiles(OnedriveFolder, "*.jpg", SearchOption.AllDirectories);
            var i = 1;
            var total = files.Length;
            foreach (var file in files)
            {
                Console.WriteLine($"Copying file {i}/{total} - {file}");
                using (var exifReader = new ExifReader(file))
                {
                    DateTime datePictureTaken;
                    if (!exifReader.GetTagValue(ExifTags.DateTimeDigitized, out datePictureTaken))
                    {
                        Console.WriteLine("Can not read EXIF from file");
                        continue;
                    }
                    var folderName = GetFolderName(datePictureTaken);
                    var formattableString = $"{ResultFolder}\\{folderName}";
                    Directory.CreateDirectory(formattableString);
                    var newFileName = $"{Guid.NewGuid():N}.jpg";
                    var destination = Path.Combine(formattableString, newFileName);
                    File.Copy(file, destination);
                }
            i++;
            }
        }

        private static string GetFolderName(DateTime dateTime)
        {
            return string.Format(FolderFormat, dateTime.Year, dateTime.ToString("MM"));
        }
    }
}