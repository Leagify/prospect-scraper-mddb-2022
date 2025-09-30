using SharpConfig;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace prospect_scraper_mddb_2022.Extensions
{
    public static class SectionExtensions
    {
        public static string GetUrlToScrape(this Section pageSection, string scrapeYear)
        {
            return pageSection.Contains(scrapeYear + "Url") ?
                pageSection[scrapeYear + "Url"].StringValue :
                pageSection["UrlPattern"].StringValue.Replace("{year}", scrapeYear);
        }

        public static string GetDataSourceMode(this Configuration config)
        {
            return config.Contains("DataSource") && config["DataSource"].Contains("Mode") ?
                config["DataSource"]["Mode"].StringValue : "Web";
        }

        public static string GetCsvBasePath(this Configuration config)
        {
            return config.Contains("DataSource") && config["DataSource"].Contains("CsvBasePath") ?
                config["DataSource"]["CsvBasePath"].StringValue : "site-csvs";
        }

        public static int GetCsvRunCount(this Configuration config)
        {
            return config.Contains("DataSource") && config["DataSource"].Contains("CsvRunCount") ?
                config["DataSource"]["CsvRunCount"].IntValue : 1;
        }

        public static bool GetVerboseOutput(this Configuration config)
        {
            return config.Contains("DataSource") && config["DataSource"].Contains("VerboseOutput") ?
                config["DataSource"]["VerboseOutput"].BoolValue : false;
        }

        public static string[] GetCsvFilesForYear(this Configuration config, string year)
        {
            string basePath = config.GetCsvBasePath();
            string yearPath = Path.Combine(basePath, year);

            if (!Directory.Exists(yearPath))
                return new string[0];

            // Only get files from main directory, excluding processed subfolder
            var files = Directory.GetFiles(yearPath, $"consensus-big-board-{year}-*.csv", SearchOption.TopDirectoryOnly)
                .OrderBy(f => ExtractDateFromFilename(f))
                .ToArray();

            int runCount = config.GetCsvRunCount();
            // Take the oldest N files and return them in chronological order (oldest to newest)
            return files.Take(runCount).ToArray();
        }

        public static DateTime ExtractDateFromFilename(string filename)
        {
            var match = Regex.Match(Path.GetFileName(filename), @"consensus-big-board-\d{4}-(\d{8})\.csv");
            if (match.Success && DateTime.TryParseExact(match.Groups[1].Value, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime date))
            {
                return date;
            }
            return DateTime.MinValue;
        }
    }
}