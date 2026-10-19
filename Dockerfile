# Stage 1: Build ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["PersonalFinanceApp.csproj", "./"]
RUN dotnet restore "PersonalFinanceApp.csproj"

COPY . .
RUN dotnet publish "PersonalFinanceApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Chạy ứng dụng nhẹ gọn (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PersonalFinanceApp.dll"]
