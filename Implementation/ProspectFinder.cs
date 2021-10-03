using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using HtmlAgilityPack;
using prospect_scraper_mddb_2022.Extensions;

namespace prospect_scraper_mddb_2022.Implementation
{
    public class ProspectFinder
    {
        public List<ProspectRanking> FindProspects(HtmlNodeCollection nodes, string todayString)
        {
            var prospectRankings = new List<ProspectRanking>();

            var ranksToPoints = ReadRanksToPoints();
            var schoolsToStatesAndConfs = ReadSchoolsStatesConferences();

            foreach (var node in nodes)
            {
                var pickContainer = node.Descendants().FirstOrDefault(n => n.HasClass("pick-container"));
                var playerContainer = node.Descendants().FirstOrDefault(n => n.HasClass("player-container"));
                var percentageContainer = node.Descendants().FirstOrDefault(n => n.HasClass("percentage-container"));
                string projectedDraftSpot = "";
                string projectedDraftTeam = "";
                string playerSchool;

                var actualPickStuff = pickContainer?.FirstChild;
                string currentRank = actualPickStuff?.FirstChild.InnerText;
                var peakRankHtml = actualPickStuff?.LastChild; //Rank 1 is in the middle child, not the last child for some reason. Seems to l=only happen when actualPickStuff.LastChild has 3 children.
                string peakRank = peakRankHtml?.ChildNodes[1].InnerText; // this is inside a span, but I'm not sure if it's reliably the second element.
                var namePositionSchool = node.LastChild;
                string playerName = playerContainer?.FirstChild.InnerText.Replace("&#39;", "'");
                string playerPosition = playerContainer?.LastChild.FirstChild.InnerText.Replace("|", "").Trim();
                int? afterPipeStringLength = playerContainer?.LastChild.FirstChild.InnerText.Split("|")[1].Length;
                if (playerContainer.LastChild.ChildNodes.Count == 2 && afterPipeStringLength <= 2)
                {
                    playerSchool = playerContainer.LastChild.LastChild.InnerText.Replace("&amp;", "&");
                }
                else if (afterPipeStringLength > 2)
                {
                    playerSchool = playerContainer.LastChild.FirstChild.InnerText.Split("|")[1].Replace("&amp;", "&").Trim();
                }
                else
                {
                    playerSchool = playerContainer.LastChild.ChildNodes[1].InnerText.Replace("&amp;", "&");
                }

                if (percentageContainer != null)
                {
                    (projectedDraftSpot, projectedDraftTeam) = ReadPercentageContainer(percentageContainer, projectedDraftSpot, projectedDraftTeam);
                }

                Console.WriteLine(
                    $"Player: {playerName} at rank {currentRank} from {playerSchool} playing {playerPosition} got up to peak rank {peakRank}");

                playerSchool = playerSchool.ConvertSchool();

                string leagifyPoints = ranksToPoints[currentRank];
                string schoolConference = schoolsToStatesAndConfs[playerSchool].Item1;
                string schoolState = schoolsToStatesAndConfs[playerSchool].Item2;

                var currentPlayer = new ProspectRanking
                {
                    Rank = currentRank,
                    Peak = peakRank,
                    PlayerName = playerName,
                    School = playerSchool,
                    Position = playerPosition,
                    RankingDateString = todayString,
                    Projection = projectedDraftSpot,
                    ProjectedTeam = projectedDraftTeam,
                    State = schoolState,
                    Conference = schoolConference,
                    ProjectedPoints = leagifyPoints
                };
                prospectRankings.Add(currentPlayer);
            }

            return prospectRankings;
        }

        private static IDictionary<string, string> ReadRanksToPoints()
        {
            //read in CSV from info/RanksToProjectedPoints.csv
            var dt = new DataTable();
            using var streamReader = new StreamReader($"info{Path.DirectorySeparatorChar}RanksToProjectedPoints.csv");
            using var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);

            // Do any configuration to `CsvReader` before creating CsvDataReader.
            using var csvDataReader = new CsvDataReader(csvReader);
            dt.Load(csvDataReader);

            // Transform datatable dt to dictionary
            return dt.AsEnumerable()
                .ToDictionary<DataRow, string, string>(row => row.Field<string>(0),
                    row => row.Field<string>(1));
        }

        private static IDictionary<string, (string, string)> ReadSchoolsStatesConferences()
        {
            var dt2 = new DataTable();
            using var reader = new StreamReader($"info{Path.DirectorySeparatorChar}SchoolStatesAndConferences.csv");
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            // Do any configuration to `CsvReader` before creating CsvDataReader.
            using var dr = new CsvDataReader(csv);
            dt2.Load(dr);

            return dt2.AsEnumerable()
                .ToDictionary<DataRow, string, (string, string)>(row => row.Field<string>(0),
                    row => (row.Field<string>(1), row.Field<string>(2))
                );
        }
        
        private static (string, string) ReadPercentageContainer(HtmlNode percentageContainer, string projectedDraftSpot, string projectedDraftTeam)
        {
            int percentageContainerChildNodeCount = percentageContainer.ChildNodes.Count;
            if (percentageContainerChildNodeCount != 2) 
                return (projectedDraftSpot, projectedDraftTeam);
            
            //if projected draft spot starts with "Possible" then it's a general grade with no consensus.
            projectedDraftSpot = percentageContainer.FirstChild.LastChild.InnerText.Replace("#", "").Replace(":", "");
            projectedDraftTeam = percentageContainer.LastChild.InnerText;
            if (projectedDraftTeam == "No Consensus Available") 
                return (projectedDraftSpot, projectedDraftTeam);
            
            string projectedDraftTeamHref = percentageContainer.LastChild.FirstChild.Attributes.FirstOrDefault()?.Value;
            string[] hrefStrings = projectedDraftTeamHref?.Split("/");
            projectedDraftTeam = hrefStrings?[^1].Replace("-", " ").ToUpper();

            return (projectedDraftSpot, projectedDraftTeam);
        }
    }
}