SELECT 'CREATE DATABASE we_national_reporting'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'we_national_reporting')\gexec
