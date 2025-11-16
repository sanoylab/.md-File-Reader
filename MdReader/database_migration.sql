-- Migration script to delete all guest-user documents
-- Run this script after implementing GitHub OAuth authentication

-- Delete all documents created by guest users
DELETE FROM md_reader_documents 
WHERE user_id = 'guest-user';

-- Verify deletion (optional - check count before and after)
-- SELECT COUNT(*) FROM md_reader_documents WHERE user_id = 'guest-user';

