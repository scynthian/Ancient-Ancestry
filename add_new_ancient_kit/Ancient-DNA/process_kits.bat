@echo off
del /F /Q data\*
del /F /Q ibd\*

echo Initializing ...
copy ancient-kits\* data > NUL

echo Finding CA - Level 1
ibdcsfast.exe
del /F /Q data\*
renkits.exe
copy ibd\* data > NUL
del /F /Q ibd\*

rem -- try a couple of times

echo Finding CA - Level 2
ibdcsfast.exe
renkits.exe
copy /Y ibd\* data > NUL
del /F /Q ibd\*

echo Finding CA - Level 3
ibdcsfast.exe
renkits.exe
copy /Y ibd\* data > NUL
del /F /Q ibd\*

echo Finding CA - Level 4
ibdcsfast.exe
renkits.exe
copy /Y ibd\* data > NUL
del /F /Q ibd\*

rem ---
echo Removing Duplicates ...
copy data\* ibd > NUL
del /F /Q data\*
renkits.exe

echo Preparing List ...
preparelist.exe

echo Generating XML ...
genxml.exe

echo Generating ZIP file ...
cd ibd
..\7z.exe a ..\ibd.zip *
cd ..

echo Generating SNP List ...
getsnplist.exe

echo Compressing ...
gzip snps_list.txt

echo "All Done! - atree.xml, ibd.zip and snps_list.txt.gz generated successfully."
