REM Need to have choco installed.
REM Install putty for trabsfer of data.
choco install putty

REM Generate ssh. Optional if you don't have ssh
ssh-keygen -t rsa

REM send my public ssh key to rasbery so I can ssh without entering password
type %USERPROFILE%\.ssh\id_rsa.pub | plink -ssh -pw %1 pi@raspi4 "umask 077; test -d .ssh || mkdir .ssh ; cat >> .ssh/authorized_keys"

REM Install dotnet runtime on the raspbery
plink -ssh -pw %1 pi@raspi4 -m install_dotnet.sh

REM install dotnet remote debuger on the raspbery
plink -ssh -pw %1 pi@raspi4 "curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -r linux-arm -v latest -l ~/vsdbg"

REM Create working directory
plink -ssh -pw %1 pi@raspi4 "mkdir app"

REM Install vide to image app fswebcam
plink -ssh -pw %1 pi@raspi4 "sudo apt install v4l-utils libc6-dev libgdiplus libx11-dev"

REM configure ppk.Manual step here
puttygen %USERPROFILE%\.ssh\id_rsa -o %USERPROFILE%\.ssh\id_rsa.ppk -O private