# Build context: server/  (docker build -f Lottery.NotificationWorker/Dockerfile .)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Build.targets ./
COPY Lottery.NotificationWorker/Lottery.NotificationWorker.csproj Lottery.NotificationWorker/
COPY Lottery.Messaging/Lottery.Messaging.csproj Lottery.Messaging/
RUN dotnet restore Lottery.NotificationWorker/Lottery.NotificationWorker.csproj

COPY Lottery.NotificationWorker/ Lottery.NotificationWorker/
COPY Lottery.Messaging/ Lottery.Messaging/
RUN dotnet publish Lottery.NotificationWorker/Lottery.NotificationWorker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app .
USER app
ENTRYPOINT ["dotnet", "Lottery.NotificationWorker.dll"]
