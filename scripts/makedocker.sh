# Read the version from disk.
version=`cat VERSION`

echo "**** Building Docker Damselfly"
docker build --build-arg DAMSELFLY_VERSION=$version -t damselfly . 

docker tag damselfly webreaper/damselfly:dev
docker push webreaper/damselfly:dev

echo "Damselfly docker build complete."
