docker compose up -d migdb pgadmin
docker compose run --rm migapp import /app/TestData/departments.tsv departments
docker compose run --rm migapp import /app/TestData/employees.tsv employees
docker compose run --rm migapp import /app/TestData/jobtitles.tsv jobtitles
docker compose run --rm migapp print
docker compose run --rm migapp clear

