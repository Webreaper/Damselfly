ARG BASE_IMAGE=webreaper/damselfly-base:1.9.1

FROM $BASE_IMAGE as final

WORKDIR /app
COPY /Models ./Models
COPY /desktop ./wwwroot/desktop
COPY /publish .
RUN chmod +x Damselfly.Web.Server

# optional if we want to strace the CLR startup
# RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get --no-install-recommends install -y strace

# Copy the entrypoint script
COPY ./damselfly-entrypoint.sh /
RUN ["chmod", "+x", "/damselfly-entrypoint.sh"]
ADD VERSION .

EXPOSE 6363
ENTRYPOINT ["sh","/damselfly-entrypoint.sh"]
