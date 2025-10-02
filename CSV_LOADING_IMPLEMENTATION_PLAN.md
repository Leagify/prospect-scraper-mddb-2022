# CSV Loading Mode Implementation Plan

## Overview
This document outlines the implementation plan for adding CSV file loading capabilities to the prospect scraper, allowing it to process pre-existing CSV files instead of scraping websites.

## Current vs New Data Flow

### Current Flow (Scraping)
```
Website Scraping → JSON Parsing → ProspectRanking Objects → CSV Output & Analysis
```

### New Flow (CSV Loading)
```
CSV Files (mockdb-csv/) → CSV Parsing → Data Enrichment → ProspectRanking Objects → CSV Output & Analysis
```

## Data Format Analysis

### Current Scraped Format (Full Data)
```csv
Rank,Peak,PlayerName,School,Position,RankingDateString,Projection,ProjectedTeam,State,Conference,ProjectedPoints
1,1,Aidan Hutchinson,Michigan,EDGE,2022-04-22,1,JACKSONVILLE JAGUARS,Michigan,Big Ten,35
```

### New CSV Format (Limited Data)
```csv
Rank,Player Name,Position,College
1,Cade Klubnik,QB,Clemson
```

### File Structure
```
mockdb-csv/
├── 2026/
│   └── consensus-big-board-2026-20250814.csv
├── 2027/
│   └── consensus-big-board-2027-20260315.csv
└── 2028/
    └── consensus-big-board-2028-20270420.csv
```

## Configuration Updates

### Enhanced scraper.conf
```ini
[General]
YearsToScrape = { 2026 }
DataSource = "scrape"  # Options: "scrape", "csv"
CsvBasePath = "mockdb-csv"

[CsvSettings]
# Pattern: {basePath}/{year}/consensus-big-board-{year}-{date}.csv
FileNamePattern = "consensus-big-board-{year}-{date}.csv"
# If date not specified, use most recent file in directory
AutoSelectLatestFile = true
# Optional: specify exact date for file selection
# TargetDate = "20250814"
```

## Data Mapping Strategy

### Available Fields (Direct Mapping)
- ✅ **Rank** → Rank
- ✅ **Player Name** → PlayerName  
- ✅ **Position** → Position
- ✅ **College** → School (with .ConvertSchool() cleanup)

### Missing Fields (Require Defaults/Lookups)
- **Peak** → Default to current Rank (assumption: current rank is peak)
- **RankingDateString** → Extract from filename (`20250814` from `consensus-big-board-2026-20250814.csv`)
- **Projection** → Empty string (not available in source CSV)
- **ProjectedTeam** → Empty string (not available in source CSV)
- **State** → Lookup from `info/SchoolStatesAndConferences.csv`
- **Conference** → Lookup from `info/SchoolStatesAndConferences.csv`  
- **ProjectedPoints** → Lookup from `info/RanksToProjectedPoints.csv`

### Board Info Defaults
```csharp
var bigBoardInfo = new ConsensusBigBoardInfo(
    today, 
    0,  // bigBoards (CSV source - no big boards scraped)
    0,  // mockDrafts (CSV source - no mock drafts scraped) 
    0,  // teamMockDrafts (CSV source - no team mock drafts scraped)
    prospects.Count, 
    "CSV Import"
);
```

## Architecture Design

### 1. Data Source Interface
```csharp
public interface IDataSource
{
    List<ProspectRanking> LoadProspects(string year, string today);
    ConsensusBigBoardInfo GetBoardInfo(string year, string today, int prospectCount);
    Dictionary<string, string> GetSchoolImageLinks();
}

public class ScrapingDataSource : IDataSource 
{
    // Current Selenium-based scraping logic
}

public class CsvDataSource : IDataSource 
{
    // New CSV file loading logic
}
```

### 2. Enhanced ProspectRanking Constructor
```csharp
// Add constructor overload for CSV data
public static ProspectRanking FromCsvData(
    string csvRank, 
    string csvPlayerName, 
    string csvPosition, 
    string csvSchool, 
    string dateFromFilename,
    Dictionary<string, (string State, string Conference)> schoolLookup,
    Dictionary<string, string> pointsLookup)
{
    var school = csvSchool.ConvertSchool();
    var (state, conference) = schoolLookup.GetValueOrDefault(school, ("", ""));
    var points = pointsLookup.GetValueOrDefault(csvRank, "1");
    
    return new ProspectRanking(
        dateFromFilename,    // RankingDateString
        csvRank,            // Rank
        csvRank,            // Peak (default to current rank)
        csvPlayerName,      // PlayerName
        school,             // School
        csvPosition,        // Position
        state,              // State (from lookup)
        conference,         // Conference (from lookup)
        points,             // ProjectedPoints (from lookup)
        "",                 // Projection (empty - not available)
        ""                  // ProjectedTeam (empty - not available)
    );
}
```

