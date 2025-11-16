FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["MdReader.csproj", "./"]
RUN dotnet restore "MdReader.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "MdReader.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MdReader.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MdReader.dll"]

