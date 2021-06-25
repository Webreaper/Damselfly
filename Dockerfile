
FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS final

WORKDIR /app
COPY /publish .
RUN chmod +x Damselfly.Web 

# Copy the entrypoint script
COPY ./damselfly-entrypoint.sh /
RUN ["chmod", "+x", "/damselfly-entrypoint.sh"]
ADD VERSION .

# Need sudo for the iNotify count increase
RUN set -ex && apk --no-cache add sudo

# Add Microsoft fonts that'll be used for watermarking
RUN apk add --no-cache msttcorefonts-installer fontconfig && update-ms-fonts

# Add ExifTool
RUN apk --update add exiftool && rm -rf /var/cache/apk/*

# Install this for onnx - per https://stackoverflow.com/questions/61407089/asp-net-core-load-an-onnx-file-inside-a-docker-container
RUN apt-get update && apt-get install -y libgomp1

EXPOSE 6363
ENTRYPOINT ["sh","/damselfly-entrypoint.sh"]
