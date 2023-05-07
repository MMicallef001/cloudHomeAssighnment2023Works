FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build-env
WORKDIR /app

COPY TranscribeService/TranscribeService.csproj TranscribeService/
COPY Common/Common.csproj Common/
RUN dotnet restore TranscribeService/TranscribeService.csproj

COPY . ./
RUN dotnet publish TranscribeService -c Release -o out
 
FROM mcr.microsoft.com/dotnet/aspnet:3.1
WORKDIR /app
EXPOSE 80
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "TranscribeService.dll"]