# Настройка nuget.config

Для доступа к приватным GitHub Packages нужно:

1. Скопировать `nuget.config.example` в `nuget.config`:
   ```bash
   cp nuget.config.example nuget.config
   ```

2. Отредактировать `nuget.config` и заменить:
   - `YOUR_GITHUB_USERNAME` на ваш GitHub username
   - `YOUR_PERSONAL_ACCESS_TOKEN` на ваш Personal Access Token с правами `read:packages`

3. Или использовать команду dotnet для настройки:
   ```bash
   dotnet nuget update source github \
     --username YOUR_GITHUB_USERNAME \
     --password YOUR_PERSONAL_ACCESS_TOKEN \
     --store-password-in-clear-text
   ```

**Важно**: Файл `nuget.config` с токеном находится в `.gitignore` и не будет закоммичен.
