using CsvHelper;
using prospect_scraper_mddb_2022.DTOs;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace prospect_scraper_mddb_2022.Extensions
{
    public static class StatusContextExtensions
    {
        public static void ScrapeYear(this StatusContext ctx, string htmlContent, string scrapeYear)
        {
            var match = Regex.Match(htmlContent, "data-react-props=\"([^\"]+)\"");

            if (!match.Success)
            {
                AnsiConsole.MarkupLine($"[bold red]Could not find react props for year {scrapeYear}. Skipping.[/]");
                return;
            }

            var encodedJson = match.Groups[1].Value;
            var reactPropsJson = HttpUtility.HtmlDecode(encodedJson);

            var reactProps = JsonConvert.DeserializeObject<ReactProps>(reactPropsJson);

            if (reactProps == null || reactProps.Mock == null)
            {
                AnsiConsole.MarkupLine($"[bold red]Failed to deserialize react props for year {scrapeYear}. Skipping.[/]");
                return;
            }

            DateTime timeUtc = DateTime.UtcNow;
            string today = "";
            try
            {
                TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
                DateTime cstTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, cstZone);
                today = cstTime.ToString("yyyy-MM-dd");
            }
            catch (TimeZoneNotFoundException)
            {
                today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            }
            catch (InvalidTimeZoneException)
            {
                today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            }

            var prospects = new List<ProspectRanking>();
            foreach (var selection in reactProps.Mock.Selections)
            {
                prospects.Add(new ProspectRanking
                {
                    Rank = selection.Pick.ToString(),
                    PlayerName = selection.Player.Name,
                    School = selection.Player.College.Name,
                    Position = selection.Player.Position,
                    Date = today
                });
            }

            string baseDirectory = Path.Join("ranks", scrapeYear);
            baseDirectory.EnsureExists();

            ctx.SpinnerStyle(Style.Parse("green"));
            AnsiConsole.MarkupLine("Doing some prospect work...");

            Spectre.Console.Table prospectTable = new Spectre.Console.Table();
            prospectTable.AddColumn("Player");
            prospectTable.AddColumn("Rank");
            prospectTable.AddColumn("School");
            prospectTable.AddColumn("Position");
            prospectTable.Border(TableBorder.Ascii);

            foreach (var dude in prospects)
            {
                prospectTable.AddRow(dude.PlayerName, dude.Rank, dude.School, dude.Position);
            }
            
            AnsiConsole.Write(prospectTable);

            ctx.Status("Writing draft prospect CSV");

            string playerInfoDirectory = Path.Combine(baseDirectory, "players");
            playerInfoDirectory.EnsureExists();

            string playerInfoFileName = Path.Combine(playerInfoDirectory, $"{today}-ranks.csv");
            string collectedRanksFileName = Path.Combine(baseDirectory, $"{scrapeYear}ranks.csv");

            using (var writer = new StreamWriter(playerInfoFileName))
            using (var csv = new CsvWriter(writer, CultureInfo.CurrentCulture))
            {
                csv.WriteRecords(prospects);
            }

            ctx.Status("Writing to collected ranks CSV");
            prospects.WriteToCsvFile(collectedRanksFileName);

            AnsiConsole.Write(new BarChart()
            .Width(120)
            .Label("[green bold underline]Total prospects in list[/]")
            .CenterLabel()
            .AddItem(":person: NFL Prospects :person:", prospects.Count, Color.Cyan1 )
            );
        }
    }
}
