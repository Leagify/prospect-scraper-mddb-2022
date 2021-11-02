namespace prospect_scraper_mddb_2022.DTOs
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
        public int ProjectedPoints { get; set; }

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
            Rank = rank;
            Peak = peak;
            PlayerName = name;
            School = school;
            Position = pos;
            RankingDateString = dateString;
            Projection = projPick;
            ProjectedTeam = projTeam;
            State = state;
            Conference = conference;
            ProjectedPoints = int.Parse(projectedPoints);
        }
    }
}