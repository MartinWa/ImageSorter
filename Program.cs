using System.Globalization;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.FileSystem;
using MetadataExtractor.Formats.FileType;
using MetadataExtractor.Formats.QuickTime;

const string OriginalFolder = @"C:\Users\marti\git\test";
const string ResultFolder = @"C:\Users\marti\git\sorted";

var files = Directory.EnumerateFiles(OriginalFolder, "*.*", SearchOption.AllDirectories).ToArray();
var i = 0;
var total = files.Length;
Console.WriteLine($"Copying from {OriginalFolder} to {ResultFolder} - {total} files");
foreach (var file in files)
{
    if (i % 100 == 0)
    {
        Console.WriteLine($"Copying file {i + 1}/{total}");
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
    var dateTimeCreated = metadata == null ? null : GetCreatedDateTimeFromMetadata(metadata, file);
    string destination;
    if (dateTimeCreated == null)
    {
        Console.WriteLine($"Could not find date from file {file}");
        var errorFolder = $"{ResultFolder}\\errors";
        Directory.CreateDirectory(errorFolder);
        var errorFileName = CreateErrorFileName(file);
        destination = Path.Combine(errorFolder, errorFileName);
    }
    else
    {
        var extension = GetCorrectedExtension(metadata, file);
        var folderName = CreateFolderName(dateTimeCreated.Value);
        var newFileName = CreateFileName(dateTimeCreated.Value, extension);
        destination = Path.Combine(folderName, newFileName);
        Directory.CreateDirectory(folderName);
    }
    File.Copy(file, destination);
    i++;

}

string GetCorrectedExtension(IReadOnlyList<MetadataExtractor.Directory>? metadata, string file)
{
    var extension = Path.GetExtension(file)?.ToLower() ?? "";
    var expectedExtension = "";
    var fileType = metadata?.OfType<FileTypeDirectory>().FirstOrDefault();
    if (fileType != null)
    {
        expectedExtension = fileType?.GetDescription(FileTypeDirectory.TagExpectedFileNameExtension) ?? string.Empty;
    }
    if (expectedExtension != null)
    {
        return $".{expectedExtension}";
    }
    Console.WriteLine($"Could not find expected extension in medatada for file {file}");
    return extension;
}

DateTime? GetCreatedDateTimeFromMetadata(IReadOnlyList<MetadataExtractor.Directory> metadata, string file)
{
    try
    {
        DateTime dateTime;
        var exifSubIfdDirectory = metadata.OfType<ExifSubIfdDirectory>().FirstOrDefault();
        if (exifSubIfdDirectory != null)
        {
            string exifFormat = "yyyy:MM:dd HH:mm:ss";
            var exifDateString = exifSubIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);
            if (DateTime.TryParseExact(exifDateString, exifFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
            {
                return dateTime;
            }
            exifDateString = exifSubIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeDigitized);
            if (DateTime.TryParseExact(exifDateString, exifFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
            {
                return dateTime;
            }
        }

        var quickTimeMovieHeader = metadata.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault();
        if (quickTimeMovieHeader != null)
        {
            string quickTimeFormat = "ddd MMM dd HH:mm:ss yyyy";
            var createdString = quickTimeMovieHeader?.GetDescription(QuickTimeMovieHeaderDirectory.TagCreated) ?? string.Empty;
            if (DateTime.TryParseExact(createdString, quickTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
            {
                return dateTime;
            }
        }
        var fileType = metadata.OfType<FileMetadataDirectory>().FirstOrDefault();
        if (fileType != null)
        {
            var createdString = fileType?.GetDescription(FileMetadataDirectory.TagFileModifiedDate) ?? string.Empty;
            string fileDateFormat = "ddd MMM dd HH:mm:ss zzz yyyy";
            return DateTime.ParseExact(createdString, fileDateFormat, CultureInfo.InvariantCulture);
        }
        Console.WriteLine($"Unknown metadata format from file {file}");
        foreach (var directory in metadata)
        {
            Console.WriteLine($"{directory.Name}");
        }
        return null;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{ex.Message} from file {file}");
        return null;
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
    var month = dateTime.ToString("MM");
    return $"{ResultFolder}\\{dateTime.Year}-{month}";
}
