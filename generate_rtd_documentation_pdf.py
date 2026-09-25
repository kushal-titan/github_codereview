import sys
import os
from reportlab.lib.pagesizes import letter
from reportlab.lib import colors
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, PageBreak, KeepTogether, HRFlowable
)
from reportlab.pdfgen import canvas

class SphinxReadTheDocsCanvas(canvas.Canvas):
    def __init__(self, *args, **kwargs):
        super(SphinxReadTheDocsCanvas, self).__init__(*args, **kwargs)
        self._saved_page_states = []

    def showPage(self):
        self._saved_page_states.append(dict(self.__dict__))
        self._startPage()

    def save(self):
        num_pages = len(self._saved_page_states)
        for state in self._saved_page_states:
            self.__dict__.update(state)
            self.draw_sphinx_decorations(num_pages)
            super(SphinxReadTheDocsCanvas, self).showPage()
        super(SphinxReadTheDocsCanvas, self).save()

    def draw_sphinx_decorations(self, total_pages):
        if self._pageNumber == 1:
            self.saveState()
            self.setFillColor(colors.HexColor("#2980b9"))
            self.rect(0, 786, 612, 6, fill=1, stroke=0)
            
            self.setStrokeColor(colors.HexColor("#cbd5e1"))
            self.setLineWidth(0.6)
            self.line(40, 36, 572, 36)
            self.setFont("Helvetica", 7.5)
            self.setFillColor(colors.HexColor("#64748b"))
            self.drawString(40, 25, "Enterprise Software Architecture & Quality Engineering Manual")
            page_str = f"Page 1 of {total_pages}"
            self.drawRightString(572, 25, page_str)
            self.restoreState()
            return

        self.saveState()
        self.setFont("Helvetica", 7.5)
        self.setFillColor(colors.HexColor("#475569"))
        self.drawString(40, 762, "Automated .NET Code Quality Monitor Documentation, Release 1.0.0")
        
        self.setStrokeColor(colors.HexColor("#cbd5e1"))
        self.setLineWidth(0.6)
        self.line(40, 755, 572, 755)

        self.line(40, 36, 572, 36)
        self.setFont("Helvetica", 7.5)
        self.setFillColor(colors.HexColor("#64748b"))
        self.drawString(40, 25, "Enterprise Software Architecture & Quality Engineering Manual")
        page_str = f"Page {self._pageNumber} of {total_pages}"
        self.drawRightString(572, 25, page_str)
        self.restoreState()


def create_rtd_note(title, text, note_type="note", width=532):
    styles = getSampleStyleSheet()
    
    if note_type == "warning":
        bg_col = "#fdf2e9"
        border_col = "#e67e22"
        title_col = "#d35400"
        default_title = "WARNING"
    elif note_type == "tip":
        bg_col = "#eafaf1"
        border_col = "#2ecc71"
        title_col = "#27ae60"
        default_title = "TIP"
    else:
        bg_col = "#e8f4f8"
        border_col = "#2980b9"
        title_col = "#2980b9"
        default_title = "NOTE"

    display_title = title if title else default_title

    t_style = ParagraphStyle(
        'RTDNoteTitle',
        parent=styles['Normal'],
        fontName='Helvetica-Bold',
        fontSize=8,
        leading=10.5,
        textColor=colors.HexColor(title_col),
        spaceAfter=1.5
    )
    b_style = ParagraphStyle(
        'RTDNoteBody',
        parent=styles['Normal'],
        fontName='Helvetica',
        fontSize=7.2,
        leading=9.8,
        textColor=colors.HexColor("#2c3e50")
    )
    
    content = [
        Paragraph(f"<b>{display_title}</b>", t_style),
        Paragraph(text, b_style)
    ]
    t = Table([[content]], colWidths=[width])
    t.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, -1), colors.HexColor(bg_col)),
        ('BOX', (0, 0), (-1, -1), 0.5, colors.HexColor("#d5dbdb")),
        ('LINEBEFORE', (0, 0), (0, -1), 3.5, colors.HexColor(border_col)),
        ('TOPPADDING', (0, 0), (-1, -1), 3.5),
        ('BOTTOMPADDING', (0, 0), (-1, -1), 3.5),
        ('LEFTPADDING', (0, 0), (-1, -1), 7),
        ('RIGHTPADDING', (0, 0), (-1, -1), 7),
    ]))
    return t


