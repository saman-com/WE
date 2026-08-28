SELECT 'CREATE DATABASE we_curriculum'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'we_curriculum')\gexec
