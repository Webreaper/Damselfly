ARG SDKVERSION=5.0-alpine
ARG RUNTIMEVERSION=5.0-alpine

FROM mcr.microsoft.com/dotnet/core/aspnet:$RUNTIMEVERSION AS base
WORKDIR /app
EXPOSE 6363

FROM mcr.microsoft.com/dotnet/core/sdk:$SDKVERSION AS build
ARG DAMSELFLY_VERSION
RUN echo Damselfly version ${DAMSELFLY_VERSION}
WORKDIR /src
COPY Damselfly.Web/Damselfly.Web.csproj Damselfly.Web/
COPY Damselfly.Core/Damselfly.Core.csproj Damselfly.Core/
COPY Nuget.Config . 
RUN dotnet restore --configfile Nuget.Config "Damselfly.Web/Damselfly.Web.csproj"
COPY . .
WORKDIR "/src/Damselfly.Web"
RUN dotnet build "Damselfly.Web.csproj" -c Release -o /app/build /p:Version=${DAMSELFLY_VERSION}

FROM build AS publish
ARG DAMSELFLY_VERSION
RUN dotnet publish "Damselfly.Web.csproj" -c Release -o /app/publish  -r alpine-x64 /p:Version=${DAMSELFLY_VERSION} /p:PublishReadyToRun=true 

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

COPY damselfly-entrypoint.sh /
RUN ["chmod", "+x", "/damselfly-entrypoint.sh"]

ADD VERSION .

RUN apk add --no-cache msttcorefonts-installer fontconfig
RUN update-ms-fonts

ENV EXIFTOOL_VERSION=12.04

RUN apk add --no-cache perl make
RUN cd /tmp \
	&& wget http://www.sno.phy.queensu.ca/~phil/exiftool/Image-ExifTool-${EXIFTOOL_VERSION}.tar.gz \
	&& tar -zxvf Image-ExifTool-${EXIFTOOL_VERSION}.tar.gz \
	&& cd Image-ExifTool-${EXIFTOOL_VERSION} \
	&& perl Makefile.PL \
	&& make test \
	&& make install \
	&& cd .. \
	&& rm -rf Image-ExifTool-${EXIFTOOL_VERSION}

ENTRYPOINT ["sh","/damselfly-entrypoint.sh"]
