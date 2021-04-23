rem batch file to rename extrel's chase ach file

D:

cd ACH_Files

rem ren AP_Chase_Extrel.txt AP_Chase_Extrel_x.txt

rem ren AP_Chase_Extrel.txt AP_Chase_Extrel__%date:~10,4%%date:~4,2%%date:~7,2%_%time:~0,2%%time:~3,2%%time:~6,2%.txt
ren AP_Chase_Extrel.txt AP_Chase_Extrel_%date:~4,2%-%date:~7,2%-%date:~10,4%_%time:~0,2%-%time:~3,2%-%time:~6,2%.txt


