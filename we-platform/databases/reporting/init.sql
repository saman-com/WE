SELECT 'CREATE DATABASE we_reporting'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'we_reporting')\gexec
