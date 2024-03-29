﻿using CsvHelper;
using HtmlAgilityPack;
using prospect_scraper_mddb_2022.DTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;

namespace prospect_scraper_mddb_2022.Extensions
{
    public static class HtmlNodeCollectionExtensions
    {
        public static List<ProspectRanking> FindProspects(this HtmlNodeCollection nodes, string todayString, ref Dictionary<string, string> schoolImages)
        {
            var prospectRankings = new List<ProspectRanking>();

            //read in CSV from info/RanksToProjectedPoints.csv
            var dt = new DataTable();
            using (var reader = new StreamReader(Path.Combine("info", "RanksToProjectedPoints.csv")))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                // Do any configuration to `CsvReader` before creating CsvDataReader.
                using var dr = new CsvDataReader(csv);
                dt.Load(dr);
            }
            // Transform datatable dt to dictionary
            var ranksToPoints = dt.AsEnumerable()
                    .ToDictionary<DataRow, string, string>(row => row.Field<string>(0),
                                                        row => row.Field<string>(1));

            var dt2 = new DataTable();
            using (var reader = new StreamReader(Path.Combine("info", "SchoolStatesAndConferences.csv")))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                // Do any configuration to `CsvReader` before creating CsvDataReader.
                using var dr = new CsvDataReader(csv);
                dt2.Load(dr);
            }

            var schoolsToStatesAndConfs = dt2.AsEnumerable()
                    .ToDictionary(row => row.Field<string>(0),
                                  row => (row.Field<string>(1), row.Field<string>(2))
                                 );

            foreach (var node in nodes)
            {
                var pickContainer = node.Descendants().FirstOrDefault(n => n.HasClass("pick-container"));
                var playerContainer = node.Descendants().FirstOrDefault(n => n.HasClass("player-container"));
                var percentageContainer = node.Descendants().FirstOrDefault(n => n.HasClass("percentage-container"));

                var pickContainer2 = node.Descendants().FirstOrDefault(n => n.HasClass("pick-number-container"));
                var playerContainer2 = node.Descendants().FirstOrDefault(n => n.HasClass("player-details-container"));
                var percentageContainer2 = node.LastChild;
                
                pickContainer = pickContainer ?? pickContainer2;
                playerContainer = playerContainer ?? playerContainer2;
                percentageContainer = percentageContainer ?? percentageContainer2;

                string projectedDraftSpot = "";
                string projectedDraftTeam = "";
                string playerSchool = "";

                var actualPickStuff = pickContainer.FirstChild;
                string currentRank = actualPickStuff.FirstChild.InnerText;
                var peakRankHtml = pickContainer.LastChild; //Rank 1 is in the middle child, not the last child for some reason. Seems to only happen when actualPickStuff.LastChild has 3 children.
                string peakRank = peakRankHtml.InnerText.Replace("Peak: ", "");  // this is inside a span, but I'm not sure if it's reliably the second element.
                var namePositionSchool = node.LastChild;
                string playerName = playerContainer.FirstChild.InnerText.Replace("&#39;", "'");
                string playerPositionOld = playerContainer.LastChild.FirstChild.InnerText.Replace("|", "").Trim();
                string playerPosition = playerContainer.LastChild.FirstChild.InnerText.Split("|")[0].Trim();
                int afterPipeStringLength = playerContainer.LastChild.FirstChild.InnerText.Split("|")[1].Length;
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

                string asdfasdfhgh = percentageContainer.InnerText.Substring(0, 10);
                if (percentageContainer != null && percentageContainer.InnerText.Length >= 10 && percentageContainer.InnerText.Substring(0, 10).Equals("Projection"))
                {
                    // int percentageContainerChildNodeCount = percentageContainer.ChildNodes.Count;
                    // if (percentageContainerChildNodeCount == 2)
                    // {
                    //     //if projected draft spot starts with "Possible" then it's a general grade with no consensus.
                    //     projectedDraftSpot = percentageContainer.FirstChild.LastChild.InnerText.Replace("#", "").Replace(":", "");
                    //     projectedDraftTeam = percentageContainer.LastChild.InnerText;
                    //     if (projectedDraftTeam != "No Consensus Available")
                    //     {
                    //         string projectedDraftTeamHref = percentageContainer.LastChild.FirstChild.Attributes.FirstOrDefault()?.Value;
                    //         string[] hrefStrings = projectedDraftTeamHref?.Split("/");
                    //         projectedDraftTeam = hrefStrings[^1].Replace("-", " ").ToUpper();
                    //     }
                    // }
                    int stringLength = percentageContainer.InnerText.Length;
                    if (stringLength > 10)
                    {
                        projectedDraftSpot = percentageContainer.InnerText.Substring(10, stringLength - 10).Replace("#", "").Replace(":", "");
                        if (projectedDraftSpot.Equals("None"))
                        {
                            projectedDraftSpot = "First Round";
                        }
                        if (int.TryParse(projectedDraftSpot, out int projectedDraftSpotInt))
                        {
                            var url = percentageContainer.LastChild.FirstChild.Attributes[1].Value;
                            string[] urlParts = url.Split("/");
                            string team = urlParts[^1].Replace("-", " ").ToUpper();
                            projectedDraftTeam = team;
                        }
                    }
                    
                }

                //var ranking = new ProspectRanking();

                playerSchool = playerSchool.ConvertSchool();

                //var schoolLogo = pickContainer.LastChild.LastChild.GetAttributeValue("src", "").Replace("&amp;", "&");
                
                var logoHtmlParts = node.FirstChild.LastChild.LastChild.OuterHtml.Split("\"");
                string schoolLogo = logoHtmlParts[5];

                if (!schoolImages.ContainsKey(playerSchool))
                {
                    schoolImages.Add(playerSchool, schoolLogo);
                }
                //schoolImages.Add(playerSchool, schoolLogo);
                string leagifyPoints = ranksToPoints.GetValueOrDefault(currentRank, "1");

                (string schoolConference, string schoolState) = schoolsToStatesAndConfs.GetValueOrDefault(playerSchool, ("", ""));

                //Console.WriteLine($"Player: {playerName} at rank {currentRank} from {playerSchool} playing {playerPosition} got up to peak rank {peakRank} with {leagifyPoints} possible points");

                var currentPlayer = new ProspectRanking(todayString, currentRank, peakRank, playerName, playerSchool, playerPosition, schoolState, schoolConference, leagifyPoints, projectedDraftSpot, projectedDraftTeam);
                prospectRankings.Add(currentPlayer);
            }

