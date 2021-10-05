using CsvHelper.Configuration;

namespace prospect_scraper_mddb_2022.Maps
{
    public sealed class ConsensusBigBoardInfoMap : ClassMap<ConsensusBigBoardInfo>
    {
        public ConsensusBigBoardInfoMap()
        {
            Map(m => m.ScrapeDate).Index(0).Name("ScrapeDate");
            Map(m => m.BigBoardsUsed).Index(1).Name("BigBoardsUsed");
            Map(m => m.MockDraftsUsed).Index(2).Name("MockDraftsUsed");
            Map(m => m.TeamBasedMockDraftsUsed).Index(3).Name("TeamBasedMockDraftsUsed");
            Map(m => m.ProspectCount).Index(4).Name("ProspectCount");
            Map(m => m.LastUpdated).Index(5).Name("LastUpdated");
        }
    }
}