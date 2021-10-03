using System;
using Spectre.Console;
using SharpConfig;
using CsvHelper;
using CsvHelper.Configuration;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using prospect_scraper_mddb_2022.DTOs;
using prospect_scraper_mddb_2022.Implementation;

namespace prospect_scraper_mddb_2022
{
    internal class Program
    {
        private static CsvConfiguration _headerlessCsvConfiguration;

        internal Program()
        {
            _headerlessCsvConfiguration = new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                HasHeaderRecord = false,
            };
        }
        
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
                    string lastUpdated = draftInfo[0].ChildNodes[2].InnerText.Replace("Last Updated: ", "").Trim();
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
                    var infos = new List<ConsensusBigBoardInfo>
                    {
                        bigBoardInfo
                    };
                    
                    // use CsvWriter to write bigBoardInfo to csv
                    //This is the file name we are going to write.
                    string bigBoardInfoFileName = $"ranks{Path.DirectorySeparatorChar}{scrapeYear}{Path.DirectorySeparatorChar}{scrapeYear}BoardInfo.csv";

                    Console.WriteLine("Creating Draft Info csv...");
                    //Write projects to csv with date.
                    using (var stream = File.Open(bigBoardInfoFileName, FileMode.Append))
                    using (var writer = new StreamWriter(stream))
                    using (var csv = new CsvWriter(writer, _headerlessCsvConfiguration))
                    {
                        csv.WriteRecords(infos);
                    }

                    ctx.SpinnerStyle(Style.Parse("green"));

                    AnsiConsole.MarkupLine("Doing some prospect work...");
                    var prospectFinder = new ProspectFinder();
                    var prospects = prospectFinder.FindProspects(bigBoard, today);

                    // Update the status and spinner
                    ctx.Status("Writing draft prospect CSV");
                    string playerInfoFileName =
                        $"ranks{Path.DirectorySeparatorChar}{scrapeYear}{Path.DirectorySeparatorChar}players{Path.DirectorySeparatorChar}{today}-ranks.csv";

                    //Write projects to csv with date.
                    using (var writer = new StreamWriter(playerInfoFileName))
                    using (var csv = new CsvWriter(writer, CultureInfo.CurrentCulture))
                    {
                        csv.WriteRecords(prospects);
                    }

                    ctx.Status("Writing to collected ranks CSV");
                    //Write projects to csv with date.
                    WriteCollectedRanksToCsv(scrapeYear, prospects);
                });

            // Give a rendered result to the terminal.
            RenderResult(bigBoards, mockDrafts, teamMockDrafts);
        }

        private static void RenderResult(int bigBoards, int mockDrafts, int teamMockDrafts)
        {
            // Give a rendered result to the terminal.
            AnsiConsole.Render(new BarChart()
                .Width(60)
                .Label("[green bold underline]Number of sources[/]")
                .CenterLabel()
                .AddItem("Big Boards", bigBoards, Color.Yellow)
                .AddItem("Mock Drafts", mockDrafts, Color.Green)
                .AddItem("Team Mock Drafts", teamMockDrafts, Color.Red));
        }

        private static void WriteCollectedRanksToCsv(string scrapeYear, IEnumerable<ProspectRanking> prospects)
        {
            string collectedRanksFileName = $"ranks{Path.DirectorySeparatorChar}{scrapeYear}{Path.DirectorySeparatorChar}{scrapeYear}ranks.csv";
            
            //Write projects to csv with date.
            using var stream = File.Open(collectedRanksFileName, FileMode.Append);
            using var writer = new StreamWriter(stream);
            using var csv = new CsvWriter(writer, _headerlessCsvConfiguration);
            csv.WriteRecords(prospects);
        }
    }
}