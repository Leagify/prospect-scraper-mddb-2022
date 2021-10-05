namespace prospect_scraper_mddb_2022.DTOs
{
    public class Region
	{
		public string region;
		public string state;

		public Region () { }
		public Region (string state, string region)
		{
			this.region = region;
			this.state = state;
		}
	}
}