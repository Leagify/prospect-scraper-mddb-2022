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
            ScrapeDate = scrapeDate;
            BigBoardsUsed = bigboardsUsed;
            MockDraftsUsed = mockDraftsUsed;
            TeamBasedMockDraftsUsed = teamBasedMockDraftsUsed;
            ProspectCount = prospectCount;
            LastUpdated = lastUpdated;
        }

    }
}
