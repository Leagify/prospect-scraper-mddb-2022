using CsvHelper.Configuration;
using prospect_scraper_mddb_2022.DTOs;

namespace prospect_scraper_mddb_2022.Maps
{
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