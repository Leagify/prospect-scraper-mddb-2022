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


namespace prospect_scraper_mddb_2022
{
    class Program
    {
        static void Main(string[] args)
        {
            int bigBoards = 0;
            int mockDrafts = 0;
            int teamMockDrafts = 0;


            var scraperConfig = new Configuration();
            scraperConfig = Configuration.LoadFromFile("scraper.conf");
            var pageSection = scraperConfig["Pages"];
            var generalSection = scraperConfig["General"];

            AnsiConsole.Status()
            .Start("Thinking...", ctx => 
            {
                ctx.Spinner(Spinner.Known.Star);
                var webGet = new HtmlWeb();
                webGet.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:91.0) Gecko/20100101 Firefox/91.0";
                string scrapeYear = generalSection["YearToScrape"].StringValue;
                string urlToScrape = scrapeYear + "Url";
                var document = webGet.Load(pageSection[urlToScrape].StringValue);
                // This is still messy from debugging the different values.  It should be optimized.
                var dn = document.DocumentNode;
                // https://html-agility-pack.net/select-nodes
                // 2022 NFL Draft was compiled using 1 Big Board(s), 14 1st RoundMock Draft(s), and 0 Team BasedMock Draft(s). 
                // /html/body/div.container/div.consensus-mock-container/ul/li
                
                var bigBoard = dn.SelectNodes("//div[contains(@class, 'consensus-mock-container')]/ul/li");
                var draftInfo = dn.SelectNodes("//div[contains(@class, 'list-title')]");
                var lastUpdated = draftInfo[0].ChildNodes[2].InnerText.Replace("Last Updated: ", "").Trim();
                var boardCountContainer = draftInfo[0].ChildNodes[1];
                //var bigboardsUsed = dn.SelectNodes("/html[1]/body[1]/div[1]/div[2]/div[2]/p[1]/span[1]");
                //var mockDraftsUsed = dn.SelectNodes("/html[1]/body[1]/div[1]/div[2]/div[2]/p[1]/span[2]");
                //var teamBasedMockDraftsUsed = dn.SelectNodes("/html[1]/body[1]/div[1]/div[2]/div[2]/p[1]/span[3]");
                //bool bigBoardParsed = int.TryParse(bigboardsUsed[0].InnerText, out bigBoards);
                //bool mockDraftParsed = int.TryParse(mockDraftsUsed[0].InnerText, out mockDrafts);
                //bool teamMockDraftParsed = int.TryParse(teamBasedMockDraftsUsed[0].InnerText, out teamMockDrafts);
                bool bigBoardParsed = int.TryParse(boardCountContainer.ChildNodes[1].InnerText, out bigBoards);
                bool mockDraftParsed = int.TryParse(boardCountContainer.ChildNodes[4].InnerText, out mockDrafts);
                bool teamMockDraftParsed = int.TryParse(boardCountContainer.ChildNodes[7].InnerText, out teamMockDrafts);
                
                Console.WriteLine("Big Board count: " + bigBoards);
                Console.WriteLine("Mock Draft count: " + mockDrafts);
                Console.WriteLine("Team Mock count: " + teamMockDrafts);
                
                Console.WriteLine("Prospect count: " + bigBoard.Count);
                
                // Get today's date in the format of yyyy-mm-dd
                string today = DateTime.Now.ToString("yyyy-MM-dd");

                // Create a ConsesusBigBoardInfo object from the parsed values.
                var bigBoardInfo = new ConsensusBigBoardInfo(today, bigBoards, mockDrafts, teamMockDrafts, bigBoard.Count, lastUpdated);
                List<ConsensusBigBoardInfo> infos = new List<ConsensusBigBoardInfo>();
                infos.Add(bigBoardInfo);


                // use CsvWriter to write bigBoardInfo to csv
               
                //This is the file name we are going to write.
                var bigBoardInfoFileName = $"ranks{Path.DirectorySeparatorChar}{scrapeYear}{Path.DirectorySeparatorChar}{scrapeYear}BoardInfo.csv";

                Console.WriteLine("Creating Draft Info csv...");
                
                var consensusConfig = new CsvConfiguration(CultureInfo.CurrentCulture);
                consensusConfig.HasHeaderRecord =  false;
                //Write projects to csv with date.
                using (var stream = File.Open(bigBoardInfoFileName, FileMode.Append))
                using (var writer = new StreamWriter(stream))
                using (var csv = new CsvWriter(writer, consensusConfig))
                {
                    csv.WriteRecords(infos);
                }
            
                ctx.SpinnerStyle(Style.Parse("green"));

                AnsiConsole.MarkupLine("Doing some prospect work...");
                var prospects = findProspects(bigBoard, today);

                
                // Update the status and spinner
                ctx.Status("Writing draft prospect CSV");
                var playerInfoFileName = $"ranks{Path.DirectorySeparatorChar}{scrapeYear}{Path.DirectorySeparatorChar}players{Path.DirectorySeparatorChar}{today}-ranks.csv";
                var collectedRanksFileName = $"ranks{Path.DirectorySeparatorChar}{scrapeYear}{Path.DirectorySeparatorChar}{scrapeYear}ranks.csv";

                //Write projects to csv with date.
                using (var writer = new StreamWriter(playerInfoFileName))
                using (var csv = new CsvWriter(writer, CultureInfo.CurrentCulture))
                {
                    csv.WriteRecords(prospects);
                }
                
                ctx.Status("Writing to collected ranks CSV");
                var collectedRanksConfig = new CsvConfiguration(CultureInfo.CurrentCulture);
                collectedRanksConfig.HasHeaderRecord =  false;
                //Write projects to csv with date.
                using (var stream = File.Open(collectedRanksFileName, FileMode.Append))
                using (var writer = new StreamWriter(stream))
                using (var csv = new CsvWriter(writer, collectedRanksConfig))
                {
                    csv.WriteRecords(prospects);
                }

                
            });

