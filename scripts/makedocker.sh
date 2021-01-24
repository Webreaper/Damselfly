
echo "**** Building Docker Damselfly"
docker build -t damselfly . 

docker tag damselfly webreaper/damselfly:dev
docker push webreaper/damselfly:dev

echo "Damselfly docker build complete."
