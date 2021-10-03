namespace prospect_scraper_mddb_2022.Extensions
{
    public static class SchoolExtensions
    {
        public static string CheckSchool(this School school)
        {
            school.schoolName = school.schoolName switch
            {
                _ => school.schoolName,
            };
            return school.schoolName;
        }

        public static string ConvertSchool(this string schoolName)
        {
            schoolName = schoolName switch
            {
                "Mississippi" => "Ole Miss",
                "Pittsburgh" => "Pitt",
                _ => schoolName,
            };
            return schoolName;
        }
    }
}