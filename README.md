# Markdown Reader Application

A simple, modern web application for reading, editing, and managing markdown files with real-time preview and export capabilities.

## Features

- **GitHub Authentication**: Secure login with GitHub OAuth
- **Markdown Editor**: Paste or upload `.md` files with live preview
- **Resizable Panels**: Adjustable split view for input and preview
- **Toggleable Sidebar**: Modern document list with smooth animations
- **Document Management**: Save, load, and delete documents
- **Document Limits**: 500 documents per user (unlimited for admin)
- **Export Options**: Export to PDF or Word format
- **PostgreSQL Storage**: All documents stored in PostgreSQL database
- **Bootstrap UI**: Modern, responsive interface with dark mode
- **Docker Support**: Containerized deployment ready

## Tech Stack

- **Framework**: ASP.NET Core MVC (.NET 9.0)
- **Database**: PostgreSQL (external instance)
- **Markdown Rendering**: Markdig
- **PDF Export**: QuestPDF
- **Word Export**: DocX
- **UI**: Bootstrap 5

## Prerequisites

- .NET 9.0 SDK
- PostgreSQL database (external instance)
- GitHub OAuth App credentials
- Docker (optional, for containerized deployment)

## Setup

### 1. Database Setup

Execute the SQL script `database.sql` on your PostgreSQL database:

```sql
-- Run database.sql on your PostgreSQL instance
```

### 2. GitHub OAuth Setup

1. Go to [GitHub Developer Settings](https://github.com/settings/developers)
2. Click "New OAuth App"
3. Fill in the application details:
   - **Application name**: Markdown Reader (or your preferred name)
   - **Homepage URL**: `http://localhost:8080` (for local development)
   - **Authorization callback URL**: `http://localhost:8080/signin-github` (for local development)
4. Click "Register application"
5. Copy the **Client ID** and generate a **Client Secret**
6. Save these credentials for the next step

### 3. Environment Variables

Create a `.env` file in the `MdReader` directory with your configuration:

```bash
# PostgreSQL Database Configuration
PGHOST=your_host
PGPORT=5432
PGDATABASE=your_database
PGUSER=your_username
PGPASSWORD=your_password
PGSSLMODE=require

# GitHub OAuth Configuration
GITHUB_CLIENT_ID=your_github_client_id
GITHUB_CLIENT_SECRET=your_github_client_secret

# Application Configuration
ASPNETCORE_ENVIRONMENT=Development
PORT=8080
```

**Note**: Never commit the `.env` file to version control. See `.env.example` for a template.

### 4. Database Migration

After setting up GitHub OAuth, run the migration script to clean up guest documents:

```bash
# Connect to your PostgreSQL database and run:
psql -h your_host -U your_username -d your_database -f database_migration.sql
```

This will delete all documents created by guest users before authentication was implemented.

### 5. Local Development

```bash
cd MdReader
dotnet restore
dotnet run
```

The application will be available at `http://localhost:5000`

### 6. Docker Build

```bash
docker build -t md-reader .
docker run -p 8080:8080 \
  -e PGHOST="your_host" \
  -e PGPORT="5432" \
  -e PGDATABASE="your_database" \
  -e PGUSER="your_username" \
  -e PGPASSWORD="your_password" \
  -e PGSSLMODE="require" \
  -e GITHUB_CLIENT_ID="your_github_client_id" \
  -e GITHUB_CLIENT_SECRET="your_github_client_secret" \
  md-reader
```

## Deployment on Render.com

1. Connect your GitHub repository to Render
2. Create a new Web Service
3. Set the following environment variables in Render dashboard:
   - `PGHOST`: Your PostgreSQL host
   - `PGPORT`: `5432`
   - `PGDATABASE`: Your database name
   - `PGUSER`: Your database username
   - `PGPASSWORD`: Your database password
   - `PGSSLMODE`: `require`
   - `GITHUB_CLIENT_ID`: Your GitHub OAuth Client ID
   - `GITHUB_CLIENT_SECRET`: Your GitHub OAuth Client Secret
   - `ASPNETCORE_ENVIRONMENT`: `Production`
   - `PORT`: Render will set this automatically

4. **Important**: Update your GitHub OAuth App callback URL to match your Render deployment URL:
   - Go to GitHub Developer Settings → Your OAuth App
   - Update "Authorization callback URL" to: `https://your-app.onrender.com/signin-github`

5. Set Build Command: `dotnet publish -c Release -o ./publish`
6. Set Start Command: `dotnet ./publish/MdReader.dll`

## Project Structure

```
MdReader/
├── Controllers/
│   ├── HomeController.cs      # Main page controller
│   ├── DocumentController.cs  # Document CRUD API
│   ├── AccountController.cs    # Authentication
│   └── ExportController.cs     # Export functionality
├── Models/
│   └── Document.cs             # Document entity
├── Services/
│   ├── DocumentService.cs     # Document business logic
│   └── ExportService.cs        # Export functionality
├── Data/
│   └── ApplicationDbContext.cs # EF Core context
├── Views/
│   ├── Home/
│   │   └── Index.cshtml        # Main editor UI
│   └── Shared/
│       └── _Layout.cshtml      # Layout template
├── wwwroot/
│   ├── css/
│   │   └── site.css            # Custom styles
│   └── js/
│       └── app.js              # Client-side logic
├── Dockerfile                  # Docker configuration
└── database.sql                # Database schema
```

## API Endpoints

### Documents
- `GET /api/Document` - Get all documents for current user
- `GET /api/Document/{id}` - Get specific document
- `POST /api/Document` - Save or update document
- `DELETE /api/Document/{id}` - Delete document

### Export
- `POST /api/Export/pdf` - Export to PDF
- `POST /api/Export/word` - Export to Word

## Security

- **GitHub OAuth Authentication**: All users must authenticate with GitHub to use the application
- **User-scoped Documents**: Users can only access their own documents
- **Document Limits**: 500 documents per user (unlimited for `expertsanoy@gmail.com`)
- **SQL Injection Prevention**: EF Core parameterization
- **File Upload Validation**: Only `.md` files are accepted
- **HTTPS Enforcement**: Required in production

## License

MIT License

