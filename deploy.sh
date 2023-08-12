xbuild ./MTPSync/MTPSync.csproj

#rm --recursive ./MTPSync/bin
#rm --recursive ./MTPSync/obj

#keepass2 --plgx-create ./MTPSync

cp ./MTPSync/bin/Debug/MediaDevices.dll /usr/lib/keepass2/Plugins/MediaDevices.dll
cp ./MTPSync/bin/Debug/MTPSync.dll /usr/lib/keepass2/Plugins/MTPSync.dll
