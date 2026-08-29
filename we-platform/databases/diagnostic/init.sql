SELECT 'CREATE DATABASE we_diagnostic'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'we_diagnostic')\gexec
