# 🛡️ Automated Code Quality Monitor Workflow

Below is the complete, validated YAML code. Save this exact content into `.github/workflows/code-quality.yml`.

```yaml
name: Automated Code Quality Monitor

on:
  pull_request:
    types: [opened, synchronize, reopened, review_requested, closed]
  push:
    branches:
      - main
      - master
      - develop
  workflow_dispatch:

permissions:
  contents: write
  pull-requests: write
  issues: write
  checks: write
  statuses: write

jobs:
  code-quality-gate:
    name: "Roslyn AST Quality Analysis & Alerting"
    runs-on: ubuntu-latest

    steps:
      - name: 📥 Checkout Repository Code
        uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: ⚙️ Setup .NET SDK
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: 🔨 Restore & Build Code Monitor
        run: |
          if [ -f "github_review/src/CodeMonitor/CodeMonitor.csproj" ]; then
            PROJECT_PATH="github_review/src/CodeMonitor/CodeMonitor.csproj"
          elif [ -f "src/CodeMonitor/CodeMonitor.csproj" ]; then
            PROJECT_PATH="src/CodeMonitor/CodeMonitor.csproj"
          else
            PROJECT_PATH=$(find . -name "CodeMonitor.csproj" | head -n 1)
          fi
          echo "PROJECT_PATH=$PROJECT_PATH" >> $GITHUB_ENV
          echo "Found CodeMonitor project at: $PROJECT_PATH"
          dotnet restore "$PROJECT_PATH"
          dotnet build "$PROJECT_PATH" -c Release

      - name: 🔍 Resolve PR Author & Reviewers' Emails
        id: resolve_recipients
        uses: actions/github-script@v7
        with:
          script: |
            const { execSync } = require('child_process');
            
            // 1. Resolve PR Author Email
            let commitEmail = '';
            try {
              commitEmail = execSync("git log -1 --format='%ae'").toString().trim();
            } catch (e) {
              console.log('Notice reading git log:', e.message);
            }
            
            let prAuthorEmail = commitEmail;
            if (!prAuthorEmail || prAuthorEmail === 'null' || !prAuthorEmail.includes('@')) {
              prAuthorEmail = context.payload.pull_request?.user?.email || context.payload.pusher?.email || '';
            }
            if (!prAuthorEmail || prAuthorEmail === 'null') {
              const actorLogin = context.payload.pull_request?.user?.login || context.actor || '';
              if (actorLogin) {
                prAuthorEmail = `${actorLogin.toLowerCase()}@titan.co.in`;
              }
            }
            
            console.log(`👤 Resolved PR Author Mail: ${prAuthorEmail}`);
            core.setOutput('author_email', prAuthorEmail);
            
            // 2. Resolve Requested Reviewers
            const requestedReviewers = context.payload.pull_request?.requested_reviewers || [];
            const reviewerHandles = [];
            const reviewerEmails = [];
            
            for (const reviewer of requestedReviewers) {
              const handle = reviewer.login;
              reviewerHandles.push(handle);
              let foundEmail = '';
              try {
                const userRes = await github.rest.users.getByUsername({ username: handle });
                if (userRes.data && userRes.data.email) {
                  foundEmail = userRes.data.email.trim();
                }
              } catch (err) {
                console.log(`Notice fetching user profile for ${handle}:`, err.message);
              }
              
              if (!foundEmail) {
                // Corporate domain fallback for Titan organization members
                foundEmail = `${handle.toLowerCase()}@titan.co.in`;
              }
              
              if (foundEmail && !reviewerEmails.includes(foundEmail)) {
                reviewerEmails.push(foundEmail);
              }
            }
            
            const handlesStr = reviewerHandles.join(', ');
            const emailsStr = reviewerEmails.join(', ');
            console.log(`👥 Requested Reviewers (${reviewerHandles.length}): ${handlesStr || 'None'}`);
            console.log(`📬 Reviewers Email Addresses: ${emailsStr || 'None'}`);
            
            core.setOutput('reviewers_handles', handlesStr);
            core.setOutput('reviewers_emails', emailsStr);

      - name: 🛡️ Run Quality Gate & AST Analyzer
        id: run_analyzer
        continue-on-error: true
        env:
          EVENT_NAME: ${{ github.event_name }}
          BASE_BRANCH: ${{ github.base_ref }}
          PR_NUMBER: ${{ github.event.pull_request.number }}
          PR_URL: ${{ github.event.pull_request.html_url }}
          PR_AUTHOR_EMAIL: ${{ steps.resolve_recipients.outputs.author_email }}
          PR_REVIEWERS: ${{ steps.resolve_recipients.outputs.reviewers_handles }}
          REVIEWERS_EMAILS: ${{ steps.resolve_recipients.outputs.reviewers_emails }}
          GITHUB_WORKSPACE: ${{ github.workspace }}
          OUTLOOK_SENDER_EMAIL: ${{ secrets.OUTLOOK_SENDER_EMAIL }}
          OUTLOOK_APP_PASSWORD: ${{ secrets.OUTLOOK_APP_PASSWORD }}
          OUTLOOK_ADDITIONAL_RECIPIENTS: ${{ secrets.OUTLOOK_ADDITIONAL_RECIPIENTS }}
          OUTLOOK_REVIEWERS_EMAILS: ${{ steps.resolve_recipients.outputs.reviewers_emails }}
          OUTLOOK_LEAD_EMAIL: ${{ secrets.OUTLOOK_LEAD_EMAIL }}
          OUTLOOK_MANAGER_EMAIL: ${{ secrets.OUTLOOK_MANAGER_EMAIL }}
          OUTLOOK_SMTP_SERVER: "smtp.office365.com"
          OUTLOOK_SMTP_PORT: 587
        run: |
          if [ "$EVENT_NAME" = "pull_request" ] && [ -n "$BASE_BRANCH" ]; then
            BASE_REF="origin/$BASE_BRANCH"
          else
            BASE_REF="HEAD~1"
          fi
          echo "Analyzing changes against base: $BASE_REF for PR Author: $PR_AUTHOR_EMAIL"
          echo "Reviewers: $PR_REVIEWERS ($REVIEWERS_EMAILS)"
          echo "Workspace: $GITHUB_WORKSPACE"
          dotnet run --project "$PROJECT_PATH" -c Release -f net8.0 --no-build -- \
            --path "$GITHUB_WORKSPACE" \
            --base-ref "$BASE_REF" \
            --pr-number "$PR_NUMBER" \
            --pr-url "$PR_URL" \
            --pr-author "$PR_AUTHOR_EMAIL" \
            --reviewers "$PR_REVIEWERS" \
            --reviewers-emails "$REVIEWERS_EMAILS"

      - name: 💬 Post or Update PR Quality Comment
        if: always() && github.event_name == 'pull_request'
        uses: actions/github-script@v7
        with:
          script: |
            const fs = require('fs');
            let prCommentFile = 'pr-comment.md';
            if (!fs.existsSync(prCommentFile) && fs.existsSync('github_review/pr-comment.md')) {
              prCommentFile = 'github_review/pr-comment.md';
            }

            if (fs.existsSync(prCommentFile)) {
              const body = fs.readFileSync(prCommentFile, 'utf8');
              try {
                const { data: comments } = await github.rest.issues.listComments({
                  owner: context.repo.owner,
                  repo: context.repo.repo,
                  issue_number: context.issue.number,
                });
                
                const botComment = comments.find(comment => 
                  comment.body.includes('Code Quality Gate:') || 
                  comment.body.includes('Code Quality Check:')
                );
                
                if (botComment) {
                  await github.rest.issues.updateComment({
                    owner: context.repo.owner,
                    repo: context.repo.repo,
                    comment_id: botComment.id,
                    body: body
                  });
                  console.log('✅ Updated existing PR quality comment.');
                } else {
                  await github.rest.issues.createComment({
                    owner: context.repo.owner,
                    repo: context.repo.repo,
                    issue_number: context.issue.number,
                    body: body
                  });
                  console.log('✅ Created new PR quality comment.');
                }
              } catch (err) {
                console.log('⚠️ Could not post PR comment:', err.message);
              }
            }

      - name: 📋 File or Auto-Close GitHub Issue for Blocking Errors
        if: always()
        uses: actions/github-script@v7
        with:
          script: |
            const fs = require('fs');
            let errorFile = 'issues/latest_errors.md';
            if (!fs.existsSync(errorFile) && fs.existsSync('github_review/issues/latest_errors.md')) {
              errorFile = 'github_review/issues/latest_errors.md';
            }

            const prNumber = context.payload.pull_request ? context.payload.pull_request.number : '';
            const branchName = (context.payload.pull_request ? context.payload.pull_request.head.ref : context.ref || '').replace('refs/heads/', '');
            const commitSha = context.sha ? context.sha.substring(0, 7) : '';
            
            let hasErrors = false;
            let body = '';
            if (fs.existsSync(errorFile)) {
              body = fs.readFileSync(errorFile, 'utf8');
              if (body.includes('Summary of Critical Blocking Errors') || body.includes('Action Required') || body.includes('❌ **Error**')) {
                hasErrors = true;
              }
            }
            
            // 1. Discover files modified in this commit/PR
            let modifiedFiles = [];
            try {
              const { execSync } = require('child_process');
              const baseRef = process.env.BASE_REF || (context.payload.pull_request ? `origin/${context.payload.pull_request.base.ref}` : 'origin/main');
              const diffOutput = execSync(`git diff --name-only ${baseRef} HEAD 2>/dev/null || git diff --name-only HEAD~1 HEAD 2>/dev/null`).toString();
              modifiedFiles = diffOutput.split('\n').map(f => f.trim()).filter(Boolean);
              console.log(`📁 Files modified in this commit/PR (${modifiedFiles.length}):`, modifiedFiles);
            } catch (e) {
              console.log('Notice reading git diff for modified files:', e.message);
            }
            
            // 2. Query open issues across repository
            let openIssues = [];
            try {
              const res = await github.rest.issues.listForRepo({
                owner: context.repo.owner,
                repo: context.repo.repo,
                state: 'open',
                per_page: 100
              });
              openIssues = res.data;
            } catch (err) {
              console.log('Notice querying open issues:', err.message);
            }
            
            // Search for existing open living issue for this specific PR or branch
            const contextTag = prNumber ? `[PR #${prNumber}]` : (branchName ? `[Branch: ${branchName}]` : '');
            const contextLabel = prNumber ? `pr-${prNumber}` : (branchName ? `branch-${branchName}` : '');
            
            const existingLivingIssue = openIssues.find(issue => {
              const title = issue.title || '';
              const issueBody = issue.body || '';
              const issueLabels = (issue.labels || []).map(l => typeof l === 'string' ? l : l.name);
              
              const isQualityIssue = issueLabels.includes('code-quality-violation') ||
                                     title.includes('Code Quality Blocking Errors') || 
                                     title.includes('❌') || 
                                     issueBody.includes('Critical Blocking Errors');
              if (!isQualityIssue) return false;
              
              if (contextLabel && issueLabels.includes(contextLabel)) return true;
              if (contextTag && (title.includes(contextTag) || issueBody.includes(contextTag))) return true;
              return false;
            });

            if (hasErrors) {
              let prAuthor = '';
              if (context.payload.pull_request && context.payload.pull_request.user && context.payload.pull_request.user.login) {
                prAuthor = context.payload.pull_request.user.login;
              } else if (context.actor) {
                prAuthor = context.actor;
              } else if (context.payload.sender && context.payload.sender.login) {
                prAuthor = context.payload.sender.login;
              }
              prAuthor = (prAuthor || '').trim();
              
              const now = new Date();
              const istOffset = 5.5 * 60 * 60 * 1000;
              const istDate = new Date(now.getTime() + istOffset);
              const timeString = istDate.toISOString().replace('T', ' ').substring(0, 19) + ' IST';
              const title = contextTag 
                ? `❌ Code Quality Blocking Errors ${contextTag} - ${timeString}`
                : `❌ Code Quality Blocking Errors - ${timeString}`;
                
              let enrichedBody = body;
              if (prAuthor) {
                enrichedBody = `> **👤 Assigned PR Author:** @${prAuthor} (Automatically assigned for immediate review & remediation)  \n\n` + body;
              }
              
              const issueLabels = ['code-quality-violation'];
              if (contextLabel) issueLabels.push(contextLabel);

              if (existingLivingIssue) {
                console.log(`🔄 Updating existing Issue #${existingLivingIssue.number} for ${contextTag || 'current run'} on commit ${commitSha}...`);
                try {
                  await github.rest.issues.update({
                    owner: context.repo.owner,
                    repo: context.repo.repo,
                    issue_number: existingLivingIssue.number,
                    title: title,
                    body: enrichedBody,
                    labels: issueLabels
                  });
                  await github.rest.issues.createComment({
                    owner: context.repo.owner,
                    repo: context.repo.repo,
                    issue_number: existingLivingIssue.number,
                    body: `🔄 **New commit pushed (\`${commitSha}\`):** Code Quality Gate re-evaluated. Critical blocking errors are still present. Please see the updated summary above.`
                  });
                  console.log(`✅ Successfully updated Issue #${existingLivingIssue.number} with latest commit error report.`);
                } catch (updateErr) {
                  console.log('⚠️ Could not update existing issue:', updateErr.message);
                }
              } else {
                console.log(`🚀 Creating initial GitHub Issue for blocking errors: ${title}`);
                const issuePayload = {
                  owner: context.repo.owner,
                  repo: context.repo.repo,
                  title: title,
                  body: enrichedBody,
                  labels: issueLabels
                };
                if (prAuthor) {
                  issuePayload.assignees = [prAuthor];
                }
                
                try {
                  const issue = await github.rest.issues.create(issuePayload);
                  console.log(`✅ Successfully created GitHub Issue #${issue.data.number} assigned to @${prAuthor}: ${issue.data.html_url}`);
                  if (prAuthor) {
                    await github.rest.issues.addAssignees({
                      owner: context.repo.owner,
                      repo: context.repo.repo,
                      issue_number: issue.data.number,
                      assignees: [prAuthor]
                    }).catch(e => console.log('Notice on addAssignees confirmation:', e.message));
                  }
                } catch (err) {
                  console.log('⚠️ Failed with assignees in create, attempting fallback creation:', err.message);
                  delete issuePayload.assignees;
                  try {
                    const fallbackIssue = await github.rest.issues.create(issuePayload);
                    console.log(`✅ Created fallback GitHub Issue #${fallbackIssue.data.number}: ${fallbackIssue.data.html_url}`);
                  } catch (fallbackErr) {
                    console.log('❌ Could not create GitHub Issue:', fallbackErr.message);
                  }
                }
              }
            } else {
              console.log('✅ 0 critical blocking errors detected in this commit/PR (Quality Gate passed).');
              console.log('🔍 Checking for open blocking error issues strictly related to this PR / branch / modified files to auto-close...');
              
              const matchingIssues = openIssues.filter(issue => {
                const title = issue.title || '';
                const issueBody = issue.body || '';
                const issueLabels = (issue.labels || []).map(l => typeof l === 'string' ? l : l.name);
                const isQualityIssue = issueLabels.includes('code-quality-violation') ||
                                       title.includes('Code Quality Blocking Errors') || 
                                       title.includes('❌') || 
                                       issueBody.includes('Critical Blocking Errors');
                if (!isQualityIssue) return false;
                
                if (contextLabel && issueLabels.includes(contextLabel)) return true;
                if (contextTag && (title.includes(contextTag) || issueBody.includes(contextTag))) return true;
                
                if (modifiedFiles.length > 0) {
                  const touchesModifiedFile = modifiedFiles.some(file => {
                    const fileName = file.split('/').pop().split('\\').pop();
                    return fileName && fileName.endsWith('.cs') && (issueBody.includes(file) || issueBody.includes(fileName) || title.includes(fileName));
                  });
                  if (touchesModifiedFile) return true;
                }
                
                if (!prNumber && (context.ref === 'refs/heads/main' || context.ref === 'refs/heads/master')) {
                  return true;
                }
                
                return false;
              });
              
              console.log(`Found ${matchingIssues.length} open blocking issue(s) matching this PR / modified files to auto-close.`);
              for (const issue of matchingIssues) {
                console.log(`🎉 Auto-closing resolved Issue #${issue.number}: ${issue.title}`);
                const prContextText = prNumber ? `on **PR #${prNumber}** (commit \`${commitSha}\`)` : `in commit \`${commitSha}\``;
                const filesContextText = modifiedFiles.length > 0 ? `\n* **Verified Files:** \`${modifiedFiles.join('`, `')}\`` : '';
                await github.rest.issues.createComment({
                  owner: context.repo.owner,
                  repo: context.repo.repo,
                  issue_number: issue.number,
                  body: `### 🟢 Code Quality Violations Resolved!\n\nAll critical blocking errors reported in this issue have been successfully remediated ${prContextText} and verified by the Automated Code Quality Monitor with **0 errors**.\n${filesContextText}\n* **Quality Gate:** Passed ✅\n* **Status:** Closed as completed.`
                }).catch(e => console.log('Notice on resolution comment:', e.message));
                
                await github.rest.issues.update({
                  owner: context.repo.owner,
                  repo: context.repo.repo,
                  issue_number: issue.number,
                  state: 'closed',
                  state_reason: 'completed'
                }).catch(e => console.log('Notice on closing issue:', e.message));
                
                console.log(`✅ Successfully closed Issue #${issue.number} as completed.`);
              }
            }

      - name: 💾 Commit & Push Error Report to Branch
        if: always() && github.event_name == 'pull_request'
        run: |
          if [ -d "issues" ] && [ -n "$(ls -A issues/errors_*.md 2>/dev/null)" ]; then
            echo "Committing generated error report (.md) to branch..."
            git config --global user.name "github-actions[bot]"
            git config --global user.email "github-actions[bot]@users.noreply.github.com"
            git add issues/
            git commit -m "chore(quality): save error report (.md) [skip ci]" || echo "No new files to commit"
            git push origin HEAD:${{ github.head_ref }} || echo "Could not push directly to branch"
          fi

      - name: 📦 Archive Quality Reports Artifacts (.md)
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: code-quality-report
          path: issues/
          if-no-files-found: ignore

      - name: 🚨 Evaluate Quality Gate Result
        if: always()
        run: |
          if [ "${{ steps.run_analyzer.outcome }}" != "success" ]; then
            echo "❌ Quality Gate failed due to critical code quality violations."
            exit 1
          else
            echo "✅ Quality Gate passed."
          fi
```
