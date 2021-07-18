using System;
using CsvHelper;
using CsvHelper.Configuration;

namespace prospect_scraper_mddb_2022
{

    public class ConsensusBigBoardInfo
    {
        public string ScrapeDate { get; set; }
        public int BigBoardsUsed { get; set; }
        public int MockDraftsUsed { get; set; }
        public int TeamBasedMockDraftsUsed { get; set; }
        public int ProspectCount { get; set; }
        public string LastUpdated { get; set; }
        

        public ConsensusBigBoardInfo(string scrapeDate, int bigboardsUsed, int mockDraftsUsed, int teamBasedMockDraftsUsed, int prospectCount, string lastUpdated)
        {
            this.ScrapeDate = scrapeDate;
            this.BigBoardsUsed = bigboardsUsed;
            this.MockDraftsUsed = mockDraftsUsed;
            this.TeamBasedMockDraftsUsed = teamBasedMockDraftsUsed;
            this.ProspectCount = prospectCount;
            this.LastUpdated = lastUpdated;
        }

    }
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
