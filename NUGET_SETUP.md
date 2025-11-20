# Настройка nuget.config

Для доступа к приватным GitHub Packages нужно:

1. Создать Personal Access Token на GitHub с правами `read:packages`
2. Добавить credentials в nuget.config:

```xml
<packageSourceCredentials>
  <github>
    <add key="Username" value="YOUR_GITHUB_USERNAME" />
    <add key="ClearTextPassword" value="YOUR_PERSONAL_ACCESS_TOKEN" />
  </github>
</packageSourceCredentials>
```

Для локальной разработки сейчас используется ProjectReference на WorkTrack.Common.Messaging.
