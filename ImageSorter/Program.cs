﻿using System;
using System.Globalization;
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

        private static void Main()
        {
            var files = Directory.EnumerateFiles(OnedriveFolder, "*.*", SearchOption.AllDirectories).ToArray();
            var i = 0;
            var total = files.Length;
            foreach (var file in files)
            {
                if (i%100 == 0)
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
                        try
                        {
                        using (var exifReader = new ExifReader(file))
                        {
                            if (!exifReader.GetTagValue(ExifTags.DateTimeDigitized, out dateTaken))
                            {
                                Console.WriteLine($"Can not read EXIF from file {file}");
                                dateTaken = File.GetLastWriteTime(file);
                            }
                        }

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{ex.Message} from file {file}");
                            dateTaken = File.GetLastWriteTime(file);
                        }
                        break;
                    default:
                        dateTaken = File.GetLastWriteTime(file);
                        break;
                }
                var folderName = CreateFolderName(dateTaken);
                Directory.CreateDirectory(folderName);
                var newFileName = $"{Guid.NewGuid():N}{extension}";
                var destination = Path.Combine(folderName, newFileName);
                File.Copy(file, destination);
                i++;
            }
        }

        private static string CreateFolderName(DateTime dateTime)
        {
            var month = dateTime.ToString("MMMM", CultureInfo.CreateSpecificCulture("sv"));
            var monthFirstCap = FirstLetterToUpper(month);
            return $"{ResultFolder}\\{dateTime.Year}\\{monthFirstCap}";
        }

        private static string FirstLetterToUpper(string str)
        {
            if (str == null)
                return null;

            if (str.Length > 1)
                return char.ToUpper(str[0]) + str.Substring(1);

            return str.ToUpper();
        }
    }
}