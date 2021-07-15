using System;

namespace prospect_scraper_mddb_2022
{

    public class ProspectRanking
    {
        public int Rank { get; set; }
        public int Peak { get; set; }
        public string PlayerName { get; set; }
        public string School { get; set; }
        public string Position { get; set; }
        public DateTime RankingDate { get; set; }
        public string RankingDateString { get; set; }
        public string Projection { get; set; }
        public string ProjectedTeam { get; set; }

        public ProspectRanking(DateTime date, int rank, int peak, string name, string school, string pos, string projPick = "", string projTeam = "")
        {
            this.RankingDate = date;
            this.Rank = rank;
            this.Peak = peak;
            this.PlayerName = name;
            this.School = school;
            this.Position = pos;
            this.RankingDateString = date.ToString("yyyy-MM-dd");
            this.Projection = projPick;
            this.ProjectedTeam = projTeam;
        }
    }
}