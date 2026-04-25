
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY . .
RUN dotnet publish src/API -c Release -o out
WORKDIR /app/out
ENTRYPOINT ["dotnet","API.dll"]
