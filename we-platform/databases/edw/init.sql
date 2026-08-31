SELECT 'CREATE DATABASE we_edw'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'we_edw')\gexec
