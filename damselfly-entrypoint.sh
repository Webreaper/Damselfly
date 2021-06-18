#!/bin/bash
set -e

if ! [ -z "$SYNO_THUMBS" ]; then
  echo "Synology thumbnails enabled."
  cmdlineargs="--syno"
fi;

#echo "Increasing inotify watch limit..."
#echo fs.inotify.max_user_instances=524288 | sudo tee -a /etc/sysctl.conf && sudo sysctl -p

echo "Preparing to start Damselfly...."
echo "  ./Damselfly.Web /pictures --config=/config --thumbs=/thumbs ${cmdlineargs}"

cd /app
./Damselfly.Web /pictures --config=/config --thumbs=/thumbs ${cmdlineargs}

exec "$@"

