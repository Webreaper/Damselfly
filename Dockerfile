ARG SDKVERSION=5.0-alpine
ARG RUNTIMEVERSION=5.0-alpine

# Assemble the final image
FROM mcr.microsoft.com/dotnet/aspnet:$RUNTIMEVERSION AS final
ARG DAMSELFLY_VERSION
EXPOSE 6363

WORKDIR /app
COPY --from=publish /app/publish .

# Copy the entrypoint script
COPY ./damselfly-entrypoint.sh /
RUN ["chmod", "+x", "/damselfly-entrypoint.sh"]
ADD VERSION .

# Add Microsoft fonts that'll be used for watermarking
RUN apk add --no-cache msttcorefonts-installer fontconfig && update-ms-fonts

# Add ExifTool
RUN apk --update add exiftool && rm -rf /var/cache/apk/*

# Make and install exiftool if we want a newer version
# ENV EXIFTOOL_VERSION=12.10
# RUN apk add --no-cache perl make
# RUN cd /tmp \
# 	&& wget http://www.sno.phy.queensu.ca/~phil/exiftool/Image-ExifTool-${EXIFTOOL_VERSION}.tar.gz \
# 	&& cd Image-ExifTool-${EXIFTOOL_VERSION} \
# 	&& tar -zxvf Image-ExifTool-${EXIFTOOL_VERSION}.tar.gz \
# 	&& perl Makefile.PL \
# 	&& make test \
# 	&& make install \
# 	&& cd .. \
# 	&& rm -rf Image-ExifTool-${EXIFTOOL_VERSION}

ENTRYPOINT ["sh","/damselfly-entrypoint.sh"]
