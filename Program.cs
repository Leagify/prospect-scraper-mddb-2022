using System;
using Spectre.Console;
using SharpConfig;
using CsvHelper;
using CsvHelper.Configuration;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Data;
using prospect_scraper_mddb_2022.DTOs;
using prospect_scraper_mddb_2022.Extensions;

namespace prospect_scraper_mddb_2022
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var scraperConfig = Configuration.LoadFromFile("scraper.conf");
            var pageSection = scraperConfig["Pages"];
            var generalSection = scraperConfig["General"];

            AnsiConsole
                .Status()
                .Start("Thinking...", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Star);
                    var webGet = new HtmlWeb
                    {
                        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:91.0) Gecko/20100101 Firefox/91.0"
                    };

                    string[] scrapeYears = generalSection["YearsToScrape"].StringValueArray;
                    foreach (string scrapeYear in scrapeYears)
                    {
                        string urlToScrape = pageSection.GetUrlToScrape(scrapeYear);
                        ScrapeYear(ctx, webGet, scrapeYear, urlToScrape);
                    }

                    ctx.Status("Done!");
                });
        }

        private static void ScrapeYear(StatusContext ctx, HtmlWeb webGet, string scrapeYear, string urlToScrape)
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
            EnsureExists(baseDirectory);

            // use CsvWriter to write bigBoardInfo to csv

            //This is the file name we are going to write.
            string bigBoardInfoFileName = Path.Combine(baseDirectory, $"{scrapeYear}BoardInfo.csv");

            Console.WriteLine("Creating Draft Info csv...");

            //Write projects to csv with date.
            WriteToCsvFile(bigBoardInfoFileName, infos);

            ctx.SpinnerStyle(Style.Parse("green"));

            AnsiConsole.MarkupLine("Doing some prospect work...");
            var prospects = FindProspects(bigBoard, today);


            // Update the status and spinner
            ctx.Status("Writing draft prospect CSV");
            
            string playerInfoDirectory = Path.Combine(baseDirectory, "players");
            EnsureExists(playerInfoDirectory);

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
            WriteToCsvFile(collectedRanksFileName, prospects);

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
            EnsureExists(schoolInfoDirectory);

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
            WriteToCsvFile(schoolInfoFileName, schoolInfos);

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
            EnsureExists(statesDirectory);

            string stateRankInfoFileName = Path.Combine(statesDirectory, $"{today}-top-states.csv");
            string stateInfoFileName = Path.Combine(baseDirectory, $"{scrapeYear}StateInfo.csv");

            //Write schools to csv with date.
            using (var writer = new StreamWriter(stateRankInfoFileName))
            using (var csv = new CsvWriter(writer, CultureInfo.CurrentCulture))
            {
                csv.WriteRecords(topStates);
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
            );
        }

        private static void WriteToCsvFile<T>(string fileName, IEnumerable<T> data)
        {
            var csvConfig = new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                HasHeaderRecord = false
            };
            using var stream = File.Open(fileName, FileMode.Append);
            using var writer = new StreamWriter(stream);
            using var csv = new CsvWriter(writer, csvConfig);
            csv.WriteRecords(data);
        }

        private static void EnsureExists(string directory)
        {
            if(!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        private static List<ProspectRanking> FindProspects(HtmlNodeCollection nodes, string todayString)
        {
            var prospectRankings = new List<ProspectRanking>();

            //read in CSV from info/RanksToProjectedPoints.csv
            var dt = new DataTable();
            using (var reader = new StreamReader(Path.Combine("info", "RanksToProjectedPoints.csv")))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                // Do any configuration to `CsvReader` before creating CsvDataReader.
                using (var dr = new CsvDataReader(csv))
                {
                    dt.Load(dr);
                }
            }
            // Transform datatable dt to dictionary
            var ranksToPoints = dt.AsEnumerable()
                    .ToDictionary<DataRow, string, string>(row => row.Field<string>(0),
                                                        row => row.Field<string>(1));
                        
            var dt2 = new DataTable();
            using (var reader = new StreamReader(Path.Combine("info", "SchoolStatesAndConferences.csv")))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                // Do any configuration to `CsvReader` before creating CsvDataReader.
                using (var dr = new CsvDataReader(csv))
                {
                    dt2.Load(dr);
                }
            }

            var schoolsToStatesAndConfs = dt2.AsEnumerable()
                    .ToDictionary(row => row.Field<string>(0),
                                  row => (row.Field<string>(1), row.Field<string>(2))
                                 );

            foreach(var node in nodes)
            {
                var pickContainer = node.Descendants().FirstOrDefault(n => n.HasClass("pick-container"));
                var playerContainer = node.Descendants().FirstOrDefault(n => n.HasClass("player-container"));
                var percentageContainer = node.Descendants().FirstOrDefault(n => n.HasClass("percentage-container"));
                string projectedDraftSpot = "";
                string projectedDraftTeam = "";
                string playerSchool = "";

                var actualPickStuff = pickContainer.FirstChild;
                string currentRank = actualPickStuff.FirstChild.InnerText;
                var peakRankHtml = actualPickStuff.LastChild; //Rank 1 is in the middle child, not the last child for some reason. Seems to l=only happen when actualPickStuff.LastChild has 3 children.
                string peakRank = peakRankHtml.ChildNodes[1].InnerText;  // this is inside a span, but I'm not sure if it's reliably the second element.
                var namePositionSchool = node.LastChild;
                string playerName = playerContainer.FirstChild.InnerText.Replace("&#39;", "'");
                string playerPosition = playerContainer.LastChild.FirstChild.InnerText.Replace("|", "").Trim();
                int afterPipeStringLength = playerContainer.LastChild.FirstChild.InnerText.Split("|")[1].Length;
                if (playerContainer.LastChild.ChildNodes.Count == 2 && afterPipeStringLength <= 2)
                {
                    playerSchool = playerContainer.LastChild.LastChild.InnerText.Replace("&amp;", "&");
                }
                else if (afterPipeStringLength > 2)
                {
                    playerSchool = playerContainer.LastChild.FirstChild.InnerText.Split("|")[1].Replace("&amp;", "&").Trim();
                }
                else
                {
                    playerSchool = playerSchool = playerContainer.LastChild.ChildNodes[1].InnerText.Replace("&amp;", "&");
                }
                if (percentageContainer != null)
                {
                    int percentageContainerChildNodeCount = percentageContainer.ChildNodes.Count;
                    if (percentageContainerChildNodeCount == 2)
                    {
                        //if projected draft spot starts with "Possible" then it's a general grade with no consensus.
                        projectedDraftSpot = percentageContainer.FirstChild.LastChild.InnerText.Replace("#", "").Replace(":", "");
                        projectedDraftTeam = percentageContainer.LastChild.InnerText;
                        if (projectedDraftTeam != "No Consensus Available")
                        {
                            string projectedDraftTeamHref = percentageContainer.LastChild.FirstChild.Attributes.FirstOrDefault().Value;
                            string[] hrefStrings = projectedDraftTeamHref.Split("/");
                            projectedDraftTeam = hrefStrings[hrefStrings.Length - 1].Replace("-", " ").ToUpper();
                        }
                    }
                }
                            
                //var ranking = new ProspectRanking();
                
                playerSchool = ConvertSchool(playerSchool);
                string leagifyPoints = ranksToPoints.GetValueOrDefault(currentRank, "1");

                (string schoolConference, string schoolState) = schoolsToStatesAndConfs.GetValueOrDefault(playerSchool, ("", ""));

                Console.WriteLine($"Player: {playerName} at rank {currentRank} from {playerSchool} playing {playerPosition} got up to peak rank {peakRank} with {leagifyPoints} possible points");
                
                var currentPlayer = new ProspectRanking(todayString, currentRank, peakRank, playerName, playerSchool, playerPosition, schoolState, schoolConference, leagifyPoints, projectedDraftSpot, projectedDraftTeam );
                prospectRankings.Add(currentPlayer);
            }

            return prospectRankings;
        }

        private static string ConvertSchool(string schoolName)
        {
            schoolName = schoolName switch
            {
                "Mississippi" => "Ole Miss",
                "Pittsburgh" => "Pitt",
                _ => schoolName,
            };
            return schoolName;
        }    
    }
}
