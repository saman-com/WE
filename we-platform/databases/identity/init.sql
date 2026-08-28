SELECT 'CREATE DATABASE we_identity'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'we_identity')\gexec
