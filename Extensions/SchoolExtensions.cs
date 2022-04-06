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
                "Lenoir-Rhyne" => "Lenoir–Rhyne",
                "CSU Pueblo" => "Colorado State–Pueblo",
                "UMass" => "Massachusetts",
                _ => schoolName,
            };
            return schoolName;
        }
    }
}
