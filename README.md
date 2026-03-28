# Adeptus Boticus

A .NET Discord bot that monitors Warhammer Community for new articles and automatically posts them to configured channels based on game system.

## Features

- Automatically fetches new articles from Warhammer Community API
- Routes articles to appropriate Discord channels (40k, AoS, HH, Blood Bowl)
- Rich embed messages with thumbnails, titles, and links
- Prevents duplicate posts using persistent timestamp tracking
- Configurable check intervals and data storage location

## Quick Setup

### Prerequisites

- .NET 10.0 SDK
- Discord bot token (from [Discord Developer Portal](https://discord.com/developers/applications))
- Discord channel IDs (enable Developer Mode in Discord to copy IDs)

### Installation

```bash
# Clone and build
dotnet restore
dotnet build

# Create .env file from sample
cp sample.env src/AdeptusBoticus/.env
# Edit src/AdeptusBoticus/.env with your values
```

### Configuration

Set these environment variables in `.env`:

| Variable | Required | Description |
|----------|----------|-------------|
| `DISCORD_TOKEN` | Yes | Your Discord bot token |
| `WH40K_ID` | No* | Channel ID for Warhammer 40k articles |
| `AOS_ID` | No* | Channel ID for Age of Sigmar articles |
| `HH_ID` | No* | Channel ID for Horus Heresy articles |
| `BB_ID` | No* | Channel ID for Blood Bowl articles |
| `RSS_CHECK_INTERVAL_MS` | No | Check interval (default: 300000 = 5 min) |
| `DATA_FILE_PATH` | No | Path to tracker file (default: `./data/trackers.json`) |

*Only required for the categories you want to enable

### Running

```bash
# Development
dotnet run --project src/AdeptusBoticus

# Production build
dotnet publish -c Release -r linux-x64 -o ./publish
```

## Deploying to Linux

1. Publish the app and copy files to server
2. Create `.env` file in the deployment directory
3. Create systemd service:

```ini
[Unit]
Description=AdeptusBoticus Discord Bot
After=network.target

[Service]
Type=simple
User=YOUR_USER
WorkingDirectory=/opt/adeptusboticus
ExecStart=/opt/adeptusboticus/AdeptusBoticus
Restart=on-failure
RestartSec=10

[Install]
WantedBy=multi-user.target
```

4. Enable and start:

```bash
sudo systemctl daemon-reload
sudo systemctl enable adeptusboticus
sudo systemctl start adeptusboticus
sudo systemctl status adeptusboticus
```

## Category Routing

| Channel Variable | Matches Topics |
|------------------|----------------|
| `WH40K` | Warhammer 40,000, 40k, Kill Team |
| `AOS` | Age of Sigmar, The Old World, AoS |
| `HH` | The Horus Heresy |
| `BB` | Blood Bowl |

## Testing

```bash
dotnet test
```

## License

MIT
