SELECT 'CREATE DATABASE we_communication'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'we_communication')\gexec
