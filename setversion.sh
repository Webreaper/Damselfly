if [ -n "$1" ]; then
  version=$1;
else
    # Read the version from disk.
    version=`cat VERSION`
fi

