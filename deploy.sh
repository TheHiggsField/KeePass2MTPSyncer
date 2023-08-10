xbuild ./MTPSync/MTPSync.csproj

rm --recursive ./MTPSync/bin
rm --recursive ./MTPSync/obj

keepass2 --plgx-create ./MTPSync

cp ./MTPSync.plgx /usr/lib/keepass2/Plugins/MTPSync.plgx
