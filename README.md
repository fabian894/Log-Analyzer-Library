# Log-Analyzer-Library
Implement the library described within the document. Then Create an API that will utilize the library project in which each endpoints of the API will execute each function of the library

US – 9 : Search Logs in directories
there is several directories that contents various logs :
• C:\AmadeoLogs
• C:\AWIErrors
• C:\Loggings

it is very important to note the above directories can be located on other drive such as D:\
Note also that the above directories can be sub directories
Regardless to their location, library should be implemented in such way that i won't cause any 
problem.
to be able to perform a test, create those directories on you pc drive, download the logs 
samples in the link below
https://www.dropbox.com/s/x1usuj26wz53c4s/Logs%20samples.zip?dl=0

Important: the sample provided do not contains all the type of errors within log file, it means 
that every entry in a log file has a date and time therefore it identify each line
note also that each line need to be compared to each other in other to be able to implement 
functionality requested within other tasks but exceptions exist.

US-12 : Counts number of unique errors per log files
See User story - 9 for the initial description
each log file content several lines of errors, sometime those errors are duplicated, the goal 
here is to count/sort errors without taking in account or counting the duplicated ones.

US-13: Counts number of duplicated errors per log files
See User story - 9 for the initial description
each log file content several lines of errors, sometime those errors are duplicated, the goal 
here is to count/sort how many errors have been duplicated

US-17: Delete archive from a period
See User story - 9 for the initial description
Delete an archive from a period, this is a recursive delete.

US-16: archive logs from a period
See User story - 9 for the initial description
Zip some log files and store them in the same directory then delete the logs zipped / archived 
the name of the zipped file should the date range e.g "10_06_2020-15_06_2020.zip" which
content the logs of that period in a target directory

US-18: upload logs on a remote server per API
See User story - 9 for the initial description
Upload logs file to a remote server using the API

US-11: Delete logs from a period
See User story - 9 for the initial description
delete logs from a period in all directories or target directory

US-10: Count Total available logs in a period
See User story - 9 for the initial description
total logs in all directories or a target directory in a period

US-15: Search logs per size
See User story - 9 for the initial description
search log per sizes, by given a size range e.g 1kb - 4kb, only work in kilo byte

US-14: Search logs per directory
See User story - 9 for the initial description
