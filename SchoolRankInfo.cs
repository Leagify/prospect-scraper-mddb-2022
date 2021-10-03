using System;
using CsvHelper;
using CsvHelper.Configuration;

namespace prospect_scraper_mddb_2022
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
    public sealed class SchoolRankInfoMap : ClassMap<SchoolRankInfo>
    {
        public SchoolRankInfoMap()
        {
            Map(m => m.ScrapeDate).Index(0).Name("ScrapeDate");
            Map(m => m.NumberOfSchools).Index(1).Name("NumberOfSchools");
            Map(m => m.TopSchool).Index(2).Name("TopSchool");
            Map(m => m.TopProjectedPoints).Index(3).Name("TopProjectedPoints");
            Map(m => m.ProspectCountForTopSchool).Index(4).Name("ProspectCountForTopSchool");
        }
    }
}
