using CsvHelper.Configuration;
using prospect_scraper_mddb_2022.DTOs;

namespace prospect_scraper_mddb_2022.Maps
{
    public sealed class RegionCsvMap : ClassMap<Region>
    {
        public RegionCsvMap()
        {
            Map(m => m.state).Name("State");
            Map(m => m.region).Name("Region");
        }
    }
}