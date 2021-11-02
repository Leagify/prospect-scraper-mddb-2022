using SharpConfig;

namespace prospect_scraper_mddb_2022.Extensions
{
    public static class SectionExtensions
    {
        public static string GetUrlToScrape(this Section pageSection, string scrapeYear)
        {
            return pageSection.Contains(scrapeYear + "Url") ?
                pageSection[scrapeYear + "Url"].StringValue :
                pageSection["UrlPattern"].StringValue.Replace("{year}", scrapeYear);
        }
    }
}