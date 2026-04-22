# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Commands

```bash
# Build the project
dotnet build

# Run tests
dotnet test

# Run in development
dotnet run --project src/AdeptusBoticus

# Publish for production
dotnet publish -c Release -r linux-x64 -o ./publish
```

## Architecture

This is a Discord bot that monitors Warhammer Community for new articles and posts them to configured channels.

### Core Components

- **Program.cs**: Entry point with dependency injection setup, polling timer, and graceful shutdown
- **WarComArticleService**: Main service that fetches articles, matches them to channels, and posts to Discord
- **DiscordBot**: Handles Discord API interactions and message posting
- **DataService**: Manages persistence of article timestamps to prevent duplicates
- **WarComArticleReader**: HTTP client for fetching articles from Warhammer Community API

### Data Flow

1. Timer triggers `CheckArticlesAsync()` every 5 minutes (configurable)
2. Fetch articles from Warhammer Community API
3. For each configured channel, find newest matching article by category
4. Compare article timestamp with last posted timestamp
5. If newer, post embed message to Discord and update timestamp
6. Track timestamps in `trackers.json` to prevent duplicate posts

### Configuration

Environment variables in `.env` file:
- `DISCORD_TOKEN`: Required bot token
- `WH40K_ID`, `AOS_ID`, `HH_ID`, `BB_ID`: Channel IDs for each category
- `POLLING_INTERVAL_MS`: Check interval (default: 300000ms = 5min)
- `DATA_FILE_PATH`: Path to tracker file (default: ./data/trackers.json)

### Article Timestamps

Articles have a `Date` field that's parsed using `GetParsedDate()` which handles:
- "dd MMM yy" format with year correction (20th/21st century)
- Fallback to standard DateTime parsing
- Returns DateTime.MinValue if parsing fails

### Category Matching

Articles are matched to channels based on topic titles:
- WH40K: "Warhammer 40000", "Warhammer 40,000", "40k", "Kill Team"
- AoS: "Warhammer Age of Sigmar", "Old World", "Warhammer: The Old World", etc.
- HH: "The Horus Heresy", "Warhammer: The Horus Heresy"
- BB: "Blood Bowl"

### Deployment

- Uses systemd service for Linux deployment
- Fly.io deployment with persistent volume for `trackers.json`
- Docker multi-stage build for small image size