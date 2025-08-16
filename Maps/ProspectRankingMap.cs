using CsvHelper.Configuration;
using prospect_scraper_mddb_2022.DTOs;

namespace prospect_scraper_mddb_2022.Maps
{
    public sealed class ProspectRankingMap : ClassMap<ProspectRanking>
    {
        public ProspectRankingMap()
        {
            Map(m => m.Rank).Index(0).Name("Rank");
            Map(m => m.PlayerName).Index(1).Name("PlayerName");
            Map(m => m.School).Index(2).Name("School");
            Map(m => m.Position).Index(3).Name("Position");
            Map(m => m.Date).Index(4).Name("Date");
        }
    }
}
