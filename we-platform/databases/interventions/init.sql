SELECT 'CREATE DATABASE we_interventions'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'we_interventions')\gexec
