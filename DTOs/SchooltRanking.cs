namespace prospect_scraper_mddb_2022.DTOs
{
    public class SchoolRanking
    {
        public string RankingDateString { get; set; }
        public string School { get; set; }
        public string State { get; set; }
        public string Conference { get; set; }
        public string ProjectedPoints { get; set; }
        public string ProspectCount { get; set; }

        public SchoolRanking(
            string dateString, 
            string school,
            string state, 
            string conference, 
            string projectedPoints, 
            string prospectCount)
        {
            School = school;
            RankingDateString = dateString;
            State = state;
            Conference = conference;
            ProjectedPoints = projectedPoints;
            ProspectCount = prospectCount;
        }
    }
}