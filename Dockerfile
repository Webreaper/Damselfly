
FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS final

WORKDIR /app
COPY /publish .
RUN chmod +x Damselfly.Web 

# Copy the entrypoint script
COPY ./damselfly-entrypoint.sh /
RUN ["chmod", "+x", "/damselfly-entrypoint.sh"]
ADD VERSION .

# Install this for onnx - per https://stackoverflow.com/questions/61407089/asp-net-core-load-an-onnx-file-inside-a-docker-container
RUN set -ex && apk add --no-cache sudo exiftool libgomp libx11 libstdc++ 
RUN apk add libc-dev libgdiplus-dev --update-cache --repository http://dl-3.alpinelinux.org/alpine/edge/testing/ --allow-untrusted

# Add Microsoft fonts that'll be used for watermarking
RUN apk add --no-cache msttcorefonts-installer fontconfig && update-ms-fonts

EXPOSE 6363
ENTRYPOINT ["sh","/damselfly-entrypoint.sh"]
