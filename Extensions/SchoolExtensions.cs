namespace prospect_scraper_mddb_2022.Extensions
{
    public static class SchoolExtensions
    {
        public static string ConvertSchool(this string schoolName)
        {
            schoolName = schoolName switch
            {
                "Mississippi" => "Ole Miss",
                "Pittsburgh" => "Pitt",
                "Nicholls" => "Nicholls State",
                "Missouri Western" => "Nicholls State",
                _ => schoolName,
            };
            return schoolName;
        }
    }
}
