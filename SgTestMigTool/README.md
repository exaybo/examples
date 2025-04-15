Решение к тестовому заданию https://github.com/STARKOV-Group/SGTest

запуск БД и pgadmin:
docker compose up -d migdb pgadmin

После этого можно пользоваться утилитой
импорты данных:
docker compose run --rm migapp import /app/TestData/departments.tsv departments
docker compose run --rm migapp import /app/TestData/employees.tsv employees
docker compose run --rm migapp import /app/TestData/jobtitles.tsv jobtitles

вывод структуры:
docker compose run --rm migapp print

очистка таблиц:
docker compose run --rm migapp clear

тестовые данные лежат в ./TestData и пробрасываются в контейнер в /app/TestData
бд и профль pgadmin - в ./pg

