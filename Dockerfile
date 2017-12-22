FROM microsoft/dotnet:latest
WORKDIR ./app

COPY PwdLess .

# BUILD LINES
ENV ASPNETCORE_ENVIRONMENT Production

COPY ./entrypoint.sh .
RUN sed -i.bak 's/\r$//' ./entrypoint.sh
RUN chmod +x ./entrypoint.sh
CMD /bin/bash ./entrypoint.sh