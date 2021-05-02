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
    }
}