            return prospectRankings;
        }
        public static Dictionary<string, string> GetSchoolImageLinks(this HtmlNodeCollection bigBoardNode)
        {
            var schoolImageLinks = new Dictionary<string, string>();
            var lis = bigBoardNode.Elements().Where(n => n.Name == "li").ToList();
            var nodeCount = bigBoardNode.Count();
            foreach (var schoolImageNode in bigBoardNode)
            {
                var li = bigBoardNode.Where(n => n.Name == "li").ToList();
                var schoolImageNodes = bigBoardNode.Descendants().FirstOrDefault(n => n.HasClass("school-image"));
                var pickContainer = bigBoardNode.Descendants().FirstOrDefault(n => n.HasClass("pick-container"));
                var playerContainer = bigBoardNode.Descendants().FirstOrDefault(n => n.HasClass("player-details"));

                var schoolName = "";
                int afterPipeStringLength = playerContainer.InnerText.Split("|")[1].Length;
                string schoolAttempt = playerContainer.InnerText.Split("|")[1].Trim();

                if (playerContainer.LastChild.ChildNodes.Count == 2 && afterPipeStringLength <= 2)
                {
                    schoolName = playerContainer.InnerText.Split("|")[1].Replace("&amp;", "&").Trim();
                }
                else if (afterPipeStringLength > 2)
                {
                    schoolName = playerContainer.InnerText.Split("|")[1].Replace("&amp;", "&").Trim();
                }
                else
                {
                    schoolName = playerContainer.InnerText.Split("|")[1].Replace("&amp;", "&").Trim();
                }
                
                var schoolImageLink = "";
                

                
                schoolImageLinks.Add(schoolName, schoolImageLink);
            }
            return schoolImageLinks;
        }
    }
}
