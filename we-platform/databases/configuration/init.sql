SELECT 'CREATE DATABASE we_configuration'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'we_configuration')\gexec
