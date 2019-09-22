sudo apt install curl libunwind8 gettext
curl -sSL -o dotnet.tar.gz https://download.visualstudio.microsoft.com/download/pr/6147382e-bccd-4800-ac45-19db334518ad/389c7a0554fab6994a48e546def1b636/dotnet-runtime-2.2.7-linux-arm.tar.gz
sudo mkdir -p /opt/dotnet && sudo tar zxf dotnet.tar.gz -C /opt/dotnet
sudo ln -s /opt/dotnet/dotnet /usr/local/bin
export DOTNET_ROOT=/opt/dotnet
export PATH=$PATH:/opt/dotnet

dotnet --help