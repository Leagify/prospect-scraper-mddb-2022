using CsvHelper;
using HtmlAgilityPack;
using prospect_scraper_mddb_2022.DTOs;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Web;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace prospect_scraper_mddb_2022.Extensions
{
    public static class StatusContextExtensions
    {
        public static void ScrapeYear(this StatusContext ctx, HtmlWeb webGet, string scrapeYear, string urlToScrape)
        {
            int schools = 0,
                states = 0;

            HtmlDocument document = null;
            
            // Setup Chrome options with stealth settings to avoid detection
            var options = new ChromeOptions();
            options.AddArgument("--headless=new"); // Use new headless mode
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--disable-web-security");
            options.AddArgument("--disable-features=VizDisplayCompositor");
            options.AddArgument("--disable-extensions");
            options.AddArgument("--disable-plugins");
            options.AddArgument("--disable-images");
            // Keep JavaScript enabled for React component rendering
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36");
            
            // Additional stealth options
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalOption("useAutomationExtension", false);
            options.AddArgument("--disable-blink-features=AutomationControlled");
            
            // Add cleanup options to ensure Chrome processes terminate properly
            options.AddArgument("--disable-background-timer-throttling");
            options.AddArgument("--disable-backgrounding-occluded-windows");
            options.AddArgument("--disable-renderer-backgrounding");
            options.AddArgument("--force-device-scale-factor=1");

            // Retry logic for handling connection issues
            const int maxRetries = 3;
            Exception lastException = null;
            ChromeDriver driver = null;
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    driver = new ChromeDriver(options);
                    
                    ctx.Status($"Loading page (attempt {attempt}/{maxRetries}): {urlToScrape}");
                    
                    // Add some delay between retries
                    if (attempt > 1)
                    {
                        ctx.Status($"Waiting {attempt * 2} seconds before retry...");
                        System.Threading.Thread.Sleep(attempt * 2000);
                    }
                    
                    driver.Navigate().GoToUrl(urlToScrape);
                    
                    // Wait for the React component div to appear
                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(45));
                    wait.Until(d => d.FindElements(By.CssSelector("div[data-react-class='big_boards/Consensus']")).Count > 0);
                    
                    ctx.Status("Page loaded successfully, extracting JSON data...");
                    
                    // Get the page source after JavaScript has executed
                    var pageSource = driver.PageSource;
                    
                    // Load the page into HtmlAgilityPack to extract JSON data
                    document = new HtmlDocument();
                    document.LoadHtml(pageSource);
                    
                    // Explicitly close and quit the driver
                    driver?.Close();
                    driver?.Quit();
                    driver?.Dispose();
                    driver = null;
                    
                    // If we get here, success! Break out of retry loop
                    break;
                }
                catch (WebDriverTimeoutException ex)
                {
                    lastException = ex;
                    // Cleanup driver on failure
                    try { driver?.Close(); } catch { }
                    try { driver?.Quit(); } catch { }
                    try { driver?.Dispose(); } catch { }
                    driver = null;
                    
                    AnsiConsole.MarkupLine($"[yellow]Attempt {attempt} timed out waiting for React component to load.[/]");
                    if (attempt == maxRetries)
                    {
                        AnsiConsole.MarkupLine("[red]All attempts failed. The page structure may have changed.[/]");
                        throw;
                    }
                }
                catch (WebDriverException ex) when (ex.Message.Contains("connection") || ex.Message.Contains("handshake") || ex.Message.Contains("SSL"))
                {
                    lastException = ex;
                    // Cleanup driver on failure
                    try { driver?.Close(); } catch { }
                    try { driver?.Quit(); } catch { }
                    try { driver?.Dispose(); } catch { }
                    driver = null;
                    
                    AnsiConsole.MarkupLine($"[yellow]Attempt {attempt} failed with connection error: {ex.Message}[/]");
                    if (attempt == maxRetries)
                    {
                        AnsiConsole.MarkupLine("[red]All attempts failed due to connection issues. The website may be blocking automated requests.[/]");
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    // Cleanup driver on failure
                    try { driver?.Close(); } catch { }
                    try { driver?.Quit(); } catch { }
                    try { driver?.Dispose(); } catch { }
                    driver = null;
                    
                    AnsiConsole.MarkupLine($"[red]Attempt {attempt} failed with error: {ex.Message}[/]");
                    if (attempt == maxRetries)
                    {
                        throw;
                    }
                }
            }
            
            // Final cleanup - ensure no driver processes are left hanging
            try { driver?.Close(); } catch { }
            try { driver?.Quit(); } catch { }
            try { driver?.Dispose(); } catch { }
            
            // Force cleanup any remaining Chrome processes
            ctx.Status("Cleaning up Chrome processes...");
            try
            {
                var chromeProcesses = System.Diagnostics.Process.GetProcessesByName("chrome");
                var chromedriverProcesses = System.Diagnostics.Process.GetProcessesByName("chromedriver");
                
                foreach (var process in chromeProcesses.Concat(chromedriverProcesses))
                {
                    if (!process.HasExited)
                    {
                        try
                        {
                            process.Kill();
                            process.WaitForExit(5000);
                        }
                        catch { /* Ignore cleanup errors */ }
                    }
                    process.Dispose();
                }
            }
            catch { /* Ignore cleanup errors */ }

            // Extract JSON data from React component props
            var dn = document.DocumentNode;
            
            ctx.Status("Parsing JSON data from React component...");
            
            // Find the React component div and extract JSON props
            var reactDiv = dn.SelectSingleNode("//div[@data-react-class='big_boards/Consensus']");
            if (reactDiv == null)
            {
                AnsiConsole.MarkupLine("[red]Could not find React component div. The page structure may have changed.[/]");
                throw new InvalidOperationException("React component not found");
            }
            
            var jsonPropsAttribute = reactDiv.GetAttributeValue("data-react-props", "");
            if (string.IsNullOrEmpty(jsonPropsAttribute))
            {
                AnsiConsole.MarkupLine("[red]Could not find JSON props. The page structure may have changed.[/]");
                throw new InvalidOperationException("JSON props not found");
            }
            
            // Decode HTML entities and parse JSON
            var jsonProps = HttpUtility.HtmlDecode(jsonPropsAttribute);
            using var jsonDoc = JsonDocument.Parse(jsonProps);
            
            var root = jsonDoc.RootElement;
            var mockProperty = root.GetProperty("mock");
            var selectionsArray = mockProperty.GetProperty("selections");
            
            // Create a list to simulate the old HtmlNodeCollection for compatibility
            var prospects = new List<JsonElement>();
            foreach (var selection in selectionsArray.EnumerateArray())
            {
                prospects.Add(selection);
            }
            
            // For now, set default values for board counts (these might be available elsewhere in the JSON)
            int bigBoards = 1;  // Default, could be parsed from other parts of the page
            int mockDrafts = 10; // Default, could be parsed from other parts of the page  
            int teamMockDrafts = 0; // Default

            //Console.WriteLine("Big Board count: " + bigBoards);
            //Console.WriteLine("Mock Draft count: " + mockDrafts);
            //Console.WriteLine("Team Mock count: " + teamMockDrafts);
            //Console.WriteLine("Prospect count: " + prospects.Count);

            // Get today's date in the format of yyyy-mm-dd, in the Central Standard Time Zone
            DateTime timeUtc = DateTime.UtcNow;
            string today = "";
            try
            {
                TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
                DateTime cstTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, cstZone);
                today = cstTime.ToString("yyyy-MM-dd");
                Console.WriteLine("The date and time are {0} {1}.",
                                    cstTime,
                                    cstZone.IsDaylightSavingTime(cstTime) ?
                                            cstZone.DaylightName : cstZone.StandardName);
            }
            catch (TimeZoneNotFoundException)
            {
                Console.WriteLine("The registry does not define the Central Standard Time zone.");
                today = DateTime.UtcNow.ToString("yyyy-MM-dd");;
            }
            catch (InvalidTimeZoneException)
            {
                Console.WriteLine("Registry data on the Central Standard Time zone has been corrupted.");
                today = DateTime.UtcNow.ToString("yyyy-MM-dd");;
            }

            // Create a ConsensusBigBoardInfo object from the parsed values.
            var bigBoardInfo = new ConsensusBigBoardInfo(today, bigBoards, mockDrafts, teamMockDrafts, prospects.Count, "Recently Updated");
            var infos = new List<ConsensusBigBoardInfo>
            {
                bigBoardInfo
            };

            string baseDirectory = Path.Join("ranks", scrapeYear);
            baseDirectory.EnsureExists();

            // use CsvWriter to write bigBoardInfo to csv

            //This is the file name we are going to write.
            string bigBoardInfoFileName = Path.Combine(baseDirectory, $"{scrapeYear}BoardInfo.csv");

            Console.WriteLine("Creating Draft Info csv...");

            //Write projects to csv with date.
            infos.WriteToCsvFile(bigBoardInfoFileName);

            ctx.SpinnerStyle(Style.Parse("green"));

            AnsiConsole.MarkupLine("Doing some prospect work...");

            // get image link for schools from boardInfo
            Dictionary<string, string> schoolImageLinks = new Dictionary<string, string>();
            //schoolImageLinks = bigBoard.GetSchoolImageLinks();

            var prospectRankings = FindProspectsFromJson(prospects, today, ref schoolImageLinks);

            //Show prospects on screen.
            Spectre.Console.Table prospectTable = new Spectre.Console.Table();
            prospectTable.AddColumn("Player");
            prospectTable.AddColumn("Rank");
            prospectTable.AddColumn("School");
            prospectTable.AddColumn("Position");
            prospectTable.AddColumn("Peak");
            prospectTable.AddColumn("Points");
            prospectTable.Border(TableBorder.Ascii);

            foreach (var dude in prospectRankings)
            {
                //Console.WriteLine($"Player: {playerName} at rank {currentRank} from {playerSchool} playing {playerPosition} got up to peak rank {peakRank} with {leagifyPoints} possible points");
                prospectTable.AddRow(dude.PlayerName, dude.Rank, dude.School, dude.Position, dude.Peak, dude.ProjectedPoints.ToString());
            }
            
            AnsiConsole.Write(prospectTable);

            // Update the status and spinner
            ctx.Status("Writing draft prospect CSV");

            string playerInfoDirectory = Path.Combine(baseDirectory, "players");
            playerInfoDirectory.EnsureExists();

            string playerInfoFileName = Path.Combine(playerInfoDirectory, $"{today}-ranks.csv");
            string collectedRanksFileName = Path.Combine(baseDirectory, $"{scrapeYear}ranks.csv");

            //Write projects to csv with date.
            using (var writer = new StreamWriter(playerInfoFileName))
            using (var csv = new CsvWriter(writer, CultureInfo.CurrentCulture))
            {
                csv.WriteRecords(prospectRankings);
            }

            ctx.Status("Writing to collected ranks CSV");
            //Write projects to csv with date.
            prospectRankings.WriteToCsvFile(collectedRanksFileName);

            // OK, I'm going to use LINQ to sort the top schools by points, then by number of prospects.
            // The output I want here is: Rank, School, Conference, Points, Number of Prospects
            ctx.Status($"Putting together top schools....");

            var URLs = schoolImageLinks.ToList();

            var topSchools = prospectRankings
                .GroupBy(x => x.School)
                .Select(x => new
                {
                    School = x.Key,
                    Conference = x.First().Conference,
                    ProjectedPoints = x.Sum(y => y.ProjectedPoints),
                    NumberOfProspects = x.Count(),
                    //SchoolURL = schoolImageLinks
                })
                .OrderByDescending(x => x.ProjectedPoints)
                .ThenByDescending(x => x.NumberOfProspects)
                .Join(URLs, x => x.School, y => y.Key, (x, y) => new { x.School, x.Conference, x.ProjectedPoints, x.NumberOfProspects, SchoolURL = y.Value })
                .ToList();
            schools = topSchools.Count;

            Spectre.Console.Table schoolTable = new Spectre.Console.Table();
            schoolTable.AddColumn("School");
            schoolTable.AddColumn("Conf");
            schoolTable.AddColumn("Points");
            schoolTable.AddColumn("Prospects");
            schoolTable.Border(TableBorder.Square);

            var schoolChart = new BarChart();
            schoolChart.Label("[red]Top Schools[/]");

            // Chatty output to console.  It's messy but informative.
            //Console.WriteLine("\nTop Schools.....");
            foreach (var school in topSchools)
            {
                //Console.WriteLine($"{school.School} - {school.Conference} - {school.ProjectedPoints} - {school.NumberOfProspects}");
                schoolTable.AddRow(school.School, school.Conference, school.ProjectedPoints.ToString(), school.NumberOfProspects.ToString());
                schoolChart.AddItem(school.School, school.ProjectedPoints, Color.Red);
            }
            AnsiConsole.Write(schoolTable);
            AnsiConsole.Write(schoolChart);

            // Update the status and spinner
            ctx.Status("Writing Top Schools CSV");

            // Now, write these top schools to a CSV file in the schools directory for the year in question.
            // The file should be named with the date, then top-schools.csv, such as 2021-10-03-top-schools.csv
            // Also, write one line to a file that says how many schools there are, as well as the date, similar to 2022BoardInfo.csv
            string schoolInfoDirectory = Path.Combine(baseDirectory, "schools");
            schoolInfoDirectory.EnsureExists();

            string schoolRankInfoFileName = Path.Combine(schoolInfoDirectory, $"{today}-top-schools.csv");
            string schoolInfoFileName = Path.Combine(baseDirectory, $"{scrapeYear}SchoolInfo.csv");

            //Write schools to csv with date.
            using (var writer = new StreamWriter(schoolRankInfoFileName))
            using (var csv = new CsvWriter(writer, CultureInfo.CurrentCulture))
            {
                csv.WriteRecords(topSchools);
            }

            //Add School Info

            

            

            // Create a School Info object from the parsed values.
            //ScrapeDate,NumberOfSchools,TopSchool,TopProjectedPoints,ProspectCountForTopSchool
            var topSchool = topSchools.First();
            var schoolBoardInfo = new SchoolRankInfo(today, topSchools.Count, topSchool.School, topSchool.ProjectedPoints, topSchool.NumberOfProspects);
            var schoolInfos = new List<SchoolRankInfo>
            {
                schoolBoardInfo
            };

            //Write projects to csv with date.
            schoolInfos.WriteToCsvFile(schoolInfoFileName);

            // Similar to the top schools, I want to sort the top states by points, then by number of prospects, then by number of schools.
            var topStates = prospectRankings.GroupBy(x => x.State)
                .Select(x => new
                {
                    State = x.Key,
                    ProjectedPoints = x.Sum(y => y.ProjectedPoints),
                    NumberOfSchools = x.GroupBy(y => y.School).Count(), // Number of schools in the state.
                    NumberOfProspects = x.Count()
                })
                .OrderByDescending(x => x.ProjectedPoints)
                .ThenByDescending(x => x.NumberOfProspects)
                .ToList();
            // Chatty output to console.  It's messy but informative.

            Spectre.Console.Table stateTable = new Spectre.Console.Table();
            stateTable.AddColumn("State");
            stateTable.AddColumn("Points");
            stateTable.AddColumn("Schools");
            stateTable.AddColumn("Prospects");
            stateTable.Border(TableBorder.Rounded);
            stateTable.BorderColor(Color.Yellow);

            //Console.WriteLine("\nTop States.....");
            foreach (var state in topStates)
            {
                //Console.WriteLine($"{state.State} - {state.ProjectedPoints} - {state.NumberOfSchools} - {state.NumberOfProspects}");
                stateTable.AddRow(state.State, state.ProjectedPoints.ToString(), state.NumberOfSchools.ToString(), state.NumberOfProspects.ToString());
            }

            AnsiConsole.Write(stateTable);

            states = topStates.Count;

            // Now, write these top states to a CSV file in the states directory for the year in question.
            // The file should be named with the date, then top-states.csv, such as 2021-10-03-top-states.csv
            string statesDirectory = Path.Combine(baseDirectory, "states");
            statesDirectory.EnsureExists();

            string stateRankInfoFileName = Path.Combine(statesDirectory, $"{today}-top-states.csv");

            //Write schools to csv with date.
            using (var writer = new StreamWriter(stateRankInfoFileName))
            using (var csv = new CsvWriter(writer, CultureInfo.CurrentCulture))
            {
                csv.WriteRecords(topStates);
            }

            // Find prospects where state name is ""
            var emptyStates = prospectRankings.Where(x => string.IsNullOrEmpty(x.State)).ToList();

            if (emptyStates.Count > 0)
            {
                foreach (var prospect in emptyStates)
                {
                    AnsiConsole.WriteLine($"Player missing state: {prospect.PlayerName} - {prospect.School} - {prospect.State}");
                }
            }

            AnsiConsole.Write(new BarChart()
            .Width(120)
            .Label("[green bold underline]Total prospects in list[/]")
            .CenterLabel()
            .AddItem(":person: NFL Prospects :person:", prospects.Count, Color.Cyan1 )
            );

            // Give a rendered result to the terminal.
            AnsiConsole.Write(new BarChart()
            .Width(60)
            .Label("[green bold underline]Number of sources[/]")
            .CenterLabel()
            .AddItem(":american_football: Mock Drafts :american_football:", mockDrafts, Color.Green)
            .AddItem(":american_football: Team Mock Drafts :american_football:", teamMockDrafts, Color.Red)
            .AddItem(":american_football: Big Boards :american_football:", bigBoards, Color.Yellow)
            .AddItem(":school: Schools :school:", schools, Color.Blue)
            .AddItem(":clipboard: States :clipboard:", states, Color.Aqua)
            .AddItem(":cross_mark: State mismatches :cross_mark:", emptyStates.Count, Color.Orange1)
            );
        }
        
        private static List<ProspectRanking> FindProspectsFromJson(List<JsonElement> jsonProspects, string todayString, ref Dictionary<string, string> schoolImages)
        {
            var prospectRankings = new List<ProspectRanking>();

            //read in CSV from info/RanksToProjectedPoints.csv
            var dt = new DataTable();
            using (var reader = new StreamReader(Path.Combine("info", "RanksToProjectedPoints.csv")))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                using var dr = new CsvDataReader(csv);
                dt.Load(dr);
            }
            var ranksToPoints = dt.AsEnumerable()
                    .ToDictionary<DataRow, string, string>(row => row.Field<string>(0),
                                                        row => row.Field<string>(1));

            var dt2 = new DataTable();
            using (var reader = new StreamReader(Path.Combine("info", "SchoolStatesAndConferences.csv")))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                using var dr = new CsvDataReader(csv);
                dt2.Load(dr);
            }

            var schoolsToStatesAndConfs = dt2.AsEnumerable()
                    .ToDictionary(row => row.Field<string>(0),
                                  row => (row.Field<string>(1), row.Field<string>(2))
                                 );

            foreach (var jsonProspect in jsonProspects)
            {
                try
                {
                    var player = jsonProspect.GetProperty("player");
                    var team = jsonProspect.GetProperty("team");
                    var pick = jsonProspect.GetProperty("pick");

                    string playerName = player.GetProperty("name").GetString().Replace("'", "'");
                    string playerPosition = player.GetProperty("position").GetString();
                    string playerSchool = player.GetProperty("college").GetProperty("name").GetString();
                    
                    string currentRank = pick.ToString();
                    string peakRank = "1"; // Could be calculated differently, for now set to 1
                    
                    // Get school logo from team property
                    string schoolLogo = "";
                    if (team.TryGetProperty("logo", out var logoElement) && !logoElement.ValueKind.Equals(JsonValueKind.Null))
                    {
                        schoolLogo = logoElement.GetString();
                    }
                    
                    // Get projected draft information from consensus if available
                    string projectedDraftSpot = "";
                    string projectedDraftTeam = "";
                    
                    if (jsonProspect.TryGetProperty("consensus", out var consensus) && !consensus.ValueKind.Equals(JsonValueKind.Null))
                    {
                        if (consensus.TryGetProperty("pick", out var consensusPick))
                        {
                            projectedDraftSpot = consensusPick.GetInt32().ToString();
                        }
                        if (consensus.TryGetProperty("team_name", out var teamName))
                        {
                            projectedDraftTeam = teamName.GetString();
                        }
                    }

                    // Clean school name
                    playerSchool = playerSchool.ConvertSchool();

                    if (!schoolImages.ContainsKey(playerSchool))
                    {
                        schoolImages.Add(playerSchool, schoolLogo);
                    }

                    string leagifyPoints = ranksToPoints.GetValueOrDefault(currentRank, "1");
                    (string schoolConference, string schoolState) = schoolsToStatesAndConfs.GetValueOrDefault(playerSchool, ("", ""));

                    var currentPlayer = new ProspectRanking(todayString, currentRank, peakRank, playerName, playerSchool, playerPosition, schoolState, schoolConference, leagifyPoints, projectedDraftSpot, projectedDraftTeam);
                    prospectRankings.Add(currentPlayer);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning: Could not parse prospect data: {ex.Message}[/]");
                    // Continue processing other prospects
                }
            }

            return prospectRankings;
        }

    }
}
