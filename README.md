# Setup

This bot allows you to play some music from youtube in your discord servers.

To use this bot you should firstly create 2 following files:

1. DiscordBot/appsettings.json **with following contents**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Trace",
      "Microsoft": "Trace"
    },
    "Console": {
      "FormatterName": "CustomConsoleFormatter",
      "FormatterOptions": {
        "IncludeScopes": true,
        "TimestampFormat": "HH:mm:ss.fff",
        "UseUtcTimestamp": true,
        "SingleLine": true
      }
    }
  },
  "DiscordBotToken": "your_token",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=bot.db"
  }
}
```
2. DiscordBot/ffmpeg.exe to encode youtube audio stream to PCM format

# Commands

!play [search_query\url] 
  
!resume \ !r

!pause \ !p

!skip \ !s

!ping

!echo [text]
