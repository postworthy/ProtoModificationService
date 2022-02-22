#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

#Install protoc which will be used to get the initial file processed by this tool
#RUN apt-get update && apt-get install -y protobuf-compiler
RUN apt-get update && apt-get install -y wget unzip
RUN wget https://github.com/protocolbuffers/protobuf/releases/download/v3.13.0/protoc-3.13.0-linux-x86_64.zip
RUN unzip protoc-3.13.0-linux-x86_64.zip -d ./protoc-3.13.0-linux-x86_64
RUN cp protoc-3.13.0-linux-x86_64/bin/protoc /usr/bin/protoc
RUN rm -rf protoc-3.13.0-linux-x86_64



FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["ProtoModificationService/ProtoModificationService.csproj", "ProtoModificationService/"]
RUN dotnet restore "ProtoModificationService/ProtoModificationService.csproj"
COPY . .
WORKDIR "/src/ProtoModificationService"
RUN dotnet build "ProtoModificationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProtoModificationService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProtoModificationService.dll"]