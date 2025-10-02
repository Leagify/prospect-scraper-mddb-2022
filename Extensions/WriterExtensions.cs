using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace prospect_scraper_mddb_2022.Extensions
{
    public static class WriterExtensions
    {
        public static void WriteToCsvFile<T>(this IEnumerable<T> data, string fileName)
        {
            var csvConfig = new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                HasHeaderRecord = false
            };
            using var stream = File.Open(fileName, FileMode.Create);
            using var writer = new StreamWriter(stream);
            using var csv = new CsvWriter(writer, csvConfig);
            csv.WriteRecords(data);
        }

        public static void EnsureExists(this string directory)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }
    }
}
