SELECT 'CREATE DATABASE we_assessment'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'we_assessment')\gexec
