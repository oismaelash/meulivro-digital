FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BookWise.API/BookWise.API.csproj", "BookWise.API/"]
COPY ["BookWise.Application/BookWise.Application.csproj", "BookWise.Application/"]
COPY ["BookWise.Domain/BookWise.Domain.csproj", "BookWise.Domain/"]
COPY ["BookWise.Infrastructure/BookWise.Infrastructure.csproj", "BookWise.Infrastructure/"]
RUN dotnet restore "BookWise.API/BookWise.API.csproj"
COPY . .
RUN dotnet build "BookWise.API/BookWise.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BookWise.API/BookWise.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookWise.API.dll"]
