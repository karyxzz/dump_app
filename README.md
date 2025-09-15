Create a function app to get data from snowflake: 

Step 1: Quick Setup in Visual Studio Community
Create New Project: File → New → Project → Azure Functions

Select Template: HTTP trigger with .NET 8 Isolated

Install NuGet Package: Right-click Dependencies → Manage NuGet → Search "Snowflake.Data" → Install

Step 2: Simple Configuration
local.settings.json

Step 3: Simple Function Code
Program.cs (add if missing)

Step 4: Test It
Run Locally: Press F5 in Visual Studio

Test URL: http://localhost:7071/api/GetData?table=YOUR_TABLE_NAME

View Console: Check Visual Studio output window for logged data

Step 5: Deploy to Azure
Right-click project → Publish

Choose Azure Function App

Add your connection string in Azure Portal → Function App → Configuration
