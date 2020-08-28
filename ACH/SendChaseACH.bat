rem batch file to encrypt and send file via sftp

rem create c:\ChaseACH\COSA.TEST.IN.txt
copy C:\EpicorData\Users\manager\AP_Chase.txt COSA.ACH.NACHA.PII.txt /y
TIMEOUT /T 2

gpg --yes -u "PII_Cosa_Live" --passphrase=lx4@Fx!96ePz24Zx --pinentry-mode loopback --output COSA.ACH.NACHA.PII.txt.sig --sign COSA.ACH.NACHA.PII.txt				
TIMEOUT /T 2

rem this opens a new cmd window, but then gets stuck: START /WAIT 
rem also not work: START /B /wait 
rem psftp transmissions.jpmorgan.com -i PII_Private_Live.ppk -l COSA -pw lx4@Fx!96ePz24Zx
psftp transmissions.jpmorgan.com -i PII_Private_Live.ppk -l COSA -pw lx4@Fx!96ePz24Zx -b SendChaseACH2.bat
TIMEOUT /T 2

rem cd Inbound/Encrypted
rem put COSA.ACH.NACHA.PII.txt.sig
rem TIMEOUT /T 2

rem exit
rem quit

del COSA.ACH.NACHA.PII.txt


