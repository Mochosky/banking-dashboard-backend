# 1. Use the official .NET 10 SDK to build and publish the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore dependencies
COPY ["BankingDashboard.Api.csproj", "./"]
RUN dotnet restore "BankingDashboard.Api.csproj"

# Copy the rest of the code and publish
COPY . .
RUN dotnet publish "BankingDashboard.Api.csproj" -c Release -o /app/publish

# 2. Use the lighter ASP.NET Core runtime to run the app
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose Render's expected web port
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "BankingDashboard.Api.dll"]