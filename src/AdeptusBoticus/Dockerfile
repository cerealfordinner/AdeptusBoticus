# Use the official .NET SDK image for the build environment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the .csproj file(s) and restore any dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the code and build the application
COPY . ./
RUN dotnet publish -c Release -o out

# Use the official .NET runtime image for the final runtime environment
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

# Copy the published application from the build stage
COPY --from=build /app/out ./

# Expose port 80 for the application
EXPOSE 80

# Set the entry point for the container
ENTRYPOINT ["dotnet", "AdeptusBoticus.dll"]