def create_code_block(code_text, label="Repository Architecture & Directory Layout", width=532):
    styles = getSampleStyleSheet()
    header_style = ParagraphStyle(
        'CodeHeader',
        parent=styles['Normal'],
        fontName='Helvetica-Bold',
        fontSize=7.2,
        leading=9.0,
        textColor=colors.HexColor("#475569")
    )
    code_style = ParagraphStyle(
        'CodeText',
        parent=styles['Normal'],
        fontName='Courier',
        fontSize=6.6,
        leading=8.4,
        textColor=colors.HexColor("#1e293b")
    )
    formatted = code_text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;").replace("\n", "<br/>").replace(" ", "&nbsp;")
    content = [
        Paragraph(f"<b>{label}</b>", header_style),
        Spacer(1, 1.5),
        Paragraph(formatted, code_style)
    ]
    t = Table([[content]], colWidths=[width])
    t.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, -1), colors.HexColor("#f8fafc")),
        ('BOX', (0, 0), (-1, -1), 0.6, colors.HexColor("#cbd5e1")),
        ('TOPPADDING', (0, 0), (-1, -1), 3.5),
        ('BOTTOMPADDING', (0, 0), (-1, -1), 3.5),
        ('LEFTPADDING', (0, 0), (-1, -1), 7),
        ('RIGHTPADDING', (0, 0), (-1, -1), 7),
    ]))
    return t


