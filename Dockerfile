ARG BASE_IMAGE=webreaper/damselfly-base:2.1.2

FROM $BASE_IMAGE AS final

WORKDIR /app
COPY /Models ./Models
COPY /publish/**/Microsoft.* .
COPY /publish/**/System.* .
COPY --exclude=Microsoft.* --exclude=System.* /publish .
RUN chmod +x Damselfly.Web.Server

# optional if we want to strace the CLR startup
# RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get --no-install-recommends install -y strace

# Copy the entrypoint script
COPY ./damselfly-entrypoint.sh /
RUN ["chmod", "+x", "/damselfly-entrypoint.sh"]
ADD VERSION .

EXPOSE 6363
ENTRYPOINT ["sh","/damselfly-entrypoint.sh"]
