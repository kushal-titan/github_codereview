using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using CodeMonitor.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CodeMonitor.Services
{
    public class EmailService
    {
        public bool SendReport(AnalysisReport report, string? senderEmail, string? appPassword, string? recipientEmail, string smtpServer = "smtp.office365.com", int smtpPort = 587)
        {
            if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(appPassword))
            {
                Console.WriteLine("[EmailService] OUTLOOK_SENDER_EMAIL or OUTLOOK_APP_PASSWORD is not configured. Skipping email dispatch (Dry Run).");
                return false;
            }

            string targetRecipient = !string.IsNullOrWhiteSpace(recipientEmail) 
                ? recipientEmail! 
                : (!string.IsNullOrWhiteSpace(report.AuthorEmail) ? report.AuthorEmail : senderEmail!);

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Code Quality Monitor Bot", senderEmail));
                message.To.Add(new MailboxAddress(report.AuthorName.Length > 0 ? report.AuthorName : "Developer", targetRecipient));

                string statusTitle = report.IsPassed ? "✅ Quality Gate Passed" : "🚨 Action Required: Code Quality Violations";
                string prInfo = !string.IsNullOrWhiteSpace(report.PullRequestNumber) ? $"PR #{report.PullRequestNumber}" : report.Branch;
                message.Subject = $"{statusTitle} – {report.Repository} ({prInfo})";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = GenerateHtmlBody(report)
                };

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                var secureOption = smtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
                
                Console.WriteLine($"[EmailService] Connecting to {smtpServer}:{smtpPort}...");
                client.Connect(smtpServer, smtpPort, secureOption);
                
                Console.WriteLine($"[EmailService] Authenticating as {senderEmail}...");
                client.Authenticate(senderEmail, appPassword);
                
                Console.WriteLine($"[EmailService] Dispatching alert to PR author: {targetRecipient}...");
                client.Send(message);
                client.Disconnect(true);

                Console.WriteLine($"[EmailService] ✅ Email report successfully dispatched to {targetRecipient}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService] ❌ Failed to dispatch email: {ex.Message}");
                return false;
            }
        }

        private string GenerateHtmlBody(AnalysisReport report)
        {
            var sb = new StringBuilder();
            string statusColor = report.IsPassed ? "#107c41" : "#cf222e";
            string statusBadge = report.IsPassed ? "QUALITY GATE PASSED" : "CODE QUALITY VIOLATIONS DETECTED";

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset='utf-8'><style>");
            sb.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Helvetica, Arial, sans-serif; background-color: #f6f8fa; margin: 0; padding: 24px; color: #24292f; }");
            sb.AppendLine(".container { max-width: 720px; margin: 0 auto; background: #ffffff; border-radius: 8px; overflow: hidden; border: 1px solid #d0d7de; box-shadow: 0 4px 12px rgba(0,0,0,0.05); }");
            sb.AppendLine(".header { background-color: " + statusColor + "; padding: 28px 24px; color: #ffffff; text-align: center; }");
            sb.AppendLine(".header h1 { margin: 0; font-size: 20px; font-weight: 700; letter-spacing: 0.5px; }");
            sb.AppendLine(".header p { margin: 8px 0 0 0; opacity: 0.95; font-size: 14px; }");
            sb.AppendLine(".content { padding: 24px; }");
            sb.AppendLine(".meta-table { width: 100%; border-collapse: collapse; margin-bottom: 24px; font-size: 13px; background: #f6f8fa; border-radius: 6px; border: 1px solid #d0d7de; }");
            sb.AppendLine(".meta-table td { padding: 10px 14px; border-bottom: 1px solid #e1e4e8; }");
            sb.AppendLine(".meta-table td.label { font-weight: 600; width: 140px; color: #57606a; }");
            sb.AppendLine(".card { background: #ffffff; border: 1px solid #d0d7de; border-left: 5px solid " + statusColor + "; border-radius: 6px; padding: 18px; margin-bottom: 20px; box-shadow: 0 1px 3px rgba(0,0,0,0.04); }");
            sb.AppendLine(".badge { display: inline-block; padding: 3px 8px; border-radius: 12px; font-size: 11px; font-weight: bold; }");
            sb.AppendLine(".badge-error { background: #ffebe9; color: #cf222e; border: 1px solid #ff8182; }");
            sb.AppendLine(".badge-warning { background: #fff8c5; color: #9a6700; border: 1px solid #d4a72c; }");
            sb.AppendLine(".section-title { font-size: 13px; font-weight: 700; text-transform: uppercase; color: #57606a; margin-top: 10px; margin-bottom: 4px; }");
            sb.AppendLine(".code-box { background: #f6f8fa; border: 1px solid #d0d7de; border-radius: 6px; padding: 12px; font-family: Consolas, 'Courier New', monospace; font-size: 12px; line-height: 1.4; overflow-x: auto; white-space: pre-wrap; margin-top: 8px; }");
            sb.AppendLine(".action-box { background: #ddf4ff; border: 1px solid #54aeff; border-radius: 6px; padding: 12px 16px; margin-top: 12px; font-size: 13px; }");
            sb.AppendLine(".action-box ol { margin: 6px 0 0 0; padding-left: 20px; }");
            sb.AppendLine(".action-box li { margin-bottom: 4px; }");
            sb.AppendLine(".footer { text-align: center; padding: 20px; font-size: 12px; color: #8c959f; background: #f6f8fa; border-top: 1px solid #d0d7de; }");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine("<div class='container'>");
            sb.AppendLine($"<div class='header'><h1>{statusBadge}</h1><p>Automated Code Quality Monitor &mdash; Pull Request Analysis</p></div>");
            sb.AppendLine("<div class='content'>");

            // Metadata Table
            sb.AppendLine("<table class='meta-table'>");
            sb.AppendLine($"<tr><td class='label'>Repository</td><td><b>{report.Repository}</b></td></tr>");
            if (!string.IsNullOrWhiteSpace(report.PullRequestNumber))
            {
                sb.AppendLine($"<tr><td class='label'>Pull Request</td><td><a href='{report.PullRequestUrl}' style='color:#0969da; text-decoration:none;'><b>PR #{report.PullRequestNumber} &mdash; View on GitHub &rarr;</b></a></td></tr>");
            }
            sb.AppendLine($"<tr><td class='label'>Branch</td><td><code>{report.Branch}</code></td></tr>");
            sb.AppendLine($"<tr><td class='label'>PR Author</td><td><b>{report.AuthorName}</b> &lt;{report.AuthorEmail}&gt;</td></tr>");
            sb.AppendLine($"<tr><td class='label'>Summary</td><td><b style='color:{statusColor};'>{report.ErrorCount} Error(s)</b>, <b>{report.WarningCount} Warning(s)</b></td></tr>");
            sb.AppendLine("</table>");

            if (report.Violations.Count > 0)
            {
                sb.AppendLine("<h2 style='font-size: 17px; margin: 24px 0 12px 0; padding-bottom: 6px; border-bottom: 2px solid #e1e4e8;'>🔍 Detected Issues & How to Fix Them</h2>");
                
                int index = 1;
                foreach (var v in report.Violations)
                {
                    string badgeClass = v.Severity == ViolationSeverity.Error ? "badge-error" : "badge-warning";
                    sb.AppendLine("<div class='card'>");
                    sb.AppendLine($"<div style='display:flex; justify-content:space-between; align-items:center; margin-bottom:8px;'>");
                    sb.AppendLine($"<span class='badge {badgeClass}'>{v.Severity.ToString().ToUpperInvariant()}</span>");
                    sb.AppendLine($"<span style='font-size:12px; color:#57606a; font-weight:600;'>Line {v.LineNumber} &ndash; {v.EndLineNumber}</span>");
                    sb.AppendLine("</div>");
                    
                    sb.AppendLine($"<h3 style='margin:4px 0 10px 0; font-size:16px; color:#0969da;'>{index++}. [{v.RuleId}] {v.RuleName} in <code>{v.MemberName}()</code></h3>");
                    
                    sb.AppendLine($"<p style='margin:4px 0; font-size:13px;'><b>📍 WHERE:</b> <code>{Path.GetFileName(v.TargetFile)}</code> (Lines {v.LineNumber}-{v.EndLineNumber})</p>");
                    sb.AppendLine($"<p style='margin:4px 0; font-size:13px;'><b>⚠️ WHAT:</b> {v.Description}</p>");
                    sb.AppendLine($"<p style='margin:4px 0; font-size:13px; color:#57606a;'><b>💡 WHY:</b> {v.Rationale}</p>");

                    if (v.ActionSteps.Count > 0)
                    {
                        sb.AppendLine("<div class='action-box'>");
                        sb.AppendLine("<b>🛠️ WHAT TO DO:</b><ol>");
                        foreach (var step in v.ActionSteps)
                        {
                            sb.AppendLine($"<li>{step}</li>");
                        }
                        sb.AppendLine("</ol></div>");
                    }

                    if (!string.IsNullOrWhiteSpace(v.CodeExample))
                    {
                        sb.AppendLine("<div class='section-title'>Refactoring Code Example</div>");
                        sb.AppendLine($"<div class='code-box'>{HttpUtility.HtmlEncode(v.CodeExample)}</div>");
                    }

                    sb.AppendLine("</div>");
                }
            }
            else
            {
                sb.AppendLine("<div style='text-align:center; padding:36px 16px;'>");
                sb.AppendLine("<h2 style='color:#107c41; margin-bottom:8px;'>🎉 Great Job!</h2>");
                sb.AppendLine("<p style='font-size:14px; color:#57606a;'>All analyzed C# files meet the repository's code quality standards. Ready for review and merge!</p>");
                sb.AppendLine("</div>");
            }

            sb.AppendLine("</div>");
            sb.AppendLine($"<div class='footer'>Automated Code Quality Monitor &bull; Generated at {report.AnalysisTime:yyyy-MM-dd HH:mm:ss UTC}</div>");
            sb.AppendLine("</div></body></html>");

            return sb.ToString();
        }
    }
}
