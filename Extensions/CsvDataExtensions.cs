using CsvHelper;
using HtmlAgilityPack;
using prospect_scraper_mddb_2022.DTOs;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;

namespace prospect_scraper_mddb_2022.Extensions
{
    public static class CsvDataExtensions
    {
        public static void ProcessCsvFile(this StatusContext ctx, string csvFilePath, string scrapeYear)
        {
            var csvDate = SectionExtensions.ExtractDateFromFilename(csvFilePath);
            string dateString = csvDate.ToString("yyyy-MM-dd");

            ctx.Status($"Processing CSV: {Path.GetFileName(csvFilePath)}");

            var prospects = LoadProspectsFromCsv(csvFilePath, dateString);

            // Create BoardInfo with CSV-specific metadata
            var bigBoardInfo = new ConsensusBigBoardInfo(
                dateString,
                0,  // BigBoardsUsed
                0,  // MockDraftsUsed
                0,  // TeamBasedMockDraftsUsed
                prospects.Count,
                "CSV"  // LastUpdated
            );

            var infos = new List<ConsensusBigBoardInfo> { bigBoardInfo };

            string baseDirectory = Path.Join("ranks", scrapeYear);
            baseDirectory.EnsureExists();

            // Write BoardInfo
            string bigBoardInfoFileName = Path.Combine(baseDirectory, $"{scrapeYear}BoardInfo.csv");
            infos.WriteToCsvFile(bigBoardInfoFileName);

            ctx.SpinnerStyle(Style.Parse("green"));
            AnsiConsole.MarkupLine("Processing prospect data from CSV...");

            // Write prospect rankings
            string playerInfoFileName = Path.Combine(baseDirectory, $"ProspectRankings{dateString}.csv");
            prospects.WriteToCsvFile(playerInfoFileName);

            // Generate school statistics
            var topSchools = prospects
                .GroupBy(x => x.School)
                .Select(x => new
                {
                    School = x.Key,
                    Conference = x.First().Conference,
                    ProjectedPoints = x.Sum(y => y.ProjectedPoints),
                    NumberOfProspects = x.Count()
                })
                .OrderByDescending(x => x.ProjectedPoints)
                .ThenByDescending(x => x.NumberOfProspects)
                .ToList();

            // Generate state statistics
            var topStates = prospects.GroupBy(x => x.State)
                .Select(x => new
                {
                    State = x.Key,
                    ProjectedPoints = x.Sum(y => y.ProjectedPoints),
                    NumberOfSchools = x.GroupBy(y => y.School).Count(),
                    NumberOfProspects = x.Count()
                })
                .OrderByDescending(x => x.ProjectedPoints)
                .ThenByDescending(x => x.NumberOfProspects)
                .ToList();

            // Create school info
            var topSchool = topSchools.First();
            var schoolBoardInfo = new SchoolRankInfo(dateString, topSchools.Count, topSchool.School, topSchool.ProjectedPoints, topSchool.NumberOfProspects);
            var schoolInfos = new List<SchoolRankInfo> { schoolBoardInfo };

            // Write files
            string schoolRankingFileName = Path.Combine(baseDirectory, $"SchoolRankings{dateString}.csv");
            string schoolRankInfoFileName = Path.Combine(baseDirectory, $"SchoolRankInfo{dateString}.csv");

            // Note: topSchools is anonymous type, need to create proper objects or use dynamic writing
            using (var writer = new StreamWriter(schoolRankingFileName))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(topSchools);
            }

            schoolInfos.WriteToCsvFile(schoolRankInfoFileName);

            // Move processed CSV file to processed subfolder
            MoveToProcessedFolder(csvFilePath);

            ctx.Status("CSV processing complete!");
        }

        private static List<ProspectRanking> LoadProspectsFromCsv(string csvFilePath, string dateString)
        {
            var prospects = new List<ProspectRanking>();

            // Load ranking points lookup
            var ranksToPoints = LoadRanksToPoints();

            // Load school states and conferences lookup
            var schoolsToStatesAndConfs = LoadSchoolStatesAndConferences();

            using var reader = new StreamReader(csvFilePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Read();
            csv.ReadHeader();

            while (csv.Read())
            {
                string rank = csv.GetField("Rank");
                string playerName = csv.GetField("Player Name");
                string position = csv.GetField("Position");
                string college = csv.GetField("College");

                // Convert school name using existing extension
                college = college.ConvertSchool();

                // Look up state and conference
                (string schoolConference, string schoolState) = schoolsToStatesAndConfs.GetValueOrDefault(college, ("", ""));

                // Get projected points
                string projectedPoints = ranksToPoints.GetValueOrDefault(rank, "1");

                // Create prospect ranking (Peak = Rank for CSV data)
                var prospect = new ProspectRanking(
                    dateString,
                    rank,
                    rank,  // Peak = current rank for CSV data
                    playerName,
                    college,
                    position,
                    schoolState,
                    schoolConference,
                    projectedPoints,
                    "",  // No projected draft spot from CSV
                    ""   // No projected team from CSV
                );

                prospects.Add(prospect);
            }

            return prospects;
        }

        private static Dictionary<string, string> LoadRanksToPoints()
        {
            var dt = new DataTable();
            using var reader = new StreamReader(Path.Combine("info", "RanksToProjectedPoints.csv"));
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            using var dr = new CsvDataReader(csv);
            dt.Load(dr);

            return dt.AsEnumerable()
                .ToDictionary<DataRow, string, string>(
                    row => row.Field<string>(0),
                    row => row.Field<string>(1));
        }

        private static Dictionary<string, (string, string)> LoadSchoolStatesAndConferences()
        {
            var dt = new DataTable();
            using var reader = new StreamReader(Path.Combine("info", "SchoolStatesAndConferences.csv"));
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            using var dr = new CsvDataReader(csv);
            dt.Load(dr);

            return dt.AsEnumerable()
                .ToDictionary(
                    row => row.Field<string>(0),
                    row => (row.Field<string>(1), row.Field<string>(2)));
        }

        private static void MoveToProcessedFolder(string csvFilePath)
        {
            string directory = Path.GetDirectoryName(csvFilePath);
            string fileName = Path.GetFileName(csvFilePath);
            string processedDir = Path.Combine(directory, "processed");

            // Create processed directory if it doesn't exist
            if (!Directory.Exists(processedDir))
            {
                Directory.CreateDirectory(processedDir);
            }

            string destinationPath = Path.Combine(processedDir, fileName);

            // Move the file (this will overwrite if file already exists)
            File.Move(csvFilePath, destinationPath, true);
        }
    }
}