
if [ -n "$1" ]; then
    version=`cat VERSION`
    DOCKERTAG="${version}-beta"
else
    echo 'No docker tag specified. Defaulting to dev'
    DOCKERTAG='dev'
fi

echo "**** Building Docker Damselfly"
docker build -t damselfly . 

echo "*** Pushing docker image to ${DOCKERTAG}"

docker tag damselfly webreaper/damselfly:$DOCKERTAG
docker push webreaper/damselfly:$DOCKERTAG

echo "Damselfly docker build complete."
