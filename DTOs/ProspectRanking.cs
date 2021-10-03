namespace prospect_scraper_mddb_2022
{
    public class ProspectRanking
    {
        public string Rank { get; set; }
        public string Peak { get; set; }
        public string PlayerName { get; set; }
        public string School { get; set; }
        public string Position { get; set; }
        public string RankingDateString { get; set; }
        public string Projection { get; set; }
        public string ProjectedTeam { get; set; }
        public string State { get; set; }
        public string Conference { get; set; }
        public string ProjectedPoints { get; set; }

        public ProspectRanking(
            string dateString, 
            string rank, 
            string peak, 
            string name, 
            string school, 
            string pos, 
            string state, 
            string conference, 
            string projectedPoints, 
            string projPick = "", 
            string projTeam = "")
        {
            Conference = conference;
            Peak = peak;
            PlayerName = name;
            Position = pos;
            ProjectedPoints = projectedPoints;
            ProjectedTeam = projTeam;
            Projection = projPick;
            Rank = rank;
            RankingDateString = dateString;
            School = school;
            State = state;
        }

        public ProspectRanking()
        {
        }
    }
}