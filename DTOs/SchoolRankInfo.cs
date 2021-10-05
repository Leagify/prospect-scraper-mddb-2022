namespace prospect_scraper_mddb_2022.DTOs
{
    public class SchoolRankInfo
    {
        //ScrapeDate,NumberOfSchools,TopSchool,TopProjectedPoints,ProspectCountForTopSchool
        public int NumberOfSchools { get; set; }
        public int ProspectCountForTopSchool { get; set; }
        public int TopProjectedPoints { get; set; }
        public string ScrapeDate { get; set; }
        public string TopSchool { get; set; }

        public SchoolRankInfo(string scrapeDate, int numberOfSchools, string topSchool, int topProjectedPoints, int prospectCountForTopSchool)
        {
            NumberOfSchools = numberOfSchools;
            ProspectCountForTopSchool = prospectCountForTopSchool;
            ScrapeDate = scrapeDate;
            TopProjectedPoints = topProjectedPoints;
            TopSchool = topSchool;
        }

        public SchoolRankInfo()
        {
        }
    }
}
