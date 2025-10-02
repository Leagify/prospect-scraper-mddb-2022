# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 8 console application that scrapes NFL prospect consensus rankings from nflmockdraftdatabase.com. The application extracts prospect data, processes it through various mappings, and outputs structured CSV files for analysis.

## Common Commands

### Build and Run
```bash
dotnet build                    # Build the project
dotnet run                      # Run the scraper
dotnet watch run               # Run with hot reload during development
```

### Development
```bash
dotnet restore                 # Restore NuGet packages
dotnet test                    # Run tests (if any exist)
```

## Architecture

### Core Components

- **Program.cs**: Main entry point that reads configuration and orchestrates the scraping process
- **scraper.conf**: Configuration file specifying years to scrape and URL patterns
- **Extensions/**: Contains the core scraping logic and data processing extensions
  - `StatusContextExtensions.cs`: Main scraping logic using HtmlAgilityPack (not ScrapySharp as README suggests)
  - `HtmlNodeCollectionExtensions.cs`: HTML parsing and data extraction methods
  - `SectionExtensions.cs`: Configuration helper methods
- **DTOs/**: Data transfer objects representing the scraped data structure
- **Maps/**: CsvHelper mapping classes for CSV output formatting

### Data Flow

1. Configuration is loaded from `scraper.conf`
2. URLs are constructed for specified years using the UrlPattern
3. Web pages are scraped using HtmlAgilityPack's HtmlWeb class
4. HTML is parsed to extract prospect rankings, school information, and metadata
5. Data is processed through various extensions and mapped to DTOs
6. Results are written to CSV files in the `ranks/` directory

### Key Dependencies

- **HtmlAgilityPack**: Web scraping (primary scraper, not ScrapySharp)
- **CsvHelper**: CSV file generation and mapping
- **SharpConfig**: Configuration file parsing
- **Spectre.Console**: Console UI and progress indication

### Data Processing

The scraper extracts:
- Prospect rankings and projected points
- School/conference mappings from CSV files in `info/`
- State-to-region mappings
- Draft metadata (big boards used, mock drafts, last updated dates)

Output files are organized by year in the `ranks/` directory structure.

### Configuration

Edit `scraper.conf` to:
- Change years to scrape (YearsToScrape array)
- Modify URL patterns for different draft years
- Add new year-specific URLs if needed
- Switch between Web and CSV data sources
- Configure CSV processing options (base path, run count)

## Development Guidelines

**CRITICAL: Always commit before running `dotnet run`**
- The application creates/modifies files in `ranks/` directory
- CSV mode moves files to `processed/` subfolders
- If errors occur, you need to revert both code changes AND file system changes
- Commit code changes first, then test, then commit results if successful

**CRITICAL: Always check for state mapping errors after running `dotnet run`**
- The most important thing to verify is whether there are state/conference mapping mismatches
- Look for error messages about prospects with missing state information
- State mapping errors indicate schools missing from `info/SchoolStatesAndConferences.csv`
- Do NOT commit if there are unresolved state mapping errors
- Add missing schools to the mapping file before proceeding

## Known Issues

### Web Scraping Mode (as of 2025-09-29)

**Status**: BROKEN - NullReferenceException when parsing website data

**Error Details**:
- Exception occurs in `StatusContextExtensions.cs` at line 29
- Specific failure: `draftInfo[0].ChildNodes[2].InnerText` returns null
- Root cause: nflmockdraftdatabase.com website structure has changed since code was written

**Error Location**:
```csharp
// This line fails with NullReferenceException:
string lastUpdated = draftInfo[0].ChildNodes[2].InnerText.Replace("Last Updated: ", "").Trim();
```

**Impact**:
- Web scraping mode (`DataSource.Mode = "Web"`) is currently non-functional
- CSV processing mode (`DataSource.Mode = "CSV"`) works perfectly as an alternative

**Next Steps for Fixing**:
1. Inspect current HTML structure of nflmockdraftdatabase.com/big-boards/2026/consensus-big-board-2026
2. Update XPath selectors in `StatusContextExtensions.cs` to match new DOM structure
3. Particularly focus on the "Last Updated" parsing logic and board count extraction
4. Test against multiple years to ensure robustness

**Workaround**: Use CSV processing mode, which is fully functional and provides the same data structure.