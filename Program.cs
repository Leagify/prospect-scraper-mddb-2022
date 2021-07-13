using System;
using Spectre.Console;
using SharpConfig;
using CsvHelper;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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

            AnsiConsole.Status()
            .Start("Thinking...", ctx => 
            {
                ctx.Spinner(Spinner.Known.Star);
                var webGet = new HtmlWeb();
                webGet.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:89.0) Gecko/20100101 Firefox/89.0";
                var document = webGet.Load(pageSection["2022Url"].StringValue);
                // This is still messy from debugging the different values.  It should be optimized.
                var dn = document.DocumentNode;
                // https://html-agility-pack.net/select-nodes
                // 2022 NFL Draft was compiled using 1 Big Board(s), 14 1st RoundMock Draft(s), and 0 Team BasedMock Draft(s). 
                // /html/body/div.container/div.consensus-mock-container/ul/li
                
                var bigBoard = dn.SelectNodes("//div[contains(@class, 'consensus-mock-container')]/ul/li");
                var bigboardsUsed = dn.SelectNodes("/html[1]/body[1]/div[1]/div[2]/div[2]/p[1]/span[1]");
                var mockDraftsUsed = dn.SelectNodes("/html[1]/body[1]/div[1]/div[2]/div[2]/p[1]/span[2]");
                var teamBasedMockDraftsUsed = dn.SelectNodes("/html[1]/body[1]/div[1]/div[2]/div[2]/p[1]/span[3]");
                bool bigBoardParsed = int.TryParse(bigboardsUsed[0].InnerText, out bigBoards);
                bool mockDraftParsed = int.TryParse(mockDraftsUsed[0].InnerText, out mockDrafts);
                bool teamMockDraftParsed = int.TryParse(teamBasedMockDraftsUsed[0].InnerText, out teamMockDrafts);
                if (bigBoardParsed)
                {
                    Console.WriteLine("Big Board count: " + bigboardsUsed[0].InnerText);
                }
                if (mockDraftParsed)
                {
                    Console.WriteLine("Mock Draft count: " + mockDraftsUsed[0].InnerText);
                }
                if (teamMockDraftParsed)
                {
                    Console.WriteLine("Team Mock count: " + teamBasedMockDraftsUsed[0].InnerText);
                }
                
                Console.WriteLine("Prospect count: " + bigBoard.Count);
                findProspects(bigBoard);


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

        public static void findProspects(HtmlNodeCollection nodes)
        {
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
                    }

                }
                            
                
                Console.WriteLine($"Player: {playerName} at rank {currentRank} from {playerSchool} playing {playerPosition} got up to peak rank {peakRank}");
            }
        }

    
        
    }
}
