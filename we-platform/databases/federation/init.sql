SELECT 'CREATE DATABASE we_federation'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'we_federation')\gexec
