# QuartService
Scheduling service written in C# .NET to run background jobs (processes); similar to the Windows task scheduler. Runs as a standard Windows
service. Data for the jobs is stored in a MS SQL Server database. Job paramaters can be edited locally in the database using the 
QuartzManager app. The server process can also update its local jobs store using JSON files containing the Job objects that need to be 
removed, added, or edited allowing the service to be deployed then updated remotely.
