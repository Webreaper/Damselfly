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

echo "Damselfly docker build complete."
