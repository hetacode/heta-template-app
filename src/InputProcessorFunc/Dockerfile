FROM mcr.microsoft.com/dotnet/sdk:5.0 AS installer-env

COPY src/Commons/. /src/Commons
COPY src/InputProcessorFunc/. /src/dotnet-function-app
RUN wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && apt update \
    && apt install apt-transport-https \ 
    && apt install dotnet-sdk-3.1 -y \
    && apt install build-essential -y \
    && wget https://github.com/edenhill/librdkafka/archive/refs/tags/v1.7.0.tar.gz \
    && tar xfz v1.7.0.tar.gz \
    && cd librdkafka-1.7.0 \
    && ./configure --prefix /usr && make && make install
RUN cd /src/dotnet-function-app && \
    mkdir -p /home/site/wwwroot && \
    dotnet publish *.csproj --output /home/site/wwwroot

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/dotnet-isolated:3.0-dotnet-isolated5.0-appservice
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:3.0-dotnet-isolated5.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    LD_LIBRARY_PATH=/usr/lib

COPY --from=installer-env /usr/lib/librdkafka* /usr/lib/
COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]