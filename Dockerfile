
FROM webreaper/damselfly-base:latest AS final

WORKDIR /app
COPY /publish .
RUN chmod +x Damselfly.Web 

# Copy the entrypoint script
COPY ./damselfly-entrypoint.sh /
RUN ["chmod", "+x", "/damselfly-entrypoint.sh"]
ADD VERSION .

EXPOSE 6363
ENTRYPOINT ["sh","/damselfly-entrypoint.sh"]
