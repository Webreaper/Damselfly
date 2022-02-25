
if [ -z "$1" ]; then
    echo 'No docker tag specified. Pushing to dev'
    DOCKERTAG='dev'

    echo "**** Building Docker Damselfly using dev base image"
    docker build -t damselfly --build-arg BASE_IMAGE=webreaper/damselfly-base:dev . 
else
    version=`cat VERSION`
    DOCKERTAG="${version}-beta"
    echo "Master specified - creating tag: ${DOCKERTAG}"

    echo "**** Building Docker Damselfly"
    docker build -t damselfly . 
fi


echo "*** Pushing docker image to webreaper/damselfly:${DOCKERTAG}"

docker tag damselfly webreaper/damselfly:$DOCKERTAG
docker push webreaper/damselfly:$DOCKERTAG

if [ -n "$1" ]
then
    echo "*** Pushing docker image to webreaper/damselfly:latest"
    docker tag damselfly webreaper/damselfly:latest
    docker push webreaper/damselfly:latest
fi

echo "Damselfly docker build complete."
