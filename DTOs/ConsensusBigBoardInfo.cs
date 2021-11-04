namespace prospect_scraper_mddb_2022.DTOs
{
    public class ConsensusBigBoardInfo
    {
        public string ScrapeDate { get; set; }
        public int BigBoardsUsed { get; set; }
        public int MockDraftsUsed { get; set; }
        public int TeamBasedMockDraftsUsed { get; set; }
        public int ProspectCount { get; set; }
        public string LastUpdated { get; set; }

        public ConsensusBigBoardInfo(
            string scrapeDate, 
            int bigBoardsUsed, 
            int mockDraftsUsed, 
            int teamBasedMockDraftsUsed, 
            int prospectCount, 
            string lastUpdated)
        {
            ScrapeDate = scrapeDate;
            BigBoardsUsed = bigBoardsUsed;
            MockDraftsUsed = mockDraftsUsed;
            TeamBasedMockDraftsUsed = teamBasedMockDraftsUsed;
            ProspectCount = prospectCount;
            LastUpdated = lastUpdated;
        }
    }
}
