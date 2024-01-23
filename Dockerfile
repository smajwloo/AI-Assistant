FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

ENV ASPNETCORE_URLS=http://+:80

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:7.0 AS prebuild
ARG configuration=Release
WORKDIR /src
COPY ["./Core/AiaApi.csproj", "Core/"]
RUN dotnet restore "Core/AiaApi.csproj"
COPY . .

FROM prebuild AS test
WORKDIR /src/TestProject
CMD ["sh", "-c", "dotnet test --logger \"console;verbosity=detailed\""]

FROM prebuild AS build
WORKDIR "/src/Core"
RUN dotnet build "AiaApi.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "AiaApi.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AiaApi.dll"]