### 3. CSV File Processing Logic
```csharp
public class CsvFileProcessor
{
    public string FindCsvFile(string basePath, string year, string targetDate = null)
    {
        var yearPath = Path.Combine(basePath, year);
        var pattern = $"consensus-big-board-{year}-*.csv";
        var files = Directory.GetFiles(yearPath, pattern);
        
        if (targetDate != null)
        {
            return files.FirstOrDefault(f => f.Contains(targetDate));
        }
        
        // Auto-select latest file by date in filename
        return files
            .Select(f => new { 
                File = f, 
                Date = ExtractDateFromFilename(f) 
            })
            .OrderByDescending(x => x.Date)
            .FirstOrDefault()?.File;
    }
    
    private DateTime ExtractDateFromFilename(string filename)
    {
        var match = Regex.Match(filename, @"-(\d{8})\.csv$");
        if (match.Success && DateTime.TryParseExact(
            match.Groups[1].Value, "yyyyMMdd", null, 
            DateTimeStyles.None, out var date))
        {
            return date;
        }
        return DateTime.MinValue;
    }
}
```

## Implementation Task List

### Phase 1: Core Infrastructure
- [ ] **Task 1.1**: Create `IDataSource` interface
- [ ] **Task 1.2**: Extract current scraping logic into `ScrapingDataSource` class
- [ ] **Task 1.3**: Update configuration parser to handle `DataSource` and `CsvSettings`
- [ ] **Task 1.4**: Create `CsvFileProcessor` utility class
- [ ] **Task 1.5**: Add `FromCsvData` static method to `ProspectRanking` class

### Phase 2: CSV Data Source Implementation  
- [ ] **Task 2.1**: Implement `CsvDataSource` class
- [ ] **Task 2.2**: Add CSV file discovery and selection logic
- [ ] **Task 2.3**: Implement CSV parsing with CsvHelper
- [ ] **Task 2.4**: Add date extraction from filename functionality
- [ ] **Task 2.5**: Implement data enrichment (state, conference, points lookups)
- [ ] **Task 2.6**: Create mock school image links dictionary for CSV mode

### Phase 3: Main Application Integration
- [ ] **Task 3.1**: Update `StatusContextExtensions.ScrapeYear()` to use data source factory
- [ ] **Task 3.2**: Create data source factory based on configuration
- [ ] **Task 3.3**: Ensure all existing CSV outputs work with CSV-sourced data
- [ ] **Task 3.4**: Update status messages to indicate data source (CSV vs Scraping)
- [ ] **Task 3.5**: Add configuration validation and error handling

### Phase 4: Testing & Validation
- [ ] **Task 4.1**: Test with existing 2026 CSV file (`consensus-big-board-2026-20250814.csv`)
- [ ] **Task 4.2**: Verify all output files are generated correctly
- [ ] **Task 4.3**: Validate school/state analysis works with lookup data
- [ ] **Task 4.4**: Test auto-file-selection logic with multiple CSV files
- [ ] **Task 4.5**: Test backwards compatibility with scraping mode

### Phase 5: Documentation & Polish
- [ ] **Task 5.1**: Update README.md with CSV loading instructions
- [ ] **Task 5.2**: Add example configuration for CSV mode
- [ ] **Task 5.3**: Document CSV file format requirements
- [ ] **Task 5.4**: Add error messages for missing CSV files or invalid formats
- [ ] **Task 5.5**: Create migration guide for switching between modes

## Output Compatibility

### Maintained Outputs
All existing CSV outputs will continue to work:
- ✅ **Individual player files**: `ranks/{year}/players/{date}-ranks.csv`
- ✅ **Collected ranks**: `ranks/{year}/{year}ranks.csv`
- ✅ **Top schools analysis**: `ranks/{year}/schools/{date}-top-schools.csv`
- ✅ **Top states analysis**: `ranks/{year}/states/{date}-top-states.csv`
- ✅ **Board info files**: `ranks/{year}/{year}BoardInfo.csv`
- ✅ **School info files**: `ranks/{year}/{year}SchoolInfo.csv`

### Output Differences in CSV Mode
- **Projection/ProjectedTeam fields**: Will be empty in output files
- **Peak field**: Will show same value as current rank
- **Board counts**: Will show 0 for bigBoards, mockDrafts, teamMockDrafts
- **LastUpdated**: Will show "CSV Import" instead of "Recently Updated"
- **Charts**: Will display "CSV Import" data source in visualizations

## Backwards Compatibility

### Existing Behavior Preserved
- **Default mode**: `DataSource = "scrape"` remains default behavior
- **Configuration**: Existing scraper.conf files work without modification
- **File structure**: No changes to output directory structure
- **Data types**: All existing DTOs and classes remain unchanged

### Migration Path
1. **Phase 1**: Add CSV capability alongside existing scraping
2. **Phase 2**: Users can opt-in by changing `DataSource = "csv"`
3. **Phase 3**: No breaking changes to existing workflows

## Error Handling Strategy

### Configuration Errors
- Invalid `DataSource` value → Default to "scrape" with warning
- Missing `CsvBasePath` → Error with helpful message
- Invalid year in CSV filename → Skip file with warning

### File System Errors  
- CSV directory doesn't exist → Clear error message with path
- No CSV files found → List available files and expected naming convention
- CSV file format invalid → Show expected vs actual format
- Lookup table files missing → Error with specific missing file names

### Data Quality Issues
- Missing school in lookup tables → Warning with school name, continue processing
- Invalid rank values → Skip row with warning
- Empty or malformed CSV → Clear error with line number

This comprehensive plan ensures a smooth implementation while maintaining full backwards compatibility and robust error handling.