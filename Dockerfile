ARG SDKVERSION=5.0-alpine
ARG RUNTIMEVERSION=5.0-alpine

FROM mcr.microsoft.com/dotnet/aspnet:$RUNTIMEVERSION AS base
WORKDIR /app
EXPOSE 6363

# First, build the desktop app
FROM node as desktop
ARG DAMSELFLY_VERSION
RUN echo Damselfly Desktop version ${DAMSELFLY_VERSION}
RUN apt-get update && apt-get install -y zip
COPY Damselfly.Desktop Damselfly.Desktop
WORKDIR "/Damselfly.Desktop"
RUN yarn install &&  yarn version --new-version ${DAMSELFLY_VERSION} && yarn dist
WORKDIR "/Damselfly.Desktop/dist"
RUN zip /damselfly-macos.zip *.dmg

# Now build the app itself
FROM mcr.microsoft.com/dotnet/sdk:$SDKVERSION AS build
ARG DAMSELFLY_VERSION
RUN echo Damselfly Server version ${DAMSELFLY_VERSION}
WORKDIR /src
COPY Damselfly.Web/Damselfly.Web.csproj Damselfly.Web/
COPY Damselfly.Core/Damselfly.Core.csproj Damselfly.Core/
COPY Nuget.Config . 
RUN dotnet restore --configfile Nuget.Config "Damselfly.Web/Damselfly.Web.csproj"
COPY . .
WORKDIR "/src/Damselfly.Web"
RUN dotnet build "Damselfly.Web.csproj" -c Release -o /app/build /p:Version=${DAMSELFLY_VERSION}

# Now run the publish
FROM build AS publish
ARG DAMSELFLY_VERSION
RUN dotnet publish "Damselfly.Web.csproj" -c Release -o /app/publish  -r alpine-x64 /p:Version=${DAMSELFLY_VERSION} /p:PublishReadyToRun=true 

# Assemble the final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=desktop /damselfly-macos.zip ./wwwroot/desktop

COPY damselfly-entrypoint.sh /
RUN ["chmod", "+x", "/damselfly-entrypoint.sh"]

ADD VERSION .

# Add Microsoft fonts that'll be used for watermarking
RUN apk add --no-cache msttcorefonts-installer fontconfig
RUN update-ms-fonts

# Make and install exiftool (could use APK as below but it is often out of date 
# so don't use this for now)
# RUN apk --update add exiftool && rm -rf /var/cache/apk/*

ENV EXIFTOOL_VERSION=12.10
RUN apk add --no-cache perl make
RUN cd /tmp \
	&& wget http://www.sno.phy.queensu.ca/~phil/exiftool/Image-ExifTool-${EXIFTOOL_VERSION}.tar.gz \
	&& cd Image-ExifTool-${EXIFTOOL_VERSION} \
	&& tar -zxvf Image-ExifTool-${EXIFTOOL_VERSION}.tar.gz \
	&& perl Makefile.PL \
	&& make test \
	&& make install \
	&& cd .. \
	&& rm -rf Image-ExifTool-${EXIFTOOL_VERSION}

ENTRYPOINT ["sh","/damselfly-entrypoint.sh"]
