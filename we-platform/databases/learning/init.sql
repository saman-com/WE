SELECT 'CREATE DATABASE we_learning'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'we_learning')\gexec
