# Git Initialization Script for DataForge ETL
# Created: October 24, 2025

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "       DataForge ETL - Git Setup Script                 " -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

# Check if Git is installed
Write-Host "Checking Git installation..." -ForegroundColor Yellow
try {
    $gitVersion = git --version 2>&1
    Write-Host "OK Git is installed: $gitVersion" -ForegroundColor Green
}
catch {
    Write-Host "ERROR: Git is not installed or not in PATH" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install Git for Windows:" -ForegroundColor Yellow
    Write-Host "  1. Download from: https://git-scm.com/download/win" -ForegroundColor White
    Write-Host "  2. Run the installer" -ForegroundColor White
    Write-Host "  3. Restart PowerShell and run this script again" -ForegroundColor White
    Write-Host ""
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""

# Check if .git directory exists
if (Test-Path ".git") {
    Write-Host "WARNING: Git repository already initialized" -ForegroundColor Yellow
    $reinit = Read-Host "Do you want to reinitialize? (y/N)"
    if ($reinit -ne "y" -and $reinit -ne "Y") {
        Write-Host "Skipping initialization..." -ForegroundColor Yellow
    }
    else {
        Write-Host "Reinitializing Git repository..." -ForegroundColor Yellow
        Remove-Item -Path ".git" -Recurse -Force
        git init
        git branch -M main
        Write-Host "OK Repository reinitialized" -ForegroundColor Green
    }
}
else {
    Write-Host "Initializing Git repository..." -ForegroundColor Yellow
    git init
    git branch -M main
    Write-Host "OK Repository initialized" -ForegroundColor Green
}

Write-Host ""

# Configure Git user
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "Git User Configuration" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

$currentName = git config user.name 2>$null
$currentEmail = git config user.email 2>$null

if ($currentName -and $currentEmail) {
    Write-Host "Current configuration:" -ForegroundColor Yellow
    Write-Host "  Name:  $currentName" -ForegroundColor White
    Write-Host "  Email: $currentEmail" -ForegroundColor White
    Write-Host ""
    $reconfigure = Read-Host "Do you want to change these settings? (y/N)"
    
    if ($reconfigure -eq "y" -or $reconfigure -eq "Y") {
        $userName = Read-Host "Enter your name"
        $userEmail = Read-Host "Enter your email"
        git config user.name "$userName"
        git config user.email "$userEmail"
        Write-Host "OK Git user configured" -ForegroundColor Green
    }
}
else {
    Write-Host "Git user not configured. Please enter your details:" -ForegroundColor Yellow
    $userName = Read-Host "Enter your name"
    $userEmail = Read-Host "Enter your email"
    
    git config user.name "$userName"
    git config user.email "$userEmail"
    Write-Host "OK Git user configured" -ForegroundColor Green
}

Write-Host ""

# Verify configuration files
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "Verifying Configuration Files" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

$configFiles = @(
    ".gitignore",
    ".gitattributes",
    "README.md"
)

foreach ($file in $configFiles) {
    if (Test-Path $file) {
        Write-Host "OK $file exists" -ForegroundColor Green
    }
    else {
        Write-Host "ERROR: $file missing" -ForegroundColor Red
    }
}

Write-Host ""

# Show status
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "Repository Status" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

git status --short

Write-Host ""

# Offer to stage files
$stageFiles = Read-Host "Do you want to stage all files for commit? (Y/n)"
if ($stageFiles -ne "n" -and $stageFiles -ne "N") {
    Write-Host "Staging files..." -ForegroundColor Yellow
    git add .
    Write-Host "OK Files staged" -ForegroundColor Green
    Write-Host ""
    
    # Show what will be committed
    Write-Host "Files staged for commit:" -ForegroundColor Yellow
    git status --short
    Write-Host ""
    
    # Offer to commit
    $makeCommit = Read-Host "Do you want to create the initial commit? (Y/n)"
    if ($makeCommit -ne "n" -and $makeCommit -ne "N") {
        Write-Host "Creating initial commit..." -ForegroundColor Yellow
        
        $commitMessage = "Initial commit: DataForge ETL v2.0.0

- Core orchestration layer with unified configuration
- Dynamic Table Manager for runtime table mapping
- Excel Processor with parallel sheet processing
- Database Loader with validation and bulk import
- Avalonia UI (Phase 1+2 complete)
- Comprehensive logging and error handling
- Self-contained deployment support"
        
        git commit -m $commitMessage
        Write-Host "OK Initial commit created" -ForegroundColor Green
    }
}

Write-Host ""

# Remote repository setup
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "Remote Repository Setup" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

$setupRemote = Read-Host "Do you want to add a remote repository? (y/N)"
if ($setupRemote -eq "y" -or $setupRemote -eq "Y") {
    Write-Host ""
    Write-Host "Select your Git hosting service:" -ForegroundColor Yellow
    Write-Host "  1. GitHub" -ForegroundColor White
    Write-Host "  2. Azure DevOps" -ForegroundColor White
    Write-Host "  3. GitLab" -ForegroundColor White
    Write-Host "  4. Custom URL" -ForegroundColor White
    Write-Host ""
    
    $choice = Read-Host "Enter choice (1-4)"
    
    switch ($choice) {
        "1" {
            $username = Read-Host "Enter your GitHub username"
            $repoName = Read-Host "Enter repository name (default: DataForge_ETL)"
            if ([string]::IsNullOrWhiteSpace($repoName)) { $repoName = "DataForge_ETL" }
            $remoteUrl = "https://github.com/$username/$repoName.git"
        }
        "2" {
            $org = Read-Host "Enter your Azure DevOps organization"
            $project = Read-Host "Enter your project name"
            $repoName = Read-Host "Enter repository name (default: DataForge_ETL)"
            if ([string]::IsNullOrWhiteSpace($repoName)) { $repoName = "DataForge_ETL" }
            $remoteUrl = "https://dev.azure.com/$org/$project/_git/$repoName"
        }
        "3" {
            $username = Read-Host "Enter your GitLab username"
            $repoName = Read-Host "Enter repository name (default: DataForge_ETL)"
            if ([string]::IsNullOrWhiteSpace($repoName)) { $repoName = "DataForge_ETL" }
            $remoteUrl = "https://gitlab.com/$username/$repoName.git"
        }
        "4" {
            $remoteUrl = Read-Host "Enter the remote repository URL"
        }
        default {
            Write-Host "Invalid choice. Skipping remote setup." -ForegroundColor Yellow
            $remoteUrl = $null
        }
    }
    
    if ($remoteUrl) {
        Write-Host ""
        Write-Host "Adding remote: $remoteUrl" -ForegroundColor Yellow
        try {
            git remote add origin $remoteUrl
            Write-Host "OK Remote repository added" -ForegroundColor Green
            Write-Host ""
            
            $pushNow = Read-Host "Do you want to push to remote now? (y/N)"
            if ($pushNow -eq "y" -or $pushNow -eq "Y") {
                Write-Host "Pushing to remote..." -ForegroundColor Yellow
                git push -u origin main
                Write-Host "OK Pushed to remote successfully" -ForegroundColor Green
            }
            else {
                Write-Host "You can push later with: git push -u origin main" -ForegroundColor Cyan
            }
        }
        catch {
            Write-Host "ERROR: Failed to add remote: $_" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "Git Setup Complete!" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  - Review staged files: git status" -ForegroundColor White
Write-Host "  - View commit history: git log" -ForegroundColor White
Write-Host "  - Read GIT_SETUP.md for detailed instructions" -ForegroundColor White
Write-Host ""
Write-Host "Happy coding!" -ForegroundColor Cyan
Write-Host ""

Read-Host "Press Enter to exit"
