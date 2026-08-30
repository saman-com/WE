SELECT 'CREATE DATABASE we_mastery'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'we_mastery')\gexec
