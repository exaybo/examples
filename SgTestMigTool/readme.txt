/bin/bash
help
dotnet bin/Debug/net9.0/MigApp.dll

import deps
dotnet bin/Debug/net9.0/MigApp.dll import TestData/departments.tsv departments

import emps
dotnet bin/Debug/net9.0/MigApp.dll import TestData/employees.tsv employees

import jobs
dotnet bin/Debug/net9.0/MigApp.dll import TestData/jobtitles.tsv jobtitles

print
dotnet bin/Debug/net9.0/MigApp.dll print