SELECT 'CREATE DATABASE we_gaps'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'we_gaps')\gexec
