SELECT 'CREATE DATABASE we_evidence'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'we_evidence')\gexec
