SELECT 'CREATE DATABASE we_notifications'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'we_notifications')\gexec
