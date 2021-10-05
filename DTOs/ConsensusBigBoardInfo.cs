namespace prospect_scraper_mddb_2022.DTOs
{
    public class ConsensusBigBoardInfo
    {
        public int BigBoardsUsed { get; set; }
        public int MockDraftsUsed { get; set; }
        public int ProspectCount { get; set; }
        public int TeamBasedMockDraftsUsed { get; set; }
        public string LastUpdated { get; set; }
        public string ScrapeDate { get; set; }

        public ConsensusBigBoardInfo(string scrapeDate, int bigBoardsUsed, int mockDraftsUsed, int teamBasedMockDraftsUsed, int prospectCount, string lastUpdated)
        {
            BigBoardsUsed = bigBoardsUsed;
            LastUpdated = lastUpdated;
            MockDraftsUsed = mockDraftsUsed;
            ProspectCount = prospectCount;
            ScrapeDate = scrapeDate;
            TeamBasedMockDraftsUsed = teamBasedMockDraftsUsed;
        }

        public ConsensusBigBoardInfo()
        {
            
        }
    }
}
