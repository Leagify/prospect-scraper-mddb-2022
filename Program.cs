using HtmlAgilityPack;
using prospect_scraper_mddb_2022.Extensions;
using SharpConfig;
using Spectre.Console;

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
                        var htmlDoc = webGet.Load(urlToScrape);
                        var htmlContent = htmlDoc.DocumentNode.OuterHtml;
                        ctx.ScrapeYear(htmlContent, scrapeYear);
                    }

                    ctx.Status("Done!");
                });
        }
    }
}
