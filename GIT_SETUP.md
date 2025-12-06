# Git Setup Instructions for DataForge ETL

## Prerequisites

### Install Git for Windows
1. Download Git from: https://git-scm.com/download/win
2. Run the installer
3. During installation, select:
   - ✅ Git from the command line and also from 3rd-party software
   - ✅ Use Visual Studio Code as Git's default editor (or your preferred editor)
   - ✅ Override the default branch name: `main`
   - ✅ Git Credential Manager

### Verify Installation
```powershell
# Open a new PowerShell window and run:
git --version
```

You should see output like: `git version 2.x.x.windows.x`

---

## Initial Repository Setup

### Step 1: Initialize Git Repository

```powershell
# Navigate to project directory
cd F:\Projects-Hub\DataForge_ETL

# Initialize Git repository
git init

# Set default branch to main
git branch -M main
```

### Step 2: Configure Git User

```powershell
# Set your name and email (required for commits)
git config user.name "Your Name"
git config user.email "your.email@example.com"

# Optional: Set these globally for all repositories
git config --global user.name "Your Name"
git config --global user.email "your.email@example.com"
```

### Step 3: Verify Configuration Files

The following files have been created:
- ✅ `.gitignore` - Excludes build artifacts, logs, and sensitive data
- ✅ `.gitattributes` - Ensures consistent line endings
- ✅ `README.md` - Comprehensive documentation

### Step 4: Review Files to be Committed

```powershell
# Check status
git status

# You should see:
# - Core source files (.cs, .csproj, .sln)
# - Configuration templates (appsettings.json)
# - Documentation (README.md, DEPLOYMENT_README.md)
# - Build scripts (.bat, .ps1)
```

### Step 5: Stage All Files

```powershell
# Add all files (respecting .gitignore)
git add .

# Review what will be committed
git status
```

### Step 6: Create Initial Commit

```powershell
# Create the first commit
git commit -m "Initial commit: DataForge ETL v2.0.0

- Core orchestration layer with unified configuration
- Dynamic Table Manager for runtime table mapping
- Excel Processor with parallel sheet processing
- Database Loader with validation and bulk import
- Avalonia UI (Phase 1+2 complete)
- Comprehensive logging and error handling
- Self-contained deployment support"
```

---

## Connect to Remote Repository (GitHub/Azure DevOps)

### Option A: GitHub

1. **Create a new repository on GitHub:**
   - Go to https://github.com/new
   - Name: `DataForge_ETL`
   - Description: "Enterprise ETL system for Excel to SQL Server"
   - Visibility: Public or Private
   - **DO NOT** initialize with README, .gitignore, or license (we already have these)

2. **Connect and push:**
   ```powershell
   # Add remote repository
   git remote add origin https://github.com/YOUR_USERNAME/DataForge_ETL.git
   
   # Push to GitHub
   git push -u origin main
   ```

### Option B: Azure DevOps

1. **Create a new repository in Azure DevOps:**
   - Navigate to your Azure DevOps project
   - Go to Repos → Files
   - Initialize repository: `DataForge_ETL`

2. **Connect and push:**
   ```powershell
   # Add remote repository
   git remote add origin https://dev.azure.com/YOUR_ORG/YOUR_PROJECT/_git/DataForge_ETL
   
   # Push to Azure DevOps
   git push -u origin main
   ```

### Option C: GitLab

1. **Create a new project on GitLab:**
   - Go to https://gitlab.com/projects/new
   - Project name: `DataForge_ETL`
   - Visibility: Private/Internal/Public
   - **Uncheck** "Initialize repository with a README"

2. **Connect and push:**
   ```powershell
   # Add remote repository
   git remote add origin https://gitlab.com/YOUR_USERNAME/DataForge_ETL.git
   
   # Push to GitLab
   git push -u origin main
   ```

---

## Recommended Git Workflow

### Daily Development

```powershell
# 1. Check current status
git status

# 2. Pull latest changes (if working with team)
git pull origin main

# 3. Create a feature branch
git checkout -b feature/your-feature-name

# 4. Make changes, then stage them
git add .

# 5. Commit with descriptive message
git commit -m "Add feature: description of changes"

# 6. Push branch to remote
git push origin feature/your-feature-name

# 7. Create Pull Request on GitHub/Azure DevOps/GitLab
```

### Commit Message Guidelines

**Format:**
```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `refactor`: Code refactoring
- `perf`: Performance improvement
- `test`: Adding tests
- `chore`: Maintenance tasks

**Examples:**
```powershell
git commit -m "feat(orchestrator): add continue-on-error option"
git commit -m "fix(database): resolve column validation bug"
git commit -m "docs(readme): update installation instructions"
git commit -m "refactor(excel): improve sheet processing performance"
```

---

## Useful Git Commands

### Viewing Changes

```powershell
# View uncommitted changes
git diff

# View staged changes
git diff --staged

# View commit history
git log --oneline --graph --decorate

# View specific file history
git log -- path/to/file.cs
```

### Branching

```powershell
# List all branches
git branch -a

# Create and switch to new branch
git checkout -b feature/new-feature

# Switch to existing branch
git checkout main

# Delete local branch
git branch -d feature/old-feature

# Delete remote branch
git push origin --delete feature/old-feature
```

### Undoing Changes

```powershell
# Discard uncommitted changes in file
git checkout -- path/to/file.cs

# Unstage file (keep changes)
git reset HEAD path/to/file.cs

# Amend last commit
git commit --amend -m "Updated commit message"

# Revert a commit (creates new commit)
git revert <commit-hash>

# Reset to previous commit (dangerous!)
git reset --hard HEAD~1
```

### Stashing

```powershell
# Save current work temporarily
git stash

# List stashed changes
git stash list

# Apply most recent stash
git stash apply

# Apply and remove stash
git stash pop

# Clear all stashes
git stash clear
```

---

## Ignoring Files Already Tracked

If you accidentally committed files that should be ignored:

```powershell
# Remove from Git but keep locally
git rm --cached path/to/file

# Remove directory from Git but keep locally
git rm -r --cached path/to/directory/

# Commit the removal
git commit -m "chore: remove ignored files from tracking"
```

---

## Handling Sensitive Data

### If you accidentally committed sensitive data (passwords, keys):

**⚠️ IMPORTANT: Immediately:**

1. **Rotate the compromised credentials**
2. **Remove from history:**
   ```powershell
   # Install BFG Repo-Cleaner
   # Download from: https://rtyley.github.io/bfg-repo-cleaner/
   
   # Remove sensitive file
   bfg --delete-files appsettings.Production.json
   
   # Or replace text
   bfg --replace-text passwords.txt
   
   # Clean up
   git reflog expire --expire=now --all
   git gc --prune=now --aggressive
   
   # Force push (⚠️ coordinate with team)
   git push --force
   ```

### Prevention:

- ✅ Always use `.gitignore` (already configured)
- ✅ Store secrets in environment variables
- ✅ Use Azure Key Vault or similar for production
- ✅ Enable pre-commit hooks to scan for secrets

---

## Git Configuration for Large Files

If you need to track large Excel samples or test files:

```powershell
# Install Git LFS (Large File Storage)
# Download from: https://git-lfs.github.com/

# Initialize Git LFS
git lfs install

# Track large file types
git lfs track "*.xlsx"
git lfs track "*.xls"

# Commit .gitattributes
git add .gitattributes
git commit -m "chore: configure Git LFS for Excel files"
```

---

## Backup and Recovery

### Create a backup

```powershell
# Clone to another location
git clone F:\Projects-Hub\DataForge_ETL F:\Backups\DataForge_ETL_Backup
```

### Restore from backup

```powershell
# Copy .git directory from backup
Copy-Item -Path "F:\Backups\DataForge_ETL_Backup\.git" -Destination "F:\Projects-Hub\DataForge_ETL\" -Recurse -Force

# Reset to last commit
git reset --hard HEAD
```

---

## Team Collaboration

### Pull Request Workflow

1. Fork or clone repository
2. Create feature branch: `git checkout -b feature/amazing-feature`
3. Make changes and commit
4. Push to remote: `git push origin feature/amazing-feature`
5. Create Pull Request on GitHub/Azure DevOps
6. Address review comments
7. Merge when approved

### Code Review Guidelines

- ✅ Descriptive PR title and description
- ✅ Reference issue numbers
- ✅ Keep PRs focused (one feature/fix)
- ✅ Update documentation if needed
- ✅ Ensure all tests pass

---

## Continuous Integration (CI/CD)

### GitHub Actions Example

Create `.github/workflows/build.yml`:

```yaml
name: Build and Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore -c Release
    
    - name: Test
      run: dotnet test --no-build --verbosity normal
```

---

## Troubleshooting

### Issue: "LF will be replaced by CRLF"
**Solution:** This is normal on Windows. The `.gitattributes` file handles this.

### Issue: "Permission denied (publickey)"
**Solution:** Set up SSH keys or use HTTPS with credential manager.

### Issue: "fatal: remote origin already exists"
**Solution:** Remove and re-add remote:
```powershell
git remote remove origin
git remote add origin <your-repo-url>
```

### Issue: Merge conflicts
**Solution:**
```powershell
# Pull latest changes
git pull origin main

# Resolve conflicts in files (edit manually)
# Then stage resolved files
git add .

# Complete merge
git commit -m "Resolve merge conflicts"
```

---

## Additional Resources

- **Git Documentation:** https://git-scm.com/doc
- **GitHub Guides:** https://guides.github.com/
- **Atlassian Git Tutorials:** https://www.atlassian.com/git/tutorials
- **Pro Git Book (Free):** https://git-scm.com/book/en/v2

---

**Happy Coding! 🚀**

*Last Updated: October 24, 2025*
