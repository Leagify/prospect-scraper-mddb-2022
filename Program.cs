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
            var scraperConfig = new Configuration();
            scraperConfig = Configuration.LoadFromFile("scraper.conf");
            var pageSection = scraperConfig["Pages"];

            AnsiConsole.Status()
            .Start("Thinking...", ctx => 
            {
                // Simulate some work
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
                Console.WriteLine(bigBoard.Count);
                findProspects(bigBoard);


                AnsiConsole.MarkupLine("Doing some work...");
                System.Threading.Thread.Sleep(1000);
                
                // Update the status and spinner
                ctx.Status("Thinking some more");
                ctx.Spinner(Spinner.Known.Star);
                ctx.SpinnerStyle(Style.Parse("green"));

                // Simulate some work
                AnsiConsole.MarkupLine("Doing some more work...");
                System.Threading.Thread.Sleep(2000);
            });

            // Give a rendered result to the terminal.
            AnsiConsole.Render(new BarChart()
            .Width(60)
            .Label("[green bold underline]Number of fruits[/]")
            .CenterLabel()
            .AddItem("Sample Team 1", 12, Color.Yellow)
            .AddItem("Sample Team 2", 54, Color.Green)
            .AddItem("Sample Team 3", 33, Color.Red));

        }

        public static void findProspects(HtmlNodeCollection nodes)
        {
            foreach(var node in nodes)
            {
                var actualPickStuff = node.FirstChild.FirstChild;asdfkmasdjn;flkamnsvm,zx c;vjknasdo;pv skj F: Ki
                string currentRank = actualPickStuff.FirstChild.InnerText;
                string peakRank = actualPickStuff.LastChild.LastChild.InnerText; //Rank 1 is in the middle child, not the last child for some reason. Seems to l=only happen when actualPickStuff.LastChild has 3 children.
                var namePositionSchool = node.LastChild;
                string playerName = namePositionSchool.FirstChild.FirstChild.InnerText.Replace("&#39;", "'");
                var playerPositionAndSchool = namePositionSchool.LastChild.InnerText.Split(" | ");
                string playerPosition = playerPositionAndSchool[0];
                string playerSchool = playerPositionAndSchool[1];                
                
                Console.WriteLine($"Player: {playerName} at rank {currentRank} from {playerSchool} playing {playerPosition} got up to peak rank {peakRank}");
            }
        }
    }
}
