# Build context: server/  (docker build -f ex1/Dockerfile .)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until a project file changes
COPY Directory.Build.props Directory.Build.targets ./
COPY ex1/ex1.csproj ex1/
COPY Lottery.Messaging/Lottery.Messaging.csproj Lottery.Messaging/
RUN dotnet restore ex1/ex1.csproj

COPY ex1/ ex1/
COPY Lottery.Messaging/ Lottery.Messaging/
RUN dotnet publish ex1/ex1.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "ex1.dll"]
