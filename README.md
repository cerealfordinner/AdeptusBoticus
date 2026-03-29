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

## Deploying to Fly.io

[Fly.io](https://fly.io) is an excellent platform for running this bot with free tier and persistent storage.

### Prerequisites

- [Fly.io CLI](https://fly.io/docs/hands-on/install-fly-cli/) installed
- A Fly.io account (sign up with `fly auth login`)

### Setup Steps

1. **Create the app on Fly.io**

   ```bash
   fly apps create adeptusboticus
   ```

   This registers the app. You can skip this if you've already run `fly launch` before.

2. **Create a persistent volume** (stores `trackers.json`)

   ```bash
   fly volumes create trackers_data --region iad --size 1
   ```

   Choose a region closest to you. `iad` (Virginia) is a good default.

3. **Set secrets** (environment variables)

   ```bash
   fly secrets set \
     DISCORD_TOKEN="your_discord_bot_token" \
     WH40K_ID="channel_id_optional" \
     AOS_ID="channel_id_optional" \
     HH_ID="channel_id_optional" \
     BB_ID="channel_id_optional" \
     RSS_CHECK_INTERVAL_MS="300000"
   ```

   Only set channel IDs for the categories you want to enable.

4. **Deploy**

   ```bash
   fly deploy
   ```

   Fly will build the Docker image remotely on their infrastructure.

5. **Monitor**

   ```bash
   fly logs -f
   ```

   This streams logs. Use `fly status` to check VM status.

### Notes

- The `fly.toml` configures the bot as a worker (no HTTP endpoint).
- The mounted volume at `/data` persists tracker data across restarts and deploys.
- The free tier provides 1 VM with 256MB RAM—plenty for this bot.
- The Dockerfile is optimized for small image size using multi-stage build.

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
