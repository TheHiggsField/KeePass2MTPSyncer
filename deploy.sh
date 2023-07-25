xbuild ./MTPSyncPlugin/MTPSync/MTPSync.csproj

rm --recursive ./MTPSyncPlugin/MTPSync/bin
rm --recursive ./MTPSyncPlugin/MTPSync/obj

keepass2 --plgx-create ./MTPSyncPlugin/MTPSync

cp ./MTPSyncPlugin/MTPSync.plgx /usr/lib/keepass2/Plugins/MTPSync.plgx