            // Give a rendered result to the terminal.
            AnsiConsole.Render(new BarChart()
            .Width(60)
            .Label("[green bold underline]Number of sources[/]")
            .CenterLabel()
            .AddItem("Big Boards", bigBoards, Color.Yellow)
            .AddItem("Mock Drafts", mockDrafts, Color.Green)
            .AddItem("Team Mock Drafts", teamMockDrafts, Color.Red));

        }

        public static List<ProspectRanking> findProspects(HtmlNodeCollection nodes, string todayString)
        {
            List<ProspectRanking> prospectRankings = new List<ProspectRanking>();

            //read in CSV from info/RanksToProjectedPoints.csv
            var dt = new DataTable();
            using (var reader = new StreamReader($"info{Path.DirectorySeparatorChar}RanksToProjectedPoints.csv"))
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
            using (var reader = new StreamReader($"info{Path.DirectorySeparatorChar}SchoolStatesAndConferences.csv"))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                // Do any configuration to `CsvReader` before creating CsvDataReader.
                using (var dr = new CsvDataReader(csv))
                {		
                    dt2.Load(dr);
                }
            }

            var schoolsToStatesAndConfs = dt2.AsEnumerable()
                    .ToDictionary<DataRow, string, (string, string)>(row => row.Field<string>(0),
                                                        row => (row.Field<string>(1), row.Field<string>(2))
                                                        );




            foreach(var node in nodes)
            {
                var pickContainer = node.Descendants().Where(n => n.HasClass("pick-container")).FirstOrDefault();
                var playerContainer = node.Descendants().Where(n => n.HasClass("player-container")).FirstOrDefault();
                var percentageContainer = node.Descendants().Where(n => n.HasClass("percentage-container")).FirstOrDefault();
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
                            var hrefStrings = projectedDraftTeamHref.Split("/");
                            projectedDraftTeam = hrefStrings[hrefStrings.Length - 1].Replace("-", " ").ToUpper();
                        }
                    }

                }
                            
                //var ranking = new ProspectRanking();
                Console.WriteLine($"Player: {playerName} at rank {currentRank} from {playerSchool} playing {playerPosition} got up to peak rank {peakRank}");
                
                playerSchool = convertSchool(playerSchool);
                string schoolState = "";
                string schoolConference = "";
                string leagifyPoints = "";
                
                leagifyPoints = ranksToPoints[currentRank];
                schoolConference  = schoolsToStatesAndConfs[playerSchool].Item1;
                schoolState = schoolsToStatesAndConfs[playerSchool].Item2;

                var currentplayer = new ProspectRanking(todayString, currentRank, peakRank, playerName, playerSchool, playerPosition, schoolState, schoolConference, leagifyPoints, projectedDraftSpot, projectedDraftTeam );
                prospectRankings.Add(currentplayer);
            }



            return prospectRankings;
        }

        public static string convertSchool(string schoolName)
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
