using System.Globalization;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.FileSystem;
using MetadataExtractor.Formats.QuickTime;

//   private const string OnedriveFolder = @"F:\OneDrive\Bilder";
const string OriginalFolder = @"C:\Users\marti\git\test";
//   private const string ResultFolder = @"F:\sorted";
const string ResultFolder = @"C:\Users\marti\git\sorted";

var files = Directory.EnumerateFiles(OriginalFolder, "*.*", SearchOption.AllDirectories).ToArray();
var i = 0;
var total = files.Length;
Console.WriteLine($"Copying from {OriginalFolder} to {ResultFolder} - {total} files");
foreach (var file in files)
{
    if (i % 100 == 0)
    {
        Console.WriteLine($"Copying file {i + 1}/{total} - {file}");
    }
    IReadOnlyList<MetadataExtractor.Directory>? metadata = null;
    try

    {
        metadata = MetadataExtractor.ImageMetadataReader.ReadMetadata(file);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{ex.Message} from file {file}");
    }
    var result = metadata == null ? (null, MetadataFormat.None) : GetCreatedDateTimeFromMetadata(metadata, file);
    string destination;
    if (result.Item1 == null)
    {
        var errorFolder = $"{ResultFolder}\\errors";
        Directory.CreateDirectory(errorFolder);
        var errorFileName = CreateErrorFileName(file);
        destination = Path.Combine(errorFolder, errorFileName);
    }
    else
    {
        var extension = GetCorrectedExtension(result.Item2, file);
        var folderName = CreateFolderName(result.Item1.Value);
        var newFileName = CreateFileName(result.Item1.Value, extension);
        destination = Path.Combine(folderName, newFileName);
        Directory.CreateDirectory(folderName);
    }
    File.Copy(file, destination);
    i++;

}

string GetCorrectedExtension(MetadataFormat metadataFormat, string file)
{
    var extension = Path.GetExtension(file)?.ToLower() ?? "";
    switch (metadataFormat)
    {
        case MetadataFormat.Exif:
            if (extension != ".jpg" && extension != ".jpeg")
            {
                Console.WriteLine($"Correcting extension on {file} to jpg");
            }
            return ".jpg";
        case MetadataFormat.QuickTime:
            if (extension != ".mov")
            {
                Console.WriteLine($"Correcting extension on {file} to mov");
            }
            return ".mov";
        default:
            return extension;
    }

}

(DateTime?, MetadataFormat) GetCreatedDateTimeFromMetadata(IReadOnlyList<MetadataExtractor.Directory> metadata, string file)
{
    try
    {

        var exifSubIfdDirectory = metadata.OfType<ExifSubIfdDirectory>().FirstOrDefault();
        if (exifSubIfdDirectory != null)
        {
            var dateTimeOriginalString = exifSubIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal) ?? "";
            if (dateTimeOriginalString == "    :  :     :  :  ")
            {
                dateTimeOriginalString = exifSubIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeDigitized) ?? "";
            }
            string exifFormat = "yyyy:MM:dd HH:mm:ss";
            return (DateTime.ParseExact(dateTimeOriginalString, exifFormat, CultureInfo.InvariantCulture), MetadataFormat.Exif);
        }
        var quickTimeMovieHeader = metadata.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault();
        if (quickTimeMovieHeader != null)
        {
            var createdString = quickTimeMovieHeader?.GetDescription(QuickTimeMovieHeaderDirectory.TagCreated) ?? string.Empty;
            string quickTimeFormat = "ddd MMM dd HH:mm:ss yyyy";
            return (DateTime.ParseExact(createdString, quickTimeFormat, CultureInfo.InvariantCulture), MetadataFormat.QuickTime);
        }
        var fileType = metadata.OfType<FileMetadataDirectory>().FirstOrDefault();
        if (fileType != null)
        {
            var createdString = fileType?.GetDescription(FileMetadataDirectory.TagFileModifiedDate) ?? string.Empty;
            string fileDateFormat = "ddd MMM dd HH:mm:ss zzz yyyy";
            return (DateTime.ParseExact(createdString, fileDateFormat, CultureInfo.InvariantCulture), MetadataFormat.QuickTime);
        }
        Console.WriteLine($"Unknown metadata format from file {file}");
        foreach (var directory in metadata)
        {
            Console.WriteLine($"{directory.Name}");
        }
        return (null, MetadataFormat.None);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{ex.Message} from file {file}");
        return (null, MetadataFormat.None);
    }
}

string CreateFileName(DateTime dateTime, string extension)
{
    var day = dateTime.ToString("yyyy-MM-dd_HH-mm");
    var guid = Guid.NewGuid().ToString("N").Substring(0, 6);
    return $"{day}_{guid}{extension}";
}

string CreateErrorFileName(string file)
{
    var fileName = Path.GetFileName(file);
    var guid = Guid.NewGuid().ToString("N").Substring(0, 6);
    return $"{guid}{fileName}";
}

string CreateFolderName(DateTime dateTime)
{
    //var month = dateTime.ToString("MMMM", CultureInfo.CreateSpecificCulture("sv"));
    //var monthFirstCap = FirstLetterToUpper(month);
    var month = dateTime.ToString("MM");
    return $"{ResultFolder}\\{dateTime.Year}-{month}";
}
