using CsvHelper.Configuration;
using prospect_scraper_mddb_2022.DTOs;

namespace prospect_scraper_mddb_2022.Maps
{
    public sealed class SchoolRankingMap : ClassMap<SchoolRanking>
    {
        public SchoolRankingMap()
        {
            Map(m => m.School).Index(0).Name("School");
            Map(m => m.RankingDateString).Index(1).Name("RankingDateString");
            Map(m => m.State).Index(2).Name("Projection");
            Map(m => m.Conference).Index(3).Name("Conference");
            Map(m => m.ProjectedPoints).Index(4).Name("ProjectedPoints");
            Map(m => m.ProspectCount).Index(5).Name("ProspectCount");
        }
    }
}