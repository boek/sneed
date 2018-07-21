FROM microsoft/dotnet:2.1-sdk

WORKDIR /app

COPY *.fsproj ./
RUN dotnet restore

# copy and build everything else
COPY . ./
RUN dotnet publish -c Release -o out


RUN apt-get update
RUN apt-get install -y cron

# Add crontab file in the cron directory
ADD crontab /etc/cron.d/sneed-cron
 
# Give execution rights on the cron job
RUN chmod 0644 /etc/cron.d/sneed-cron
 

# Create log file
RUN touch /var/log/cron.log
RUN chmod 0666 /var/log/cron.log

# Volume required for tail command
VOLUME /var/log
 
# Run the command on container startup
CMD cron && tail -f /var/log/cron.log