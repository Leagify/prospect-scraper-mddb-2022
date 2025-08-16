using Newtonsoft.Json;

namespace prospect_scraper_mddb_2022.DTOs
{
    public class ReactProps
    {
        [JsonProperty("mock")]
        public Mock Mock { get; set; }
    }

    public class Mock
    {
        [JsonProperty("selections")]
        public Selection[] Selections { get; set; }

    }


    public class Selection
    {
        [JsonProperty("pick")]
        public int Pick { get; set; }

        [JsonProperty("player")]
        public Player Player { get; set; }
    }

    public class Player
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("position")]
        public string Position { get; set; }

        [JsonProperty("college")]
        public College College { get; set; }
    }

    public class College
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }

}
