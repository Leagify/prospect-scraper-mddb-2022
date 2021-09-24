using System;
using CsvHelper;
using CsvHelper.Configuration;

namespace prospect_scraper_mddb_2022
{

    public class SchoolRanking
    {
        public string RankingDateString { get; set; }
        public string School { get; set; }
        public string State { get; set; }
        public string Conference { get; set; }
        public string ProjectedPoints { get; set; }
        public string ProspectCount { get; set; }

        public SchoolRanking(string dateString, string school,  
                               string state, string conference, string projectedPoints, string prospectCount)
        {
            this.School = school;
            this.RankingDateString = dateString;
            this.State = state;
            this.Conference = conference;
            this.ProjectedPoints = projectedPoints;
            this.ProspectCount = prospectCount;
        }
    }

    public sealed class SchoolRankingMap : ClassMap<SchoolRanking>
    {
        public SchoolRankingMap()
        {
            Map(m => m.School).Index(0).Name("School");
            Map(m => m.RankingDateString).Index(1).Name("RankingDateString");
            Map(m => m.State).Index(2).Name("Projection");
            Map(m => m.Conference).Index(3).Name("Conference");
            Map(m => m.ProjectedPoints).Index(4).Name("ProjectedPoints");
            Map(m => m.ProspectCount).Index(5).Name("ProspectCount");
        }
    }
}