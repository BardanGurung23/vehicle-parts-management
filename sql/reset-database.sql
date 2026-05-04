-- Script to drop and recreate the vpims database
-- Run this as a PostgreSQL superuser (postgres)

-- Terminate all connections to the database
SELECT pg_terminate_backend(pg_stat_activity.pid)
FROM pg_stat_activity
WHERE pg_stat_activity.datname = 'vpims'
  AND pid <> pg_backend_pid();

-- Drop the database if it exists
DROP DATABASE IF EXISTS vpims;

-- Create the database
CREATE DATABASE vpims
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.UTF-8'
    LC_CTYPE = 'en_US.UTF-8'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;

-- Connect to the new database and run the schema
\c vpims

-- Now run the basic-sql.sql schema
\i basic-sql.sql
