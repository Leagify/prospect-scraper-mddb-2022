using System;
using CsvHelper;
using CsvHelper.Configuration;

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

        public ProspectRanking(string dateString, string rank, string peak, string name, string school, string pos, 
                               string state, string conference, string projectedPoints, string projPick = "", string projTeam = "")
        {
            this.Rank = rank;
            this.Peak = peak;
            this.PlayerName = name;
            this.School = school;
            this.Position = pos;
            this.RankingDateString = dateString;
            this.Projection = projPick;
            this.ProjectedTeam = projTeam;
            this.State = state;
            this.Conference = conference;
            this.ProjectedPoints = projectedPoints;
        }
    }

    public sealed class ProspectRankingMap : ClassMap<ProspectRanking>
    {
        public ProspectRankingMap()
        {
            Map(m => m.Rank).Index(0).Name("Rank");
            Map(m => m.Peak).Index(1).Name("Peak");
            Map(m => m.PlayerName).Index(2).Name("PlayerName");
            Map(m => m.School).Index(3).Name("School");
            Map(m => m.Position).Index(4).Name("Position");
            Map(m => m.RankingDateString).Index(5).Name("RankingDateString");
            Map(m => m.Projection).Index(6).Name("Projection");
            Map(m => m.ProjectedTeam).Index(7).Name("ProjectedTeam");
            Map(m => m.State).Index(8).Name("Projection");
            Map(m => m.Conference).Index(9).Name("Conference");
            Map(m => m.ProjectedPoints).Index(10).Name("ProjectedPoints");
        }
    }
}