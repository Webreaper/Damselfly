
echo "Don't forget to run EF migrations..."

# bump version
docker run --rm -v "$PWD":/app treeder/bump patch
version=`cat VERSION`
echo "New version: $version"

docker build --build-arg DAMSELFLY_VERSION=$version -t damselfly . 

docker tag damselfly 192.168.1.120:7575/damselfly:latest
docker tag damselfly 192.168.1.120:7575/damselfly:$version
docker push 192.168.1.120:7575/damselfly:latest
docker push 192.168.1.120:7575/damselfly:$version

