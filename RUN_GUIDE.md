# How to Run the MD Reader Application

## Prerequisites

Make sure you have:
- ✅ .NET 9.0 SDK installed
- ✅ PostgreSQL database accessible (connection string in `.env` file)
- ✅ Database table created (run `database.sql` script)

## Step-by-Step Instructions

### Step 1: Navigate to Project Directory

Open your terminal and navigate to the project folder:

```bash
cd /Users/yonasyeneneh/Workspace/md-reader/MdReader
```

### Step 2: Verify .env File Exists

Check that your `.env` file is present:

```bash
cat .env
```

You should see:
```
PGHOST=your_host
PGPORT=5432
PGDATABASE=your_database
PGUSER=your_username
PGPASSWORD=your_password
PGSSLMODE=require
ASPNETCORE_ENVIRONMENT=Development
PORT=8080
```

### Step 3: Ensure Database Table Exists

Make sure you've run the `database.sql` script on your PostgreSQL database to create the `md_reader_documents` table.

If not, connect to your database and run:
```sql
CREATE TABLE IF NOT EXISTS md_reader_documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id VARCHAR(500) NOT NULL,
    user_email VARCHAR(255) NOT NULL,
    title VARCHAR(500) NOT NULL,
    content TEXT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_md_reader_documents_user_id ON md_reader_documents(user_id);
CREATE INDEX IF NOT EXISTS idx_md_reader_documents_created_at ON md_reader_documents(created_at);
CREATE INDEX IF NOT EXISTS idx_md_reader_documents_updated_at ON md_reader_documents(updated_at);
```

### Step 4: Restore NuGet Packages (if needed)

```bash
dotnet restore
```

### Step 5: Build the Application

```bash
dotnet build
```

You should see: `Build succeeded.`

### Step 6: Run the Application

```bash
dotnet run
```

### Step 7: Access the Application

Once running, you'll see output like:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
      Now listening on: https://localhost:5001
```

Open your web browser and navigate to:
- **HTTP**: http://localhost:5000
- **HTTPS**: https://localhost:5001 (if available)

## Quick Start (All-in-One)

```bash
cd /Users/yonasyeneneh/Workspace/md-reader/MdReader
dotnet restore
dotnet build
dotnet run
```

Then open: **http://localhost:5000**

## Troubleshooting

### Error: "Connection string 'DefaultConnection' not found"

**Solution**: Make sure your `.env` file exists and contains PostgreSQL connection variables:
```bash
cat .env | grep PGHOST
```

### Error: Database connection failed

**Solution**: 
1. Verify your database is running and accessible
2. Check your connection variables in `.env` file (PGHOST, PGDATABASE, PGUSER, PGPASSWORD)
3. Test connection manually:
   ```bash
   psql "Host=your_host;Port=5432;Database=your_database;Username=your_username;Password=your_password;SslMode=require"
   ```

### Error: Table doesn't exist

**Solution**: Run the `database.sql` script on your PostgreSQL database.

### Port Already in Use

If port 5000 is already in use, you can specify a different port:

```bash
dotnet run --urls "http://localhost:5002"
```

Or set it in `.env`:
```
PORT=5002
```

### Application Won't Start

1. Check for build errors:
   ```bash
   dotnet build
   ```

2. Check logs for specific errors

3. Verify all packages are installed:
   ```bash
   dotnet restore
   ```

## Running in Development Mode

The application runs in Development mode by default (from `.env`). This means:
- Detailed error pages
- Hot reload enabled (if using `dotnet watch`)

## Running with Hot Reload

For automatic reloading when you change code:

```bash
dotnet watch run
```

## Stopping the Application

Press `Ctrl+C` in the terminal to stop the application.

## Production Build

To create a production build:

```bash
dotnet publish -c Release -o ./publish
```

Then run:
```bash
dotnet ./publish/MdReader.dll
```

## Using Docker

If you prefer Docker:

```bash
docker build -t md-reader .
docker run -p 8080:8080 \
  -e DATABASE_URL="your_connection_string" \
  md-reader
```

## Next Steps

Once running:
1. Open http://localhost:5000 in your browser
2. Paste or upload a markdown file
3. See the live preview
4. Save documents to the database
5. Export to PDF or Word

Enjoy using MD Reader! 🎉

