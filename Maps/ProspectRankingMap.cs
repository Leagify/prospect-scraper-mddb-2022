using CsvHelper.Configuration;
using prospect_scraper_mddb_2022.DTOs;

namespace prospect_scraper_mddb_2022.Maps
{
    public sealed class ProspectRankingMap : ClassMap<ProspectRanking>
    {
        public ProspectRankingMap()
        {
            Map(m => m.Rank).Index(0).Name("Rank");
            Map(m => m.Peak).Index(1).Name("Peak");
            Map(m => m.PlayerName).Index(2).Name("PlayerName");
            Map(m => m.School).Index(3).Name("School");
            Map(m => m.Position).Index(4).Name("Position");
            Map(m => m.RankingDateString).Index(5).Name("RankingDateString");
            Map(m => m.Projection).Index(6).Name("Projection");
            Map(m => m.ProjectedTeam).Index(7).Name("ProjectedTeam");
            Map(m => m.State).Index(8).Name("Projection");
            Map(m => m.Conference).Index(9).Name("Conference");
            Map(m => m.ProjectedPoints).Index(10).Name("ProjectedPoints");
        }
    }
}