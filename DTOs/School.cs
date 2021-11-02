namespace prospect_scraper_mddb_2022.DTOs
{
    public class School
    {
        public string schoolName;
        public string conference;
        public string state;

        public School () {}
        
        public School (string schoolName, string conference, string state)
        {
            this.schoolName = schoolName;
            this.conference = conference;
            this.state = state;
        }

        public static string CheckSchool(string schoolName)
        {
            schoolName = schoolName switch
            {
                _ => schoolName,
            };
            return schoolName;
        }
    }
}