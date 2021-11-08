using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                _ => schoolName,
            };
            return schoolName;
        }
    }
}
