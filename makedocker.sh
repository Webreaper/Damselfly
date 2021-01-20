if [ -n "$1" ]; then
  version=$1;
else
    # Read the version from disk.
    version=`cat VERSION`
fi

echo "**************************** Building Docker Damselfly ****************************"
docker build --build-arg DAMSELFLY_VERSION=$version -t damselfly . 

echo "**************************** Tagging and pushing Image ****************************"
docker tag damselfly webreaper/damselfly:dev
docker push webreaper/damselfly:dev

if [ $RELEASE == "release" ]; then 
    docker tag damselfly webreaper/damselfly:latest
    docker tag damselfly webreaper/damselfly:$version-beta
    docker push webreaper/damselfly:latest
    docker push webreaper/damselfly:$version-beta
fi
echo "Damselfly docker build complete."
