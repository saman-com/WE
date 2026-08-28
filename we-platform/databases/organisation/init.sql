SELECT 'CREATE DATABASE we_organisation'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'we_organisation')\gexec