def build_rtd_pdf(filename):
    doc = SimpleDocTemplate(
        filename,
        pagesize=letter,
        leftMargin=40,
        rightMargin=40,
        topMargin=36,
        bottomMargin=38
    )

    styles = getSampleStyleSheet()

    cover_pretitle = ParagraphStyle(
        'CoverPre',
        parent=styles['Normal'],
        fontName='Helvetica-Bold',
        fontSize=8.5,
        leading=11,
        textColor=colors.HexColor("#2980b9"),
        spaceAfter=2
    )

    cover_title = ParagraphStyle(
        'CoverTitle',
        parent=styles['Normal'],
        fontName='Helvetica-Bold',
        fontSize=18,
        leading=21,
        textColor=colors.HexColor("#2c3e50"),
        spaceAfter=2
    )

    cover_subtitle = ParagraphStyle(
        'CoverSub',
        parent=styles['Normal'],
        fontName='Helvetica',
        fontSize=9.5,
        leading=12.5,
        textColor=colors.HexColor("#555555"),
        spaceAfter=5
    )

    h1_style = ParagraphStyle(
        'Heading1_Sphinx',
        parent=styles['Normal'],
        fontName='Helvetica-Bold',
        fontSize=10,
        leading=13,
        textColor=colors.HexColor("#2980b9"),
        spaceBefore=4.5,
        spaceAfter=2,
        keepWithNext=True
    )

    body_style = ParagraphStyle(
        'Body_Sphinx',
        parent=styles['Normal'],
        fontName='Helvetica',
        fontSize=7.2,
        leading=9.8,
        textColor=colors.HexColor("#333333"),
        spaceAfter=2.5
    )

    table_cell = ParagraphStyle(
        'TableCell_Sphinx',
        parent=styles['Normal'],
        fontName='Helvetica',
        fontSize=6.5,
        leading=8.3,
        textColor=colors.HexColor("#2c3e50")
    )

    table_header = ParagraphStyle(
        'TableHeader_Sphinx',
        parent=styles['Normal'],
        fontName='Helvetica-Bold',
        fontSize=6.8,
        leading=8.8,
        textColor=colors.HexColor("#ffffff")
    )

    story = []

    # PAGE 1: OVERVIEW & ARCHITECTURE
    story.append(Paragraph("TECHNICAL DOCUMENTATION MANUAL", cover_pretitle))
    story.append(Paragraph("Automated .NET Code Quality Monitor", cover_title))
    story.append(Paragraph("Complete 6-Category Roslyn Analysis Spectrum & Enterprise Quality Gate", cover_subtitle))

    meta_data = [
        [
            Paragraph("<b>Release:</b> 1.0.0 (Production)", table_cell),
            Paragraph("<b>Runtime Platform:</b> .NET 8.0 SDK (C# 12)", table_cell),
            Paragraph("<b>Analysis Engine:</b> Roslyn AST (29 Rules)", table_cell),
            Paragraph("<b>CI/CD:</b> Native GitHub Actions", table_cell)
        ]
    ]
    t_meta = Table(meta_data, colWidths=[130, 134, 134, 134])
    t_meta.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, -1), colors.HexColor("#f1f5f9")),
        ('BOX', (0, 0), (-1, -1), 0.5, colors.HexColor("#cbd5e1")),
        ('TOPPADDING', (0, 0), (-1, -1), 2),
        ('BOTTOMPADDING', (0, 0), (-1, -1), 2),
        ('LEFTPADDING', (0, 0), (-1, -1), 5),
        ('RIGHTPADDING', (0, 0), (-1, -1), 5),
    ]))
    story.append(t_meta)
    story.append(Spacer(1, 3))

    story.append(Paragraph("1. Project Overview & Objectives", h1_style))
    story.append(Paragraph(
        "<b>1.1 Purpose:</b> The <b>Automated .NET Code Quality Monitor</b> enforces deep architectural, "
        "safety, and security standards natively in GitHub Pull Requests. It analyzes every modified C# line using Microsoft Roslyn AST "
        "compilers across all 6 core categories of the <b>Roslyn Analysis Spectrum</b>.",
        body_style
    ))
    story.append(Spacer(1, 2.5))

    story.append(Paragraph("2. System Architecture & End-to-End Workflow", h1_style))
    workflow_stages = [
        [
            Paragraph("<b>Stage</b>", table_header),
            Paragraph("<b>Component / Module</b>", table_header),
            Paragraph("<b>Operational Description</b>", table_header),
            Paragraph("<b>Output / Result</b>", table_header)
        ],
        [
            Paragraph("<b>1. Trigger</b>", table_cell),
            Paragraph("GitHub Actions Event", table_cell),
            Paragraph("Listens for <code>pull_request</code> and <code>push</code> events.", table_cell),
            Paragraph("CI runner initialization", table_cell)
        ],
        [
            Paragraph("<b>2. Diff Isolation</b>", table_cell),
            Paragraph("<code>GitDiffService</code>", table_cell),
            Paragraph("Executes unified diff against base branch (<code>git diff base...HEAD</code>) to map modified lines.", table_cell),
            Paragraph("Modified line spans map", table_cell)
        ],
        [
            Paragraph("<b>3. Roslyn AST</b>", table_cell),
            Paragraph("Spectrum Analyzers", table_cell),
            Paragraph("Parses C# AST nodes across all rules (Complexity, Safety, Async, Security, Performance, Standards).", table_cell),
            Paragraph("Structured <code>AnalysisReport</code>", table_cell)
        ],
        [
            Paragraph("<b>4. Reporting</b>", table_cell),
            Paragraph("Reporting & Email", table_cell),
            Paragraph("Emits GitHub line annotations (<code>::error</code>), step summary table, and responsive HTML email.", table_cell),
            Paragraph("PR annotations + Outlook email", table_cell)
        ],
        [
            Paragraph("<b>5. Gateway</b>", table_cell),
            Paragraph("Merge Quality Gate", table_cell),
            Paragraph("Exits Code 1 (Blocks Merge) if critical errors exist, or Code 0 (Merge Allowed) if clean.", table_cell),
            Paragraph("Branch protection status", table_cell)
        ]
    ]
    t_workflow = Table(workflow_stages, colWidths=[65, 110, 237, 120])
    t_workflow.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, 0), colors.HexColor("#2980b9")),
        ('GRID', (0, 0), (-1, -1), 0.5, colors.HexColor("#cbd5e1")),
        ('ROWBACKGROUNDS', (0, 1), (-1, -1), [colors.HexColor("#ffffff"), colors.HexColor("#f8f9fa")]),
        ('TOPPADDING', (0, 0), (-1, -1), 1.8),
        ('BOTTOMPADDING', (0, 0), (-1, -1), 1.8),
        ('LEFTPADDING', (0, 0), (-1, -1), 5),
        ('RIGHTPADDING', (0, 0), (-1, -1), 5),
    ]))
    story.append(t_workflow)
    story.append(Spacer(1, 2.5))

    story.append(Paragraph("3. Codebase Structure & Analyzers", h1_style))
    codebase_tree = """src/
|-- CodeMonitor/
|   |-- Analyzers/                       # Core Roslyn Spectrum Analyzers
|   |   |-- ICodeAnalyzer.cs             # Common interface contract
|   |   |-- MethodLengthAnalyzer.cs      # CQ001: Method lines (>50 lines)
|   |   |-- ComplexityAnalyzer.cs        # CQ002: Cyclomatic complexity (>10)
|   |   |-- ParameterCountAnalyzer.cs    # CQ003: Parameter count (>4 params)
|   |   |-- NestingDepthAnalyzer.cs      # CQ004: Nesting depth (>3 levels)
|   |   |-- RuntimeSafetyAnalyzer.cs     # SAF001-SAF007: Nulls, zero-div, leaks, bounds, dead code, generic throws
|   |   |-- ConcurrencyAnalyzer.cs       # CON001-CON005: Deadlocks, async void, sync-over-async, unsafe locks
|   |   |-- SecurityAnalyzer.cs          # SEC001-SEC004: SQL injection, hardcoded secrets, weak crypto, XSS
|   |   |-- PerformanceAnalyzer.cs       # PERF001-PERF003: String loops, boxing, LINQ Count()
|   |   \\-- ArchitectureAnalyzer.cs      # ARCH001-ARCH008: PascalCase types/methods/props, camelCase vars, field casing
|   |-- Knowledge/RecommendationEngine.cs# Refactoring blueprints for all rules
|   |-- Services/                        # GitDiffService, GitHubReporter, EmailService
|   \\-- Program.cs                       # CLI entrypoint and Quality Gate evaluator"""
    story.append(create_code_block(codebase_tree, "Repository Architecture & Complete Directory Tree", width=532))

    # PAGE BREAK TO PAGE 2
    story.append(PageBreak())

    # PAGE 2: COMPLETE ROSLYN SPECTRUM SPECIFICATION
    story.append(Paragraph("4. Complete Roslyn Spectrum Rule Catalog", h1_style))
    story.append(Paragraph(
        "Every rule is statically verified against Roslyn AST syntax nodes and enriched with step-by-step remediation blueprints:",
        body_style
    ))

    spectrum_rules = [
        [
            Paragraph("<b>Cat. &amp; ID</b>", table_header),
            Paragraph("<b>Rule Name</b>", table_header),
            Paragraph("<b>Severity</b>", table_header),
            Paragraph("<b>Target Syntax &amp; Detection Logic</b>", table_header),
            Paragraph("<b>Engineering Remediation Blueprint</b>", table_header)
        ],
        # Category 1
        [
            Paragraph("<b>CQ001</b>", table_cell),
            Paragraph("Method Length", table_cell),
            Paragraph("Error/Warn", table_cell),
            Paragraph("<code>MethodDeclarationSyntax</code> lines &gt; 50.", table_cell),
            Paragraph("Extract cohesive private helper methods.", table_cell)
        ],
        [
            Paragraph("<b>CQ002</b>", table_cell),
            Paragraph("Cyclomatic Complexity", table_cell),
            Paragraph("Error/Warn", table_cell),
            Paragraph("Decision nodes (<code>if</code>, <code>switch</code>, loops, <code>&amp;&amp;</code>, <code>||</code>, <code>??</code>) &gt; 10.", table_cell),
            Paragraph("Flatten branching with early Guard Clauses.", table_cell)
        ],
        [
            Paragraph("<b>CQ003</b>", table_cell),
            Paragraph("Parameter Count", table_cell),
            Paragraph("Error/Warn", table_cell),
            Paragraph("<code>ParameterListSyntax</code> count &gt; 4 parameters.", table_cell),
            Paragraph("Group parameters into Parameter Object / Record.", table_cell)
        ],
        [
            Paragraph("<b>CQ004</b>", table_cell),
            Paragraph("Nesting Depth", table_cell),
            Paragraph("Error/Warn", table_cell),
            Paragraph("Nested <code>BlockSyntax</code> depth &gt; 3 levels.", table_cell),
            Paragraph("Invert outer conditionals and exit early.", table_cell)
        ],
        # Category 2
        [
            Paragraph("<b>SAF001</b>", table_cell),
            Paragraph("Deep Null Dereference", table_cell),
            Paragraph("Warning", table_cell),
            Paragraph("Chained member access (3+ dots) without <code>?.</code>.", table_cell),
            Paragraph("Use safe navigation (<code>order?.Customer?.City</code>).", table_cell)
        ],
        [
            Paragraph("<b>SAF002</b>", table_cell),
            Paragraph("Division by Zero", table_cell),
            Paragraph("<b>Error</b>", table_cell),
            Paragraph("Literal <code>0</code> or <code>0.0</code> divisor in binary <code>/</code>, <code>%</code>.", table_cell),
            Paragraph("Add non-zero guard check before division.", table_cell)
        ],
        [
            Paragraph("<b>SAF003</b>", table_cell),
            Paragraph("Resource Leak", table_cell),
            Paragraph("<b>Error</b>", table_cell),
            Paragraph("<code>IDisposable</code> types instantiated without <code>using</code>.", table_cell),
            Paragraph("Declare with <code>using var</code> or <code>using (...) { }</code>.", table_cell)
        ],
        [
            Paragraph("<b>SAF004</b>", table_cell),
            Paragraph("Array Bounds Violation", table_cell),
            Paragraph("<b>Error</b>", table_cell),
            Paragraph("Negative index literal or <code>i &lt;= arr.Length</code> loop.", table_cell),
            Paragraph("Use strict <code>&lt;</code> boundary condition.", table_cell)
        ],
        [
            Paragraph("<b>SAF005</b>", table_cell),
            Paragraph("Empty Catch Block", table_cell),
            Paragraph("Warning", table_cell),
            Paragraph("Catch block swallowing exception without logging/rethrow.", table_cell),
            Paragraph("Log exception with <code>ILogger</code> or rethrow.", table_cell)
        ],
        [
            Paragraph("<b>SAF006</b>", table_cell),
            Paragraph("Unreachable Dead Code", table_cell),
            Paragraph("Warning", table_cell),
            Paragraph("Statements following unconditional <code>return</code> or <code>throw</code>.", table_cell),
            Paragraph("Remove dead code or relocate before exit.", table_cell)
        ],
        [
            Paragraph("<b>SAF007</b>", table_cell),
            Paragraph("Generic Exception Throw", table_cell),
            Paragraph("Warning", table_cell),
            Paragraph("Throwing raw <code>new Exception(...)</code>.", table_cell),
            Paragraph("Throw specific exceptions (e.g. <code>InvalidOperationException</code>).", table_cell)
        ],
        # Category 3
        [
            Paragraph("<b>CON001</b>", table_cell),
            Paragraph("Lock Order Inversion", table_cell),
            Paragraph("<b>Error</b>", table_cell),
            Paragraph("Inverted lock acquisition across methods (A-&gt;B vs B-&gt;A).", table_cell),
            Paragraph("Establish uniform global lock acquisition order.", table_cell)
        ],
        [
            Paragraph("<b>CON002</b>", table_cell),
            Paragraph("<code>async void</code> Anti-Pattern", table_cell),
            Paragraph("<b>Error</b>", table_cell),
            Paragraph("Async method returning <code>void</code> instead of <code>Task</code>.", table_cell),
            Paragraph("Change return type to <code>Task</code> or <code>ValueTask</code>.", table_cell)
        ],
        [
            Paragraph("<b>CON003</b>", table_cell),
            Paragraph("Sync-Over-Async", table_cell),
            Paragraph("<b>Error</b>", table_cell),
            Paragraph("Blocking on Task with <code>.Result</code> or <code>.Wait()</code>.", table_cell),
            Paragraph("Replace blocking call with non-blocking <code>await</code>.", table_cell)
        ],
        [
            Paragraph("<b>CON004</b>", table_cell),
            Paragraph("Unsafe Lock Target", table_cell),
            Paragraph("Warning", table_cell),
            Paragraph("Locking on <code>this</code>, string literals, or <code>typeof</code>.", table_cell),
            Paragraph("Lock on private <code>private readonly object _lock</code>.", table_cell)
        ],
        [
            Paragraph("<b>CON005</b>", table_cell),
            Paragraph("Unawaited Task Call", table_cell),
            Paragraph("Warning", table_cell),
            Paragraph("Async method called in statement without <code>await</code>.", table_cell),
            Paragraph("Add <code>await</code> to prevent unhandled background crash.", table_cell)
        ],
        # Category 4
        [
            Paragraph("<b>SEC001</b>", table_cell),
            Paragraph("SQL Injection", table_cell),
            Paragraph("<b>Error</b>", table_cell),
            Paragraph("SQL command built via string interpolation (<code>$\"...\"</code>).", table_cell),
            Paragraph("Use parameterized queries (<code>SqlParameter</code>, Dapper).", table_cell)
        ],
        [
            Paragraph("<b>SEC002</b>", table_cell),
            Paragraph("Hardcoded Secrets", table_cell),
            Paragraph("<b>Error</b>", table_cell),
            Paragraph("Hardcoded password/token/key literals in source code.", table_cell),
            Paragraph("Move to Azure Key Vault or Environment Variables.", table_cell)
        ],
        [
            Paragraph("<b>SEC003</b>", table_cell),
            Paragraph("Weak Cryptography", table_cell),
            Paragraph("<b>Error</b>", table_cell),
            Paragraph("Insecure algorithms (<code>MD5</code>, <code>SHA1</code>, <code>DES</code>, <code>RC2</code>).", table_cell),
            Paragraph("Upgrade to <code>SHA256</code> or <code>Aes.Create()</code>.", table_cell)
        ],
        [
            Paragraph("<b>SEC004</b>", table_cell),
            Paragraph("XSS Vulnerability", table_cell),
            Paragraph("<b>Error</b>", table_cell),
            Paragraph("Unencoded dynamic strings in <code>Response.Write</code>/<code>Html.Raw</code>.", table_cell),
            Paragraph("Sanitize with <code>HtmlEncoder.Default.Encode()</code>.", table_cell)
        ],
        # Category 5
        [
            Paragraph("<b>PERF001</b>", table_cell),
            Paragraph("String <code>+=</code> in Loop", table_cell),
            Paragraph("Warning", table_cell),
            Paragraph("Repeated string concatenation inside loops.", table_cell),
            Paragraph("Use <code>StringBuilder</code> to avoid O(N^2) memory allocations.", table_cell)
        ],
        [
            Paragraph("<b>PERF002</b>", table_cell),
            Paragraph("Boxing Allocations", table_cell),
            Paragraph("Warning", table_cell),
            Paragraph("Non-generic legacy collections (<code>ArrayList</code>, <code>Hashtable</code>).", table_cell),
            Paragraph("Use generic <code>List&lt;T&gt;</code> or <code>Dictionary&lt;K,V&gt;</code>.", table_cell)
        ],
        [
            Paragraph("<b>PERF003</b>", table_cell),
            Paragraph("LINQ <code>Count() &gt; 0</code>", table_cell),
            Paragraph("Warning", table_cell),
            Paragraph("Evaluating entire sequence with <code>.Count() &gt; 0</code>.", table_cell),
            Paragraph("Replace with fast <code>.Any()</code> (exits on first element).", table_cell)
        ],
        # Category 6
        [
            Paragraph("<b>ARCH001</b>", table_cell),
            Paragraph("Interface 'I' Prefix", table_cell),
            Paragraph("Warning", table_cell),
            Paragraph("Interface declaration not starting with 'I' + PascalCase.", table_cell),
            Paragraph("Rename interface to start with 'I' (e.g. <code>IUserService</code>).", table_cell)
        ],
        [
            Paragraph("<b>ARCH002</b>", table_cell),
            Paragraph("Async Suffix Missing", table_cell),
            Paragraph("Warning", table_cell),
            Paragraph("Async method declaration not ending with 'Async'.", table_cell),
            Paragraph("Rename method with 'Async' suffix (e.g. <code>SaveAsync</code>).", table_cell)
        ],
        [
            Paragraph("<b>ARCH003</b>", table_cell),
            Paragraph("Type PascalCase Naming", table_cell),
            Paragraph("Warning", table_cell),
            Paragraph("Class/struct starting with lowercase or snake_case.", table_cell),
            Paragraph("Convert type name to standard PascalCase.", table_cell)
        ],
        [
            Paragraph("<b>ARCH004</b>", table_cell),
            Paragraph("Property PascalCase", table_cell),
            Paragraph("Warning", table_cell),
            Paragraph("Property starting with lowercase or snake_case.", table_cell),
            Paragraph("Convert property name to PascalCase.", table_cell)
        ],
        [
            Paragraph("<b>ARCH005</b>", table_cell),
            Paragraph("Variable camelCase", table_cell),
            Paragraph("Warning", table_cell),
            Paragraph("Local variables or parameters in snake_case or PascalCase.", table_cell),
            Paragraph("Convert variable identifier to camelCase.", table_cell)
        ],
        [
            Paragraph("<b>ARCH006</b>", table_cell),
            Paragraph("Obsolete API Usage", table_cell),
            Paragraph("Warning", table_cell),
            Paragraph("Calling deprecated APIs (<code>BinaryFormatter</code>, <code>Thread.Abort</code>).", table_cell),
            Paragraph("Upgrade to supported modern .NET 8 equivalents.", table_cell)
        ],
        [
            Paragraph("<b>ARCH007</b>", table_cell),
            Paragraph("Method PascalCase", table_cell),
            Paragraph("Warning", table_cell),
            Paragraph("Method starting with lowercase or containing underscores.", table_cell),
            Paragraph("Rename method to standard PascalCase.", table_cell)
        ],
        [
            Paragraph("<b>ARCH008</b>", table_cell),
            Paragraph("Field Casing Standard", table_cell),
            Paragraph("Warning", table_cell),
            Paragraph("Private/internal field in PascalCase without underscore.", table_cell),
            Paragraph("Rename field to <code>_camelCase</code> or <code>camelCase</code>.", table_cell)
        ]
    ]

    t_spectrum = Table(spectrum_rules, colWidths=[46, 100, 48, 178, 160])
    t_spectrum.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, 0), colors.HexColor("#2980b9")),
        ('GRID', (0, 0), (-1, -1), 0.4, colors.HexColor("#cbd5e1")),
        ('ROWBACKGROUNDS', (0, 1), (-1, -1), [colors.HexColor("#ffffff"), colors.HexColor("#f8f9fa")]),
        ('TOPPADDING', (0, 0), (-1, -1), 1.0),
        ('BOTTOMPADDING', (0, 0), (-1, -1), 1.0),
        ('LEFTPADDING', (0, 0), (-1, -1), 4),
        ('RIGHTPADDING', (0, 0), (-1, -1), 4),
    ]))
    story.append(t_spectrum)
    story.append(Spacer(1, 3))

    story.append(create_rtd_note(
        "Zero False-Alarm Guarantee",
        "The Roslyn AST analyzer evaluates true compiler syntax nodes with semantic scoping. Legacy unchanged files remain unimpacted, "
        "enabling seamless continuous integration and reliable automated quality enforcement across enterprise .NET repositories.",
        note_type="tip",
        width=532
    ))

    doc.build(story, canvasmaker=SphinxReadTheDocsCanvas)
    print(f"Successfully generated Sphinx/ReadTheDocs style PDF: {filename}")

if __name__ == '__main__':
    output_path = sys.argv[1] if len(sys.argv) > 1 else "Code_Quality_Monitor_ReadTheDocs.pdf"
    build_rtd_pdf(output_path)
