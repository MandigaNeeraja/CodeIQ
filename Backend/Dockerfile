FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY CodeIQ.slnx ./
COPY CodeIQ.Domain/CodeIQ.Domain.csproj CodeIQ.Domain/
COPY CodeIQ.Application/CodeIQ.Application.csproj CodeIQ.Application/
COPY CodeIQ.Infrastructure/CodeIQ.Infrastructure.csproj CodeIQ.Infrastructure/
COPY CodeIQ.Api/CodeIQ.Api.csproj CodeIQ.Api/
RUN dotnet restore CodeIQ.Api/CodeIQ.Api.csproj
COPY . .
RUN dotnet publish CodeIQ.Api/CodeIQ.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CodeIQ.Api.dll"]
