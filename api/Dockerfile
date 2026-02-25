FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/ src/
RUN dotnet restore src/KamSquare.KamScore.Api/KamSquare.KamScore.Api.csproj
RUN dotnet publish src/KamSquare.KamScore.Api/KamSquare.KamScore.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "KamSquare.KamScore.Api.dll"]
