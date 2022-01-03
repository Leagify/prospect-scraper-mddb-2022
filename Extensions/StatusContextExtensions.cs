using CsvHelper;
using HtmlAgilityPack;
using prospect_scraper_mddb_2022.DTOs;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace prospect_scraper_mddb_2022.Extensions
{
    public static class StatusContextExtensions
    {
        public static void ScrapeYear(this StatusContext ctx, HtmlWeb webGet, string scrapeYear, string urlToScrape)
        {
            int schools = 0,
                states = 0;

            var document = webGet.Load(urlToScrape);
            // This is still messy from debugging the different values.  It should be optimized.
            var dn = document.DocumentNode;
            // https://html-agility-pack.net/select-nodes
            // 2022 NFL Draft was compiled using 1 Big Board(s), 14 1st RoundMock Draft(s), and 0 Team BasedMock Draft(s). 
            // /html/body/div.container/div.consensus-mock-container/ul/li

            var bigBoard = dn.SelectNodes("//div[contains(@class, 'consensus-mock-container')]/ul/li");
            var draftInfo = dn.SelectNodes("//div[contains(@class, 'list-title')]");
            string lastUpdated = draftInfo[0].ChildNodes[2].InnerText.Replace("Last Updated: ", "").Trim();
            var boardCountContainer = draftInfo[0].ChildNodes[1];
            //var bigboardsUsed = dn.SelectNodes("/html[1]/body[1]/div[1]/div[2]/div[2]/p[1]/span[1]");
            //var mockDraftsUsed = dn.SelectNodes("/html[1]/body[1]/div[1]/div[2]/div[2]/p[1]/span[2]");
            //var teamBasedMockDraftsUsed = dn.SelectNodes("/html[1]/body[1]/div[1]/div[2]/div[2]/p[1]/span[3]");
            //bool bigBoardParsed = int.TryParse(bigboardsUsed[0].InnerText, out bigBoards);
            //bool mockDraftParsed = int.TryParse(mockDraftsUsed[0].InnerText, out mockDrafts);
            //bool teamMockDraftParsed = int.TryParse(teamBasedMockDraftsUsed[0].InnerText, out teamMockDrafts);
            bool bigBoardParsed = int.TryParse(boardCountContainer.ChildNodes[1].InnerText, out int bigBoards);
            bool mockDraftParsed = int.TryParse(boardCountContainer.ChildNodes[4].InnerText, out int mockDrafts);
            bool teamMockDraftParsed = int.TryParse(boardCountContainer.ChildNodes[7].InnerText, out int teamMockDrafts);

            Console.WriteLine("Big Board count: " + bigBoards);
            Console.WriteLine("Mock Draft count: " + mockDrafts);
            Console.WriteLine("Team Mock count: " + teamMockDrafts);
            Console.WriteLine("Prospect count: " + bigBoard.Count);

            // Get today's date in the format of yyyy-mm-dd
            string today = DateTime.Now.ToString("yyyy-MM-dd");

            // Create a ConsensusBigBoardInfo object from the parsed values.
            var bigBoardInfo = new ConsensusBigBoardInfo(today, bigBoards, mockDrafts, teamMockDrafts, bigBoard.Count, lastUpdated);
            var infos = new List<ConsensusBigBoardInfo>
            {
                bigBoardInfo
            };

            string baseDirectory = Path.Join("ranks", scrapeYear);
            baseDirectory.EnsureExists();

            // use CsvWriter to write bigBoardInfo to csv

            //This is the file name we are going to write.
            string bigBoardInfoFileName = Path.Combine(baseDirectory, $"{scrapeYear}BoardInfo.csv");

            Console.WriteLine("Creating Draft Info csv...");

            //Write projects to csv with date.
            infos.WriteToCsvFile(bigBoardInfoFileName);

            ctx.SpinnerStyle(Style.Parse("green"));

            AnsiConsole.MarkupLine("Doing some prospect work...");
            var prospects = bigBoard.FindProspects(today);

            // Update the status and spinner
            ctx.Status("Writing draft prospect CSV");

            string playerInfoDirectory = Path.Combine(baseDirectory, "players");
            playerInfoDirectory.EnsureExists();

            string playerInfoFileName = Path.Combine(playerInfoDirectory, $"{today}-ranks.csv");
            string collectedRanksFileName = Path.Combine(baseDirectory, $"{scrapeYear}ranks.csv");

            //Write projects to csv with date.
            using (var writer = new StreamWriter(playerInfoFileName))
            using (var csv = new CsvWriter(writer, CultureInfo.CurrentCulture))
            {
                csv.WriteRecords(prospects);
            }

            ctx.Status("Writing to collected ranks CSV");
            //Write projects to csv with date.
            prospects.WriteToCsvFile(collectedRanksFileName);

            // OK, I'm going to use LINQ to sort the top schools by points, then by number of prospects.
            // The output I want here is: Rank, School, Conference, Points, Number of Prospects
            ctx.Status($"Putting together top schools....");

            var topSchools = prospects.GroupBy(x => x.School)
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
            schools = topSchools.Count;

            // Chatty output to console.  It's messy but informative.
            Console.WriteLine("\nTop Schools.....");
            foreach (var school in topSchools)
            {
                Console.WriteLine($"{school.School} - {school.Conference} - {school.ProjectedPoints} - {school.NumberOfProspects}");
            }

            // Update the status and spinner
            ctx.Status("Writing Top Schools CSV");

            // Now, write these top schools to a CSV file in the schools directory for the year in question.
            // The file should be named with the date, then top-schools.csv, such as 2021-10-03-top-schools.csv
            // Also, write one line to a file that says how many schools there are, as well as the date, similar to 2022BoardInfo.csv
            string schoolInfoDirectory = Path.Combine(baseDirectory, "schools");
            schoolInfoDirectory.EnsureExists();

            string schoolRankInfoFileName = Path.Combine(schoolInfoDirectory, $"{today}-top-schools.csv");
            string schoolInfoFileName = Path.Combine(baseDirectory, $"{scrapeYear}SchoolInfo.csv");

            //Write schools to csv with date.
            using (var writer = new StreamWriter(schoolRankInfoFileName))
            using (var csv = new CsvWriter(writer, CultureInfo.CurrentCulture))
            {
                csv.WriteRecords(topSchools);
            }

            //Add School Info

            // Create a School Info object from the parsed values.
            //ScrapeDate,NumberOfSchools,TopSchool,TopProjectedPoints,ProspectCountForTopSchool
            var topSchool = topSchools.First();
            var schoolBoardInfo = new SchoolRankInfo(today, topSchools.Count, topSchool.School, topSchool.ProjectedPoints, topSchool.NumberOfProspects);
            var schoolInfos = new List<SchoolRankInfo>
            {
                schoolBoardInfo
            };

            //Write projects to csv with date.
            schoolInfos.WriteToCsvFile(schoolInfoFileName);

            // Similar to the top schools, I want to sort the top states by points, then by number of prospects, then by number of schools.
            var topStates = prospects.GroupBy(x => x.State)
                .Select(x => new
                {
                    State = x.Key,
                    ProjectedPoints = x.Sum(y => y.ProjectedPoints),
                    NumberOfSchools = x.GroupBy(y => y.School).Count(), // Number of schools in the state.
                    NumberOfProspects = x.Count()
                })
                .OrderByDescending(x => x.ProjectedPoints)
                .ThenByDescending(x => x.NumberOfProspects)
                .ToList();
            // Chatty output to console.  It's messy but informative.
            Console.WriteLine("\nTop States.....");
            foreach (var state in topStates)
            {
                Console.WriteLine($"{state.State} - {state.ProjectedPoints} - {state.NumberOfSchools} - {state.NumberOfProspects}");
            }

            states = topStates.Count;

            // Now, write these top states to a CSV file in the states directory for the year in question.
            // The file should be named with the date, then top-states.csv, such as 2021-10-03-top-states.csv
            string statesDirectory = Path.Combine(baseDirectory, "states");
            statesDirectory.EnsureExists();

            string stateRankInfoFileName = Path.Combine(statesDirectory, $"{today}-top-states.csv");

            //Write schools to csv with date.
            using (var writer = new StreamWriter(stateRankInfoFileName))
            using (var csv = new CsvWriter(writer, CultureInfo.CurrentCulture))
            {
                csv.WriteRecords(topStates);
            }

            // Find prospects where state name is ""
            var emptyStates = prospects.Where(x => string.IsNullOrEmpty(x.State)).ToList();

            if (emptyStates.Count > 0)
            {
                foreach (var prospect in emptyStates)
                {
                    AnsiConsole.WriteLine($"Player missing state: {prospect.PlayerName} - {prospect.School} - {prospect.State}");
                }
            }

            // Give a rendered result to the terminal.
            AnsiConsole.Write(new BarChart()
            .Width(60)
            .Label("[green bold underline]Number of sources[/]")
            .CenterLabel()
            .AddItem("Big Boards", bigBoards, Color.Yellow)
            .AddItem("Mock Drafts", mockDrafts, Color.Green)
            .AddItem("Team Mock Drafts", teamMockDrafts, Color.Red)
            .AddItem("Schools", schools, Color.Blue)
            .AddItem("States", states, Color.Aqua)
            .AddItem("State mismatches", emptyStates.Count, Color.Orange1)
            );
        }
    }
}
