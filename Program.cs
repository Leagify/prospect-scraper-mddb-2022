using HtmlAgilityPack;
using prospect_scraper_mddb_2022.Extensions;
using SharpConfig;
using Spectre.Console;
using System;

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

                    string dataSourceMode = scraperConfig.GetDataSourceMode();
                    string[] scrapeYears = generalSection["YearsToScrape"].StringValueArray;

                    if (dataSourceMode.Equals("CSV", StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Status("CSV mode enabled - processing CSV files...");

                        foreach (string scrapeYear in scrapeYears)
                        {
                            string[] csvFiles = scraperConfig.GetCsvFilesForYear(scrapeYear);

                            if (csvFiles.Length == 0)
                            {
                                AnsiConsole.MarkupLine($"[yellow]No CSV files found for year {scrapeYear}[/]");
                                continue;
                            }

                            AnsiConsole.MarkupLine($"[cyan]Processing {csvFiles.Length} CSV file(s) for year {scrapeYear}[/]");

                            foreach (string csvFile in csvFiles)
                            {
                                ctx.ProcessCsvFile(csvFile, scrapeYear, scraperConfig);
                            }
                        }
                    }
                    else
                    {
                        ctx.Status("Web scraping mode enabled...");
                        var webGet = new HtmlWeb
                        {
                            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:91.0) Gecko/20100101 Firefox/91.0"
                        };

                        foreach (string scrapeYear in scrapeYears)
                        {
                            string urlToScrape = pageSection.GetUrlToScrape(scrapeYear);
                            ctx.ScrapeYear(webGet, scrapeYear, urlToScrape);
                        }
                    }

                    ctx.Status("Done!");
                });
        }
    }
}
