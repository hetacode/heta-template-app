FROM mcr.microsoft.com/dotnet/sdk:5.0 AS installer-env
RUN wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && apt update \
    && apt install apt-transport-https \ 
    && apt install dotnet-sdk-3.1 -y
COPY . /src/dotnet-function-app
RUN cd /src/dotnet-function-app && \
    mkdir -p /home/site/wwwroot && \
    dotnet publish *.csproj --output /home/site/wwwroot

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/dotnet-isolated:3.0-dotnet-isolated5.0-appservice
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:3.0-dotnet-isolated5.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    LD_LIBRARY_PATH=/home/site/wwwroot/runtimes/ubuntu.18.04-x64/native

COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]