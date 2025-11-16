-- Database migration script for md-reader application
-- Execute this script manually on your PostgreSQL database

CREATE TABLE IF NOT EXISTS md_reader_documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id VARCHAR(500) NOT NULL,
    user_email VARCHAR(255) NOT NULL,
    title VARCHAR(500) NOT NULL,
    content TEXT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for better query performance
CREATE INDEX IF NOT EXISTS idx_md_reader_documents_user_id ON md_reader_documents(user_id);
CREATE INDEX IF NOT EXISTS idx_md_reader_documents_created_at ON md_reader_documents(created_at);
CREATE INDEX IF NOT EXISTS idx_md_reader_documents_updated_at ON md_reader_documents(updated_at);

