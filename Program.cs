using System;
using Spectre.Console;
using SharpConfig;
using CsvHelper;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;


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
                var document = webGet.Load(pageSection["2022Url"].StringValue);
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
                string scrapeYear = generalSection["YearToScrape"].StringValue;
                var bigBoardInfoFileName = $"ranks{Path.DirectorySeparatorChar}{scrapeYear}{Path.DirectorySeparatorChar}{scrapeYear}BoardInfo.csv";

                Console.WriteLine("Creating csv...");
                
                
                //Write projects to csv with date.
                using (var stream = File.Open(bigBoardInfoFileName, FileMode.Append))
                using (var writer = new StreamWriter(stream))
                using (var csv = new CsvWriter(writer, CultureInfo.CurrentCulture))
                {
                    bool header = csv.Configuration.HasHeaderRecord;
                    csv.WriteRecords(infos);
                }







                var prospects = findProspects(bigBoard);


                AnsiConsole.MarkupLine("Doing some work...");
                System.Threading.Thread.Sleep(1000);
                
                // Update the status and spinner
                ctx.Status("Thinking some more");
                
                ctx.SpinnerStyle(Style.Parse("green"));

                // Simulate some work
                AnsiConsole.MarkupLine("Doing some more work...");
                System.Threading.Thread.Sleep(2000);
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

        public static List<ProspectRanking> findProspects(HtmlNodeCollection nodes)
        {
            List<ProspectRanking> prospectRankings = new List<ProspectRanking>();

            foreach(var node in nodes)
            {
                var pickContainer = node.Descendants().Where(n => n.HasClass("pick-container")).FirstOrDefault();
                var playerContainer = node.Descendants().Where(n => n.HasClass("player-container")).FirstOrDefault();
                var percentageContainer = node.Descendants().Where(n => n.HasClass("percentage-container")).FirstOrDefault();
                
                



                var actualPickStuff = pickContainer.FirstChild;
                string currentRank = actualPickStuff.FirstChild.InnerText;
                var peakRankHtml = actualPickStuff.LastChild; //Rank 1 is in the middle child, not the last child for some reason. Seems to l=only happen when actualPickStuff.LastChild has 3 children.
                string peakRank = peakRankHtml.ChildNodes[1].InnerText;  // this is inside a span, but I'm not sure if it's reliably the second element.
                var namePositionSchool = node.LastChild;
                string playerName = playerContainer.FirstChild.InnerText.Replace("&#39;", "'");
                string playerPosition = playerContainer.LastChild.FirstChild.InnerText.Replace("|", "").Trim();
                string playerSchool = playerContainer.LastChild.LastChild.InnerText.Replace("&amp;", "&");
                if (percentageContainer != null)
                {
                    int percentageContainerChildNodeCount = percentageContainer.ChildNodes.Count;
                    if (percentageContainerChildNodeCount == 2)
                    {
                        //if projected draft spot starts with "Possible" then it's a general grade with no consensus.
                        string projectedDraftSpot = percentageContainer.FirstChild.LastChild.InnerText.Replace("#", "").Replace(":", "");
                        string projectedDraftTeam = percentageContainer.LastChild.InnerText;
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
            }
            return prospectRankings;
        }

    
        
    }
}
