# Adeptus Boticus

A Discord bot that monitors Warhammer Community articles and posts updates to configured channels based on game system categories (Warhammer 40,000, Age of Sigmar, Horus Heresy, and Blood Bowl).

## Features

- Monitors Warhammer Community API for new articles
- Automatically posts articles to Discord channels based on configured categories
- Rich embeds with article thumbnails, titles, excerpts, and links
- Persists article timestamps to prevent duplicate posts
- Configurable check intervals and data storage
- Multi-channel support with category filtering

## Prerequisites

- **.NET 10.0 SDK** - [Download from Microsoft](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Discord Bot Token** - Create a bot application at [Discord Developer Portal](https://discord.com/developers/applications)
- **Discord Channel IDs** - The IDs of channels where you want posts to appear

## Installation

### 1. Clone the Repository

```bash
git clone <your-repo-url>
cd AdeptusBoticus
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Create Environment Variables

Copy the sample environment file to the project directory and configure it:

```bash
cp sample.env src/AdeptusBoticus/.env
```

Edit `src/AdeptusBoticus/.env` with your values:

| Variable | Description | Required | Default |
|----------|-------------|----------|---------|
| `DISCORD_TOKEN` | Your Discord bot token | Yes | - |
| `WH40K_ID` | Channel ID for Warhammer 40,000 articles | Yes* | - |
| `AOS_ID` | Channel ID for Age of Sigmar articles | Yes* | - |
| `HH_ID` | Channel ID for Horus Heresy articles | Yes* | - |
| `BB_ID` | Channel ID for Blood Bowl articles | Yes* | - |
| `RSS_CHECK_INTERVAL_MS` | How often to check for new articles (ms) | No | 300000 (5 min) |
| `DATA_FILE_PATH` | Path to store tracking data | No | ./data/trackers.json |

*Only required for categories you want to use

## Running Locally

### Development

```bash
dotnet run --project src/AdeptusBoticus
```

### Build for Production

```bash
dotnet publish src/AdeptusBoticus -c Release -r linux-x64 --self-contained -o ./publish
```

## Hosting on Linux

### 1. Publish the Application

```bash
dotnet publish src/AdeptusBoticus -c Release -r linux-x64 --self-contained -o ./publish
```

### 2. Deploy to Server

```bash
scp -r publish/* user@your-server:/opt/adeptusboticus/
```

### 3. Configure Environment Variables

SSH into your server and create the `.env` file:

```bash
ssh user@your-server
cd /opt/adeptusboticus
nano .env
```

Add your environment variables (see [Environment Variables](#environment-variables) above).

### 4. Set Permissions

```bash
chmod +x AdeptusBoticus
```

### 5. Create Systemd Service

Create `/etc/systemd/system/adeptusboticus.service`:

```ini
[Unit]
Description=Adeptus Bot Discord Bot
After=network.target

[Service]
Type=simple
User=your_user
WorkingDirectory=/opt/adeptusboticus
ExecStart=/opt/adeptusboticus/AdeptusBoticus
Restart=on-failure
RestartSec=10

[Install]
WantedBy=multi-user.target
```

### 6. Enable and Start the Service

```bash
sudo systemctl daemon-reload
sudo systemctl enable adeptusboticus
sudo systemctl start adeptusboticus
sudo systemctl status adeptusboticus
```

## Configuration

### Discord Bot Setup

1. Go to the [Discord Developer Portal](https://discord.com/developers/applications)
2. Create a new application and add a bot
3. Copy your bot token to the `DISCORD_TOKEN` environment variable
4. Under **Bot Permissions**, grant:
   - `Send Messages`
   - `Embed Links`
   - `Read Message History`
5. Invite the bot to your server with the required permissions

### Category Mapping

The bot maps articles to channels based on topics:

| Channel | Matched Topics |
|---------|----------------|
| WH40K | Warhammer 40,000, 40k, Kill Team |
| AOS | Warhammer Age of Sigmar, The Old World, Old World Almanack, Arcane Journal, AoS |
| HH | The Horus Heresy, Warhammer: The Horus Heresy, The Horus Heresy News |
| BB | Blood Bowl |

## Development

### Running Tests

```bash
dotnet test
```

### Project Structure

```
src/AdeptusBoticus/
├── Data/           # Data persistence layer
├── Models/         # Domain models
├── Services/       # Business logic and external service clients
└── Extensions/     # Extension methods

tests/AdeptusBoticus.Tests/
└── Unit and integration tests
```

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- [Warhammer Community](https://www.warhammer-community.com/) for providing the article API
- [DSharpPlus](https://dsharpplus.github.io/) for the Discord library
- [Serilog](https://serilog.net/) for structured logging
