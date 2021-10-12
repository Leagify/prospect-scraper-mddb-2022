namespace prospect_scraper_mddb_2022.DTOs
{
    public class SchoolRankInfo
    {
        //ScrapeDate,NumberOfSchools,TopSchool,TopProjectedPoints,ProspectCountForTopSchool
        public string ScrapeDate { get; set; }
        public int NumberOfSchools { get; set; }
        public string TopSchool { get; set; }
        public int TopProjectedPoints { get; set; }
        public int ProspectCountForTopSchool { get; set; }

        public SchoolRankInfo(string scrapeDate, int numberOfSchools, string topSchool, int topProjectedPoints, int prospectCountForTopSchool)
        {
            ScrapeDate = scrapeDate;
            NumberOfSchools = numberOfSchools;
            TopSchool = topSchool;
            TopProjectedPoints = topProjectedPoints;
            ProspectCountForTopSchool = prospectCountForTopSchool;
        }
    }
}
