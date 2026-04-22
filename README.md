# Adeptus Boticus

Discord bot that posts Warhammer Community articles to channels.

## Running Locally

### Setup
```bash
# Clone and build
dotnet restore
dotnet build

# Create .env file
cp sample.env src/AdeptusBoticus/.env
```

Configure `.env`:
```
DISCORD_TOKEN=your_bot_token
WH40K_ID=40k_channel_id
AOS_ID=aos_channel_id
HH_ID=heresy_channel_id
BB_ID=bowl_channel_id
```

### Run
```bash
# Development
dotnet run --project src/AdeptusBoticus

# Production build
dotnet publish -c Release -r linux-x64 -o ./publish
```

## Deploy to Fly.io

### Prerequisites
- [Fly.io CLI](https://fly.io/docs/hands-on/install-fly-cli/)
- `fly auth login`

### Steps
1. **Create app**
   ```bash
   fly apps create adeptusboticus
   ```

2. **Create volume for persistent data**
   ```bash
   fly volumes create trackers_data --region iad --size 1
   ```

3. **Set secrets**
   ```bash
   fly secrets set \
     DISCORD_TOKEN="your_bot_token" \
     WH40K_ID="40k_channel_id" \
     AOS_ID="aos_channel_id" \
     HH_ID="heresy_channel_id" \
     BB_ID="bowl_channel_id"
   ```

4. **Deploy**
   ```bash
   fly deploy
   ```

### Monitor
```bash
fly logs -f
fly status
```

## License

MIT