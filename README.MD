# AI Assistant

## Description

Why did we create this project? 
Well, we were sitting around one day, sipping our coffees, and thought, "You know what the world needs? 
Another AI project!" So, here we are.
But in all seriousness, we created this project to streamline and automate the process of updating comments. 
We hope it brings a smile to your face and a sigh of relief to your workload.

## Core Features

- **Uploading Zip**: Just zip your code, upload it, and let the AI do the rest.
- **Generating Comments**: Ever feel like you're spending more time commenting your code than writing it? Our AI Assistant can generate comments for you, giving you more time to focus on what really matters - writing great code.

## Future Features

- **Uploading Repo**: Tired of manually uploading files? Our AI Assistant has got you covered. Just provide the GitLab or GitHub project ID you wish to improve and let the AI do the rest.
- **Git Suggestions**: Our AI Assistant will provide newly generated comments in your GitLab or GitHub repo. It will even create a merge request for you!

## Getting Started

### Prerequisites

- Setup launchsettings (default based on `launchSettings.example.json` should be fine).
- Setup appsettings (use `appsettings.example.json` and fill in the blanks).
- AzureBlobStorage isn't being used right now, but it's there for future use.
- In the settings section, the path should be absolute.
- Format of `ConnectionStrings.DefaultConnection`:
```bash
  "ConnectionStrings": {
    "DefaultConnection": "Data Source={path to db file};"
```

Database Setup
The database is setup using Entity Framework Core. To setup the database, run the following commands:
- Install EF Core tools:
```bash
dotnet tool install --global dotnet-ef
```

- Update database with migrations:
```bash
dotnet ef database update
```