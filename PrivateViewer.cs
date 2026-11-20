using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

// ==============================================================================
// METADATI UFFICIALI E FIRMA INDELEBILE DELL'AUTORE (Proprietà file .EXE Windows)
// ==============================================================================
[assembly: AssemblyTitle("PrivateViewer (Stealth & School Edition)")]
[assembly: AssemblyDescription("Created by Alex Ponzin (@Ilponz) - Anti-Capture Multi-Tab Protected Workspace with AdBlock, DarkMode & Click-Through")]
[assembly: AssemblyCompany("Alex Ponzin (Ilponz)")]
[assembly: AssemblyProduct("PrivateViewer")]
[assembly: AssemblyCopyright("Copyright © 2026 Alex Ponzin (@Ilponz). All rights reserved.")]
[assembly: AssemblyTrademark("Original Software by Alex Ponzin")]
[assembly: AssemblyVersion("2.7.0.0")]
[assembly: AssemblyFileVersion("2.7.0.0")]

namespace PrivateViewerApp
{
    public class MainForm : Form, IMessageFilter
    {
        // Costanti autore
        public const string AUTHOR_NAME = "Alex Ponzin";
        public const string AUTHOR_HANDLE = "@Ilponz";
        public const string REPO_URL = "https://github.com/flydarkfenix/PrivateViewer";

        // Win32 API Constants & Functions
        private const uint WDA_NONE = 0x00000000;
        private const uint WDA_MONITOR = 0x00000001;
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MINIMIZE = 0xF020;
        private const int SC_CLOSE = 0xF060;

        private const int SW_HIDE = 0;
        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOW = 5;
        private const int SW_RESTORE = 9;

        // Window Styles per Click-Through (trasparenza completa al cursore del mouse)
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        // Global Hotkeys Win32 senza alcun conflitto con Windows (combinazioni sicure Ctrl+Alt)
        private const int WM_HOTKEY = 0x0312;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;

        private const int HOTKEY_CLICKTHROUGH = 9001; // Ctrl + Alt + X
        private const int HOTKEY_PANIC        = 9002; // Ctrl + Alt + Q
        private const int HOTKEY_OPACITY_ZERO = 9003; // Ctrl + Alt + 0 (Opacità 0% istantanea)
        private const int HOTKEY_OPACITY_FULL = 9004; // Ctrl + Alt + 9 (Ripristino opacità 100%)

        // DWM Attributes per Dark Title Bar nativo Windows
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowDisplayAffinity(IntPtr hWnd, out uint pdwAffinity);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private static int GetWindowExStyle(IntPtr hWnd)
        {
            if (IntPtr.Size == 8)
                return unchecked((int)GetWindowLongPtr64(hWnd, GWL_EXSTYLE).ToInt64());
            else
                return GetWindowLong32(hWnd, GWL_EXSTYLE);
        }

        private static void SetWindowExStyle(IntPtr hWnd, int newExStyle)
        {
            if (IntPtr.Size == 8)
                SetWindowLongPtr64(hWnd, GWL_EXSTYLE, new IntPtr(newExStyle));
            else
                SetWindowLong32(hWnd, GWL_EXSTYLE, newExStyle);
        }

        // Domini e percorsi per Blocco Ad & Tracker integrato
        private static readonly string[] AdTrackerDomains = new string[] {
            "doubleclick.net",
            "googlesyndication.com",
            "adservice.google.com",
            "google-analytics.com",
            "googletagmanager.com",
            "criteo.com",
            "criteo.net",
            "outbrain.com",
            "taboola.com",
            "adnxs.com",
            "amazon-adsystem.com",
            "scorecardresearch.com",
            "quantserve.com",
            "adroll.com",
            "popads.net",
            "propellerads.com",
            "moatads.com",
            "smartadserver.com",
            "pubmatic.com",
            "openx.net",
            "rubiconproject.com",
            "zedo.com",
            "serving-sys.com",
            "adcolony.com",
            "flashtalking.com",
            "yieldmo.com",
            "casalemedia.com",
            "indexexchange.com",
            "applovin.com",
            "vungle.com",
            "unityads.unity3d.com",
            "chartboost.com",
            "advertising.com",
            "hotjar.com",
            "clarity.ms",
            "yandex.ru/metrika",
            "mc.yandex.ru"
        };

        private static bool IsAdOrTrackerUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            string lower = url.ToLowerInvariant();
            for (int i = 0; i < AdTrackerDomains.Length; i++)
            {
                if (lower.Contains(AdTrackerDomains[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private static uint WM_SHOW_FIRST_INSTANCE;
        private static System.Threading.Mutex singleMutex;

        // Core WebView2 Shared Environment & Concurrency Controls
        private static CoreWebView2Environment sharedEnv;
        private static Task<CoreWebView2Environment> envInitTask;
        private static readonly object envLock = new object();
        private static readonly System.Threading.SemaphoreSlim wvInitSemaphore = new System.Threading.SemaphoreSlim(1, 1);
        private bool isAddingTab = false;
        private static string activeSessionFolder;

        // Top Control Panel (Unico, pulito e compatto)
        private Panel topPanel;
        private FlowLayoutPanel flowTopLeft;
        private Label lblTitle;
        private Button btnToggleAffinity;
        private Button btnOpenFile;
        private Button btnClickThrough;
        private bool isClickThrough = false;
        private Button btnDarkMode;
        private bool isForcedDark = true;
        private Button btnPanic;
        private Button btnToggleMenu;
        private Button btnMiniMode;
        private CheckBox chkAlwaysOnTop;
        private CheckBox chkHideTaskbar;
        private Panel pnlOpacity;
        private TrackBar tbOpacity;
        private Label lblOpacity;

        // Mini Mode State & Slider
        private bool isMiniMode = false;
        private Size prevNormalSize = new Size(1040, 700);
        private Point prevNormalLocation;
        private Panel pnlMiniSize;
        private TrackBar tbMiniSize;
        private Label lblMiniSize;

        // Mini bar visibile quando la barra superiore è nascosta
        private Panel pnlMiniBar;
        private Button btnRestoreMenu;
        private Label lblMiniShortcuts;
        private bool isMenuHidden = false;

        // FullScreen State
        private bool isFullScreen = false;
        private FormWindowState prevWindowState;
        private FormBorderStyle prevBorderStyle;

        // Bottom Watermark Panel (Indelebile & Responsive)
        private Panel bottomPanel;
        private Label lblFooterStatus;
        private LinkLabel lnkWatermark;

        // Tabs (Chrome-like Drag & Reorder, Close, Add)
        private TabControl tabControl;
        private TabPage tabNotes;
        private TabPage tabAddButton;
        private TextBox txtNotes;
        private int tabCounter = 1;

        // Drag & Drop delle schede
        private TabPage dragTab = null;
        private Point dragStartPoint = Point.Empty;
        private ContextMenuStrip tabContextMenu;
        private TabPage contextTargetTab = null;

        // Tooltip & Tray
        private ToolTip toolTip;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private bool isProtected = true;

        [STAThread]
        public static void Main()
        {
            WM_SHOW_FIRST_INSTANCE = RegisterWindowMessage("WM_SHOW_PRIVATEVIEWER_FLY");
            bool createdNew;
            singleMutex = new System.Threading.Mutex(true, "Local\\PrivateViewer_Fly_SingleInstance", out createdNew);

            if (!createdNew)
            {
                // Se un'istanza è già aperta, invia un messaggio broadcast per svegliarla ed esci silenziosamente
                PostMessage((IntPtr)0xffff, WM_SHOW_FIRST_INSTANCE, IntPtr.Zero, IntPtr.Zero);
                return;
            }

            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            CleanOrphanedTempSessions();
            AppDomain.CurrentDomain.ProcessExit += (s, e) => { CleanupSessionFolder(); };
            Application.Run(new MainForm());

            GC.KeepAlive(singleMutex);
        }

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "PrivateViewer - by Alex Ponzin (@Ilponz)";
            this.Size = new Size(1140, 700);
            this.MinimumSize = new Size(540, 360);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(20, 20, 20);
            this.ForeColor = Color.White;
            this.KeyPreview = true;
            this.AllowDrop = true;
            this.Opacity = 0.10; // Avvio con 90% trasparenza (10% opacità)
            this.TopMost = true; // Sempre in primo piano di default
            this.ShowInTaskbar = false; // Invisibile nella barra applicazioni di default (Stealth)

            toolTip = new ToolTip();
            toolTip.AutoPopDelay = 4000;
            toolTip.InitialDelay = 400;

            // Installa message filter globale per Boss Key (ESC), Ctrl+M, Ctrl+T/W, F11
            Application.AddMessageFilter(this);

            // Setup Tray Icon & Context Menu
            SetupTrayIcon();

            // Setup Menu contestuale tasto destro per le schede
            SetupTabContextMenu();

            // ==========================================
            // 1. Barra Superiore Unica e Compatta (Top Panel)
            // ==========================================
            topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 44;
            topPanel.BackColor = Color.FromArgb(34, 34, 34);
            topPanel.Padding = new Padding(6, 4, 6, 4);

            pnlOpacity = new Panel();
            pnlOpacity.Dock = DockStyle.Right;
            pnlOpacity.Width = 205;
            pnlOpacity.BackColor = Color.FromArgb(50, 50, 50);
            pnlOpacity.BorderStyle = BorderStyle.FixedSingle;

            lblOpacity = new Label();
            lblOpacity.Text = "Opacità: 10%";
            lblOpacity.ForeColor = Color.FromArgb(0, 220, 255);
            lblOpacity.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblOpacity.AutoSize = true;
            lblOpacity.Location = new Point(4, 10);

            tbOpacity = new TrackBar();
            tbOpacity.Minimum = 0; // Trasparenza totale supportata fino a 0%
            tbOpacity.Maximum = 100;
            tbOpacity.Value = 10;
            tbOpacity.TickStyle = TickStyle.None;
            tbOpacity.Size = new Size(100, 30);
            tbOpacity.Location = new Point(96, 6);
            tbOpacity.BackColor = Color.FromArgb(40, 40, 40);
            tbOpacity.Scroll += (s, e) => {
                this.Opacity = tbOpacity.Value / 100.0;
                lblOpacity.Text = tbOpacity.Value == 0 ? "Opacità: 0% (Zero)" : string.Format("Opacità: {0}%", tbOpacity.Value);
            };

            pnlOpacity.Controls.Add(lblOpacity);
            pnlOpacity.Controls.Add(tbOpacity);

            flowTopLeft = new FlowLayoutPanel();
            flowTopLeft.Dock = DockStyle.Fill;
            flowTopLeft.WrapContents = false;
            flowTopLeft.AutoScroll = false;
            flowTopLeft.Padding = new Padding(2);
            flowTopLeft.BackColor = Color.Transparent;

            lblTitle = new Label();
            lblTitle.Text = "🛡️ PrivateViewer";
            lblTitle.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.AutoSize = true;
            lblTitle.Margin = new Padding(4, 8, 8, 4);

            btnToggleAffinity = new Button();
            btnToggleAffinity.Text = "🛡️ Protetto";
            btnToggleAffinity.BackColor = Color.FromArgb(34, 139, 34);
            btnToggleAffinity.ForeColor = Color.White;
            btnToggleAffinity.FlatStyle = FlatStyle.Flat;
            btnToggleAffinity.FlatAppearance.BorderSize = 0;
            btnToggleAffinity.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnToggleAffinity.Size = new Size(130, 28);
            btnToggleAffinity.Margin = new Padding(3, 4, 4, 4);
            btnToggleAffinity.Cursor = Cursors.Hand;
            btnToggleAffinity.Click += BtnToggleAffinity_Click;
            toolTip.SetToolTip(btnToggleAffinity, "Attiva o disattiva la protezione da registrazione e screenshot");

            // Pulsante strumenti: Visualizzatore PDF e File locali
            btnOpenFile = CreateToolbarButton("📂 File / PDF", 100, 28, Color.FromArgb(55, 55, 55), "Apri un PDF o file locale in modalità protetta");
            btnOpenFile.Click += BtnOpenFile_Click;

            // Pulsante Clic-Attraverso (Click-Through WS_EX_TRANSPARENT)
            btnClickThrough = CreateToolbarButton("🖱️ Clic-Attraverso", 115, 28, Color.FromArgb(48, 48, 48), "Attiva/Disattiva Clic-Attraverso (Ctrl+Alt+X): i clic attraversano la finestra per interagire con le app sotto");
            btnClickThrough.Click += (s, e) => { ToggleClickThrough(); };

            // Pulsante Dark Mode Forzata Universale (Ctrl + Shift + D)
            btnDarkMode = CreateToolbarButton("🌙 Dark [ON]", 95, 28, Color.FromArgb(20, 90, 160), "Attiva/Disattiva Dark Mode forzata su tutti i siti web (Ctrl + Shift + D)");
            btnDarkMode.Click += (s, e) => { ToggleDarkMode(); };

            // Pulsante Panic Exit Istantaneo (Ctrl + Alt + Q)
            btnPanic = CreateToolbarButton("🚨 Panic", 90, 28, Color.FromArgb(160, 30, 30), "Chiusura di emergenza istantanea ed eliminazione totale tracce (Ctrl+Alt+Q)");
            btnPanic.Click += (s, e) => { PanicExit(); };

            btnToggleMenu = CreateToolbarButton("▲ Nascondi", 95, 28, Color.FromArgb(48, 48, 48), "Nascondi questa barra per vedere solo le schede e pagine (Ctrl + M)");
            btnToggleMenu.Click += (s, e) => { ToggleMenuVisibility(); };

            btnMiniMode = CreateToolbarButton("🔍 Minuscola", 95, 28, Color.FromArgb(48, 48, 48), "Riduci la schermata a finestra minuscola / compatta (Ctrl + Shift + M)");
            btnMiniMode.Click += (s, e) => { ToggleMiniMode(); };

            pnlMiniSize = new Panel();
            pnlMiniSize.Width = 175;
            pnlMiniSize.Height = 28;
            pnlMiniSize.Margin = new Padding(4, 4, 4, 4);
            pnlMiniSize.BackColor = Color.FromArgb(48, 48, 48);
            pnlMiniSize.BorderStyle = BorderStyle.FixedSingle;
            pnlMiniSize.Visible = false;

            lblMiniSize = new Label();
            lblMiniSize.Text = "Dim: 320";
            lblMiniSize.ForeColor = Color.FromArgb(255, 185, 0);
            lblMiniSize.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblMiniSize.AutoSize = true;
            lblMiniSize.Location = new Point(4, 5);

            tbMiniSize = new TrackBar();
            tbMiniSize.Minimum = 200;
            tbMiniSize.Maximum = 700;
            tbMiniSize.Value = 320;
            tbMiniSize.TickStyle = TickStyle.None;
            tbMiniSize.Size = new Size(100, 26);
            tbMiniSize.Location = new Point(68, 1);
            tbMiniSize.BackColor = Color.FromArgb(40, 40, 40);
            tbMiniSize.Scroll += (s, e) => {
                if (isMiniMode)
                {
                    int w = tbMiniSize.Value;
                    int h = (int)(w * 0.625);
                    this.Size = new Size(w, h);
                    Rectangle workingArea = Screen.FromControl(this).WorkingArea;
                    this.Location = new Point(workingArea.Right - w - 15, workingArea.Bottom - h - 15);
                    lblMiniSize.Text = string.Format("Dim: {0}", w);
                }
            };

            pnlMiniSize.Controls.Add(lblMiniSize);
            pnlMiniSize.Controls.Add(tbMiniSize);

            chkAlwaysOnTop = new CheckBox();
            chkAlwaysOnTop.Text = "Sempre in primo";
            chkAlwaysOnTop.ForeColor = Color.LightGray;
            chkAlwaysOnTop.Font = new Font("Segoe UI", 9);
            chkAlwaysOnTop.AutoSize = true;
            chkAlwaysOnTop.Checked = true;
            chkAlwaysOnTop.Margin = new Padding(6, 8, 6, 4);
            chkAlwaysOnTop.CheckedChanged += (s, e) => { this.TopMost = chkAlwaysOnTop.Checked; };

            chkHideTaskbar = new CheckBox();
            chkHideTaskbar.Text = "👻 Stealth";
            chkHideTaskbar.ForeColor = Color.YellowGreen;
            chkHideTaskbar.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            chkHideTaskbar.AutoSize = true;
            chkHideTaskbar.Checked = true;
            chkHideTaskbar.Margin = new Padding(6, 8, 6, 4);
            chkHideTaskbar.CheckedChanged += (s, e) => {
                this.ShowInTaskbar = !chkHideTaskbar.Checked;
            };
            toolTip.SetToolTip(chkHideTaskbar, "Invisibile nella barra delle applicazioni di Windows");

            flowTopLeft.Controls.Add(lblTitle);
            flowTopLeft.Controls.Add(btnToggleAffinity);
            flowTopLeft.Controls.Add(btnOpenFile);
            flowTopLeft.Controls.Add(btnClickThrough);
            flowTopLeft.Controls.Add(btnDarkMode);
            flowTopLeft.Controls.Add(btnPanic);
            flowTopLeft.Controls.Add(btnToggleMenu);
            flowTopLeft.Controls.Add(btnMiniMode);
            flowTopLeft.Controls.Add(pnlMiniSize);
            flowTopLeft.Controls.Add(chkAlwaysOnTop);
            flowTopLeft.Controls.Add(chkHideTaskbar);

            topPanel.Controls.Add(flowTopLeft);
            topPanel.Controls.Add(pnlOpacity);

            // ==========================================
            // Mini Bar (Visibile quando la barra è nascosta)
            // ==========================================
            pnlMiniBar = new Panel();
            pnlMiniBar.Dock = DockStyle.Top;
            pnlMiniBar.Height = 26;
            pnlMiniBar.BackColor = Color.FromArgb(20, 20, 20);
            pnlMiniBar.Padding = new Padding(6, 2, 6, 2);
            pnlMiniBar.Visible = false;

            btnRestoreMenu = new Button();
            btnRestoreMenu.Text = "▼ Mostra Barra (Ctrl+M)";
            btnRestoreMenu.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnRestoreMenu.FlatStyle = FlatStyle.Flat;
            btnRestoreMenu.FlatAppearance.BorderSize = 0;
            btnRestoreMenu.BackColor = Color.FromArgb(35, 35, 35);
            btnRestoreMenu.ForeColor = Color.FromArgb(0, 168, 255);
            btnRestoreMenu.Size = new Size(160, 22);
            btnRestoreMenu.Location = new Point(6, 2);
            btnRestoreMenu.Cursor = Cursors.Hand;
            btnRestoreMenu.Click += (s, e) => { ToggleMenuVisibility(); };
            toolTip.SetToolTip(btnRestoreMenu, "Riapre la barra superiore dei comandi (Ctrl + M)");

            lblMiniShortcuts = new Label();
            lblMiniShortcuts.Text = "💡 [Ctrl+M] Barra | [Ctrl+Alt+Q] Panic | [Ctrl+Alt+X] Clic-Passa | [Ctrl+Shift+D] Dark | [Ctrl+Alt+0] 0% | [Ctrl+↑/↓] Opacità | [ESC] Boss";
            lblMiniShortcuts.Font = new Font("Segoe UI", 8.5f);
            lblMiniShortcuts.ForeColor = Color.DarkGray;
            lblMiniShortcuts.Dock = DockStyle.Right;
            lblMiniShortcuts.AutoSize = true;

            pnlMiniBar.Controls.Add(btnRestoreMenu);
            pnlMiniBar.Controls.Add(lblMiniShortcuts);

            // ==========================================
            // 2. Barra Inferiore Watermark & Autore
            // ==========================================
            bottomPanel = new Panel();
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Height = 24;
            bottomPanel.BackColor = Color.FromArgb(16, 16, 16);
            bottomPanel.Padding = new Padding(6, 2, 6, 2);

            lblFooterStatus = new Label();
            lblFooterStatus.Text = "🔒 Trascina le schede per riordinarle | Click rotellina per chiudere | Trascina file/PDF per aprirli";
            lblFooterStatus.Font = new Font("Segoe UI", 8.5f);
            lblFooterStatus.ForeColor = Color.DarkGray;
            lblFooterStatus.Dock = DockStyle.Left;
            lblFooterStatus.AutoSize = true;

            lnkWatermark = new LinkLabel();
            lnkWatermark.Text = "⚡ Ideato da Alex Ponzin (@Ilponz)";
            lnkWatermark.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lnkWatermark.LinkColor = Color.FromArgb(0, 168, 255);
            lnkWatermark.ActiveLinkColor = Color.DeepSkyBlue;
            lnkWatermark.VisitedLinkColor = Color.FromArgb(0, 168, 255);
            lnkWatermark.Dock = DockStyle.Right;
            lnkWatermark.AutoSize = true;
            lnkWatermark.Cursor = Cursors.Hand;
            lnkWatermark.LinkClicked += (s, e) => {
                try { Process.Start(REPO_URL); } catch { }
            };
            toolTip.SetToolTip(lnkWatermark, "Visita il repository ufficiale di Alex Ponzin su GitHub");

            bottomPanel.Controls.Add(lblFooterStatus);
            bottomPanel.Controls.Add(lnkWatermark);

            // ==========================================
            // 3. Tab Control (Stile Chrome: Drag, Reorder, Add, Close)
            // ==========================================
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Font = new Font("Segoe UI", 9.5f);
            tabControl.AllowDrop = true;

            // Eventi per Drag and Drop delle schede (Spostare come in Chrome)
            tabControl.MouseDown += TabControl_MouseDown;
            tabControl.MouseMove += TabControl_MouseMove;
            tabControl.MouseUp += TabControl_MouseUp;
            tabControl.MouseDoubleClick += TabControl_MouseDoubleClick;

            // --- SCHEDA 1: NOTE PRIVATE ---
            tabNotes = new TabPage("📝 Note Private");
            tabNotes.BackColor = Color.FromArgb(20, 20, 20);

            Panel notesToolbar = new Panel();
            notesToolbar.Dock = DockStyle.Top;
            notesToolbar.Height = 34;
            notesToolbar.BackColor = Color.FromArgb(30, 30, 30);
            notesToolbar.Padding = new Padding(4);

            Button btnClearNotes = CreateToolbarButton("Pulisci Note", 90, 26, Color.FromArgb(55, 55, 55), "Cancella gli appunti");
            btnClearNotes.Click += (s, e) => { txtNotes.Clear(); };

            Button btnSaveNotes = CreateToolbarButton("Salva su File", 95, 26, Color.FromArgb(55, 55, 55), "Salva le note in un file .txt");
            btnSaveNotes.Click += (s, e) => {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "Text Files (*.txt)|*.txt";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(sfd.FileName, txtNotes.Text);
                        MessageBox.Show("Note salvate con successo!", "PrivateViewer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            };

            notesToolbar.Controls.Add(btnSaveNotes);
            notesToolbar.Controls.Add(btnClearNotes);

            txtNotes = new TextBox();
            txtNotes.Multiline = true;
            txtNotes.ScrollBars = ScrollBars.Both;
            txtNotes.Dock = DockStyle.Fill;
            txtNotes.BackColor = Color.FromArgb(16, 16, 16);
            txtNotes.ForeColor = Color.FromArgb(220, 220, 220);
            txtNotes.Font = new Font("Consolas", 10);
            txtNotes.BorderStyle = BorderStyle.None;
            txtNotes.Text = "// =============================================================================\r\n" +
                            "// PrivateViewer (Clean & Streamlined Edition)\r\n" +
                            "// Ideato e creato da: Alex Ponzin (@Ilponz)\r\n" +
                            "// Repository: " + REPO_URL + "\r\n" +
                            "// =============================================================================\r\n" +
                            "// • Trascina le schede per riordinarle come in un vero browser\r\n" +
                            "// • Fai clic con la rotellina (tasto centrale) su una scheda per chiuderla\r\n" +
                            "// • Fai clic sul tasto [+] a destra per aprire una nuova scheda\r\n" +
                            "// • Fai doppio clic su uno spazio vuoto della barra schede per crearne una\r\n" +
                            "// • Fai clic destro su una scheda per opzioni rapide (duplica, chiudi altre...)\r\n\r\n";

            tabNotes.Controls.Add(txtNotes);
            tabNotes.Controls.Add(notesToolbar);
            tabControl.TabPages.Add(tabNotes);

            // --- SCHEDA PERMANENTE [+] IN STILE CHROME ---
            tabAddButton = new TabPage(" ➕ ");
            tabAddButton.ToolTipText = "Nuova Scheda (Ctrl + T)";
            tabControl.TabPages.Add(tabAddButton);

            tabControl.SelectedIndexChanged += (s, e) => {
                if (tabControl.SelectedTab == tabAddButton && !isAddingTab)
                {
                    isAddingTab = true;
                    this.BeginInvoke((Action)(() => {
                        try
                        {
                            AddNewBrowserTab("https://www.google.com");
                        }
                        finally
                        {
                            isAddingTab = false;
                        }
                    }));
                }
            };

            this.Controls.Add(tabControl);
            this.Controls.Add(bottomPanel);
            this.Controls.Add(pnlMiniBar);
            this.Controls.Add(topPanel);

            // Supporto Drag and Drop di file (PDF o immagini)
            this.DragEnter += (s, e) => {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.Copy;
            };
            this.DragDrop += (s, e) => {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    foreach (string file in files)
                    {
                        AddNewBrowserTab(new Uri(file).AbsoluteUri);
                    }
                }
            };

            // Inizializzazione Display Affinity, Dark Title Bar e prima scheda
            this.HandleCreated += (s, e) => {
                ApplyProtection(isProtected);
                ApplyDarkTitleBar();
            };

            this.Load += (s, e) => {
                AddNewBrowserTab("https://www.google.com");
            };
        }

        // ==============================================================================
        // GESTIONE TRASCINAMENTO, RIORDINO E CHIUSURA SCHEDE (STILE BROWSER REALE)
        // ==============================================================================
        private void TabControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                for (int i = 0; i < tabControl.TabCount; i++)
                {
                    if (tabControl.GetTabRect(i).Contains(e.Location))
                    {
                        if (tabControl.TabPages[i] != tabAddButton)
                        {
                            dragStartPoint = e.Location;
                            dragTab = tabControl.TabPages[i];
                        }
                        break;
                    }
                }
            }
            else if (e.Button == MouseButtons.Middle)
            {
                for (int i = 0; i < tabControl.TabCount; i++)
                {
                    if (tabControl.GetTabRect(i).Contains(e.Location))
                    {
                        TabPage target = tabControl.TabPages[i];
                        if (target != tabNotes && target != tabAddButton)
                        {
                            CloseBrowserTab(target);
                        }
                        break;
                    }
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                for (int i = 0; i < tabControl.TabCount; i++)
                {
                    if (tabControl.GetTabRect(i).Contains(e.Location))
                    {
                        TabPage target = tabControl.TabPages[i];
                        if (target != tabAddButton)
                        {
                            contextTargetTab = target;
                            tabControl.SelectedTab = target;
                            tabContextMenu.Show(tabControl, e.Location);
                        }
                        break;
                    }
                }
            }
        }

        private void TabControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && dragTab != null && dragTab != tabAddButton)
            {
                if (Math.Abs(e.X - dragStartPoint.X) > 8 || Math.Abs(e.Y - dragStartPoint.Y) > 8)
                {
                    for (int i = 0; i < tabControl.TabCount; i++)
                    {
                        Rectangle rect = tabControl.GetTabRect(i);
                        TabPage hoverTab = tabControl.TabPages[i];

                        if (rect.Contains(e.Location) && hoverTab != dragTab && hoverTab != tabAddButton)
                        {
                            int targetIdx = i;
                            tabControl.TabPages.Remove(dragTab);
                            tabControl.TabPages.Insert(targetIdx, dragTab);
                            tabControl.SelectedTab = dragTab;
                            dragStartPoint = e.Location;
                            break;
                        }
                    }
                }
            }
        }

        private void TabControl_MouseUp(object sender, MouseEventArgs e)
        {
            dragTab = null;
            dragStartPoint = Point.Empty;
        }

        private void TabControl_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                bool onTab = false;
                for (int i = 0; i < tabControl.TabCount; i++)
                {
                    if (tabControl.GetTabRect(i).Contains(e.Location))
                    {
                        onTab = true;
                        break;
                    }
                }
                if (!onTab)
                {
                    AddNewBrowserTab("https://www.google.com");
                }
            }
        }

        private void SetupTabContextMenu()
        {
            tabContextMenu = new ContextMenuStrip();
            tabContextMenu.Items.Add("➕ Nuova Scheda (Ctrl+T)", null, (s, e) => AddNewBrowserTab("https://www.google.com"));
            tabContextMenu.Items.Add("📄 Duplica Scheda", null, (s, e) => {
                if (contextTargetTab != null && contextTargetTab != tabNotes)
                {
                    DuplicateBrowserTab(contextTargetTab);
                }
            });
            tabContextMenu.Items.Add("🔄 Ricarica (F5)", null, (s, e) => {
                if (contextTargetTab != null)
                {
                    ReloadBrowserTab(contextTargetTab);
                }
            });
            tabContextMenu.Items.Add(new ToolStripSeparator());
            tabContextMenu.Items.Add("✕ Chiudi Scheda (Ctrl+W)", null, (s, e) => {
                if (contextTargetTab != null && contextTargetTab != tabNotes)
                {
                    CloseBrowserTab(contextTargetTab);
                }
            });
            tabContextMenu.Items.Add("🚫 Chiudi le altre schede", null, (s, e) => {
                if (contextTargetTab != null && contextTargetTab != tabNotes)
                {
                    CloseOtherTabs(contextTargetTab);
                }
            });
            tabContextMenu.Items.Add(new ToolStripSeparator());
            tabContextMenu.Items.Add("⬅️ Sposta a Sinistra", null, (s, e) => {
                if (contextTargetTab != null) MoveTabRelative(contextTargetTab, -1);
            });
            tabContextMenu.Items.Add("➡️ Sposta a Destra", null, (s, e) => {
                if (contextTargetTab != null) MoveTabRelative(contextTargetTab, 1);
            });
        }

        private void MoveTabRelative(TabPage tab, int direction)
        {
            if (tab == tabAddButton) return;
            int curIdx = tabControl.TabPages.IndexOf(tab);
            int newIdx = curIdx + direction;
            int addIdx = tabControl.TabPages.IndexOf(tabAddButton);

            if (newIdx >= 0 && newIdx < addIdx)
            {
                tabControl.TabPages.Remove(tab);
                tabControl.TabPages.Insert(newIdx, tab);
                tabControl.SelectedTab = tab;
            }
        }

        private void DuplicateBrowserTab(TabPage sourceTab)
        {
            string url = "https://www.google.com";
            foreach (Control c in sourceTab.Controls)
            {
                if (c is WebView2)
                {
                    var wv = (WebView2)c;
                    if (wv.Source != null)
                    {
                        url = wv.Source.ToString();
                    }
                    break;
                }
            }
            AddNewBrowserTab(url);
        }

        private void ReloadBrowserTab(TabPage tab)
        {
            foreach (Control c in tab.Controls)
            {
                if (c is WebView2)
                {
                    var wv = (WebView2)c;
                    if (wv.CoreWebView2 != null) wv.Reload();
                    break;
                }
            }
        }

        private void CloseOtherTabs(TabPage keepTab)
        {
            var toClose = new List<TabPage>();
            foreach (TabPage t in tabControl.TabPages)
            {
                if (t != keepTab && t != tabNotes && t != tabAddButton)
                {
                    toClose.Add(t);
                }
            }
            foreach (var t in toClose)
            {
                CloseBrowserTab(t);
            }
        }

        // ==============================================================================
        // DARK TITLE BAR NATIVA WINDOWS
        // ==============================================================================
        private void ApplyDarkTitleBar()
        {
            try
            {
                int dark = 1;
                if (DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref dark, sizeof(int));
                }

                int captionColor = 0x00141414;
                DwmSetWindowAttribute(this.Handle, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

                int textColor = 0x00FFFFFF;
                DwmSetWindowAttribute(this.Handle, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));
            }
            catch { }
        }

        // ==============================================================================
        // MODALITÀ FOCUS / NASCONDI BARRA (Mostra solo le pagine web)
        // ==============================================================================
        public void ToggleMenuVisibility()
        {
            isMenuHidden = !isMenuHidden;
            topPanel.Visible = !isMenuHidden;
            pnlMiniBar.Visible = isMenuHidden;
        }

        public void ToggleFullScreen()
        {
            isFullScreen = !isFullScreen;
            if (isFullScreen)
            {
                prevWindowState = this.WindowState;
                prevBorderStyle = this.FormBorderStyle;
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                topPanel.Visible = false;
                pnlMiniBar.Visible = false;
                bottomPanel.Visible = false;
            }
            else
            {
                this.FormBorderStyle = prevBorderStyle;
                this.WindowState = prevWindowState;
                topPanel.Visible = !isMenuHidden;
                pnlMiniBar.Visible = isMenuHidden;
                bottomPanel.Visible = true;
            }
        }

        public void ToggleMiniMode()
        {
            this.SuspendLayout();
            try
            {
                if (!isMiniMode)
                {
                    if (isFullScreen)
                    {
                        ToggleFullScreen();
                    }
                    if (this.WindowState == FormWindowState.Maximized)
                    {
                        this.WindowState = FormWindowState.Normal;
                    }
                    prevNormalSize = this.Size;
                    prevNormalLocation = this.Location;

                    this.MinimumSize = new Size(180, 120);
                    int targetW = tbMiniSize != null ? tbMiniSize.Value : 320;
                    int targetH = (int)(targetW * 0.625);

                    // Posiziona in basso a destra sopra l'area di lavoro in un'unica operazione atomica
                    Rectangle workingArea = Screen.FromControl(this).WorkingArea;
                    this.SetBounds(workingArea.Right - targetW - 15, workingArea.Bottom - targetH - 15, targetW, targetH);

                    isMiniMode = true;

                    // Mostra slider di ridimensionamento e nascondi pulsanti ingombranti
                    if (lblTitle != null) lblTitle.Visible = false;
                    if (btnToggleAffinity != null) btnToggleAffinity.Visible = false;
                    if (btnOpenFile != null) btnOpenFile.Visible = false;
                    if (btnClickThrough != null) btnClickThrough.Visible = false;
                    if (btnDarkMode != null) btnDarkMode.Visible = false;
                    if (btnPanic != null) btnPanic.Visible = false;
                    if (btnToggleMenu != null) btnToggleMenu.Visible = false;
                    if (chkAlwaysOnTop != null) chkAlwaysOnTop.Visible = false;
                    if (chkHideTaskbar != null) chkHideTaskbar.Visible = false;
                    if (pnlMiniSize != null) pnlMiniSize.Visible = true;

                    if (btnMiniMode != null)
                    {
                        btnMiniMode.Text = "🔍 Ingrandisci";
                        btnMiniMode.BackColor = Color.FromArgb(180, 90, 0);
                        toolTip.SetToolTip(btnMiniMode, "Ripristina le dimensioni normali (Ctrl + Shift + M)");
                    }
                }
                else
                {
                    isMiniMode = false;
                    if (pnlMiniSize != null) pnlMiniSize.Visible = false;
                    if (lblTitle != null) lblTitle.Visible = true;
                    if (btnToggleAffinity != null) btnToggleAffinity.Visible = true;
                    if (btnOpenFile != null) btnOpenFile.Visible = true;
                    if (btnClickThrough != null) btnClickThrough.Visible = true;
                    if (btnDarkMode != null) btnDarkMode.Visible = true;
                    if (btnPanic != null) btnPanic.Visible = true;
                    if (btnToggleMenu != null) btnToggleMenu.Visible = true;
                    if (chkAlwaysOnTop != null) chkAlwaysOnTop.Visible = true;
                    if (chkHideTaskbar != null) chkHideTaskbar.Visible = true;

                    this.MinimumSize = new Size(540, 360);
                    if (prevNormalLocation.X >= 0 && prevNormalLocation.Y >= 0)
                    {
                        this.SetBounds(prevNormalLocation.X, prevNormalLocation.Y, prevNormalSize.Width, prevNormalSize.Height);
                    }
                    else
                    {
                        this.Size = prevNormalSize;
                    }

                    if (btnMiniMode != null)
                    {
                        btnMiniMode.Text = "🔍 Minuscola";
                        btnMiniMode.BackColor = Color.FromArgb(48, 48, 48);
                        toolTip.SetToolTip(btnMiniMode, "Riduci la schermata a finestra minuscola (Ctrl + Shift + M)");
                    }
                }
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }

        // ==============================================================================
        // GESTIONE HOTKEY GLOBALI & MODALITÀ SPECIALI (Clic-Attraverso, Dark Mode, Panic)
        // ==============================================================================
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                // Registra hotkey globali non in conflitto con Windows né con Office/browser (Ctrl+Alt)
                RegisterHotKey(this.Handle, HOTKEY_CLICKTHROUGH, MOD_CONTROL | MOD_ALT, (uint)Keys.X);
                RegisterHotKey(this.Handle, HOTKEY_PANIC, MOD_CONTROL | MOD_ALT, (uint)Keys.Q);
                RegisterHotKey(this.Handle, HOTKEY_OPACITY_ZERO, MOD_CONTROL | MOD_ALT, (uint)Keys.D0);
                RegisterHotKey(this.Handle, HOTKEY_OPACITY_FULL, MOD_CONTROL | MOD_ALT, (uint)Keys.D9);
            }
            catch { }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try
            {
                UnregisterHotKey(this.Handle, HOTKEY_CLICKTHROUGH);
                UnregisterHotKey(this.Handle, HOTKEY_PANIC);
                UnregisterHotKey(this.Handle, HOTKEY_OPACITY_ZERO);
                UnregisterHotKey(this.Handle, HOTKEY_OPACITY_FULL);
            }
            catch { }
            base.OnHandleDestroyed(e);
        }

        public void ToggleClickThrough()
        {
            isClickThrough = !isClickThrough;
            int currentStyle = GetWindowExStyle(this.Handle);
            if (isClickThrough)
            {
                SetWindowExStyle(this.Handle, currentStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
                if (btnClickThrough != null)
                {
                    btnClickThrough.Text = "🖱️ Clic Passa [ON]";
                    btnClickThrough.BackColor = Color.FromArgb(190, 85, 0);
                }
                toolTip.SetToolTip(btnClickThrough, "Clic-Attraverso ATTIVO: i clic attraversano la finestra!\nPremi [Ctrl+Alt+X] per disattivare.");
                if (lblFooterStatus != null)
                {
                    lblFooterStatus.Text = "🖱️ Clic-Attraverso ATTIVO: i clic passano alle finestre sotto | Premi [Ctrl+Alt+X] per disattivare";
                }
            }
            else
            {
                SetWindowExStyle(this.Handle, currentStyle & ~WS_EX_TRANSPARENT);
                if (btnClickThrough != null)
                {
                    btnClickThrough.Text = "🖱️ Clic-Attraverso";
                    btnClickThrough.BackColor = Color.FromArgb(48, 48, 48);
                }
                toolTip.SetToolTip(btnClickThrough, "Attiva modalità Clic-Attraverso (Ctrl+Alt+X): i clic attraversano la finestra");
                if (lblFooterStatus != null)
                {
                    lblFooterStatus.Text = "🔒 Trascina le schede per riordinarle | Click rotellina per chiudere | Trascina file/PDF per aprirli";
                }
            }
        }

        public void PanicExit()
        {
            try
            {
                this.Opacity = 0.0;
                ShowWindow(this.Handle, SW_HIDE);
                this.Visible = false;
            }
            catch { }

            try
            {
                Application.RemoveMessageFilter(this);
            }
            catch { }

            if (trayIcon != null)
            {
                try
                {
                    trayIcon.Visible = false;
                    trayIcon.Dispose();
                }
                catch { }
            }

            try { Clipboard.Clear(); } catch { }

            try
            {
                UnregisterHotKey(this.Handle, HOTKEY_CLICKTHROUGH);
                UnregisterHotKey(this.Handle, HOTKEY_PANIC);
                UnregisterHotKey(this.Handle, HOTKEY_OPACITY_ZERO);
                UnregisterHotKey(this.Handle, HOTKEY_OPACITY_FULL);
            }
            catch { }

            CleanupSessionFolder();

            Environment.Exit(0);
        }

        public void ToggleDarkMode()
        {
            isForcedDark = !isForcedDark;
            UpdateDarkModeUI();
            ApplyDarkModeToAllTabs(isForcedDark);
        }

        private void UpdateDarkModeUI()
        {
            if (btnDarkMode != null)
            {
                if (isForcedDark)
                {
                    btnDarkMode.Text = "🌙 Dark [ON]";
                    btnDarkMode.BackColor = Color.FromArgb(20, 90, 160);
                }
                else
                {
                    btnDarkMode.Text = "🌙 Dark [OFF]";
                    btnDarkMode.BackColor = Color.FromArgb(48, 48, 48);
                }
            }
        }

        private void ApplyDarkModeToTab(WebView2 wv, bool enable)
        {
            if (wv == null || wv.CoreWebView2 == null) return;
            string js;
            if (enable)
            {
                js = @"(function(){
                    var el = document.getElementById('pv-dark-mode-style');
                    if (!el) {
                        el = document.createElement('style');
                        el.id = 'pv-dark-mode-style';
                        el.textContent = 'html { filter: invert(90%) hue-rotate(180deg) !important; background-color: #121212 !important; } img, video, canvas, svg, picture, [style*=""background-image""] { filter: invert(100%) hue-rotate(180deg) !important; }';
                        (document.head || document.documentElement).appendChild(el);
                    }
                })();";
            }
            else
            {
                js = @"(function(){
                    var el = document.getElementById('pv-dark-mode-style');
                    if (el) el.remove();
                })();";
            }
            try { wv.CoreWebView2.ExecuteScriptAsync(js); } catch { }
        }

        private void ApplyDarkModeToAllTabs(bool enable)
        {
            try
            {
                foreach (TabPage tab in tabControl.TabPages)
                {
                    foreach (Control c in tab.Controls)
                    {
                        if (c is WebView2)
                        {
                            ApplyDarkModeToTab((WebView2)c, enable);
                        }
                    }
                }
            }
            catch { }
        }

        public void SetOpacityExplicit(int percent)
        {
            int clamped = Math.Max(0, Math.Min(100, percent));
            if (tbOpacity != null) tbOpacity.Value = clamped;
            this.Opacity = clamped / 100.0;
            if (lblOpacity != null)
            {
                lblOpacity.Text = clamped == 0 ? "Opacità: 0% (Zero)" : string.Format("Opacità: {0}%", clamped);
            }
        }

        private void SetupTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            var itemShow = trayMenu.Items.Add("Mostra PrivateViewer", null, (s, e) => RestoreWindow());
            itemShow.Font = new Font(trayMenu.Font, FontStyle.Bold);
            trayMenu.Items.Add("Nascondi nella Tray (Boss Key)", null, (s, e) => HideToTray());
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Schermata Minuscola On/Off (Ctrl+Shift+M)", null, (s, e) => {
                RestoreWindow();
                ToggleMiniMode();
            });
            trayMenu.Items.Add("Mostra/Nascondi Barra (Ctrl+M)", null, (s, e) => {
                RestoreWindow();
                ToggleMenuVisibility();
            });
            trayMenu.Items.Add("Nuova Scheda (Ctrl+T)", null, (s, e) => {
                RestoreWindow();
                AddNewBrowserTab("https://www.google.com");
            });
            trayMenu.Items.Add("Protezione Cattura On/Off", null, (s, e) => ApplyProtection(!isProtected));
            trayMenu.Items.Add("Clic-Attraverso On/Off (Ctrl+Alt+X)", null, (s, e) => ToggleClickThrough());
            trayMenu.Items.Add("Dark Mode Forzata On/Off (Ctrl+Shift+D)", null, (s, e) => ToggleDarkMode());
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Opacità al 100% (Ctrl+Alt+9)", null, (s, e) => {
                RestoreWindow();
                SetOpacityExplicit(100);
            });
            trayMenu.Items.Add("Opacità 0% Completamente Trasparente (Ctrl+Alt+0)", null, (s, e) => {
                SetOpacityExplicit(0);
            });
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("🚨 Panic Exit - Elimina Tracce (Ctrl+Alt+Q)", null, (s, e) => PanicExit());
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Esci da PrivateViewer", null, (s, e) => {
                trayIcon.Visible = false;
                Application.Exit();
            });

            trayIcon = new NotifyIcon();
            trayIcon.Text = "PrivateViewer - Alex Ponzin (Clicca per mostrare)";
            trayIcon.Icon = SystemIcons.Shield;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;

            trayIcon.Click += (s, e) => {
                if (e is MouseEventArgs && ((MouseEventArgs)e).Button == MouseButtons.Left)
                {
                    ToggleWindowFromTray();
                }
            };
            trayIcon.DoubleClick += (s, e) => {
                RestoreWindow();
            };
        }

        private Button CreateToolbarButton(string text, int width, int height, Color backColor, string tip)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = backColor;
            btn.ForeColor = Color.White;
            btn.Size = new Size(width, height);
            btn.Margin = new Padding(3, 2, 3, 2);
            btn.Cursor = Cursors.Hand;
            toolTip.SetToolTip(btn, tip);
            return btn;
        }

        private void BtnOpenFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Seleziona un Documento PDF o File da visualizzare";
                ofd.Filter = "File supportati (*.pdf;*.html;*.txt;*.jpg;*.png)|*.pdf;*.html;*.htm;*.txt;*.jpg;*.png;*.webp;*.svg|PDF (*.pdf)|*.pdf|Tutti i file (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string fileUrl = new Uri(ofd.FileName).AbsoluteUri;
                    AddNewBrowserTab(fileUrl);
                }
            }
        }

        private static Task<CoreWebView2Environment> GetSharedEnvAsync()
        {
            lock (envLock)
            {
                if (envInitTask == null)
                {
                    envInitTask = InitEnvironmentAsync();
                }
                return envInitTask;
            }
        }

        private static async Task<CoreWebView2Environment> InitEnvironmentAsync()
        {
            // Crea una cartella di sessione volatile e temporanea dedicata a questa singola esecuzione
            string tempRoot = Path.GetTempPath();
            string sessionFolder = Path.Combine(tempRoot, "PV_TempSession_" + Process.GetCurrentProcess().Id + "_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(sessionFolder);
            activeSessionFolder = sessionFolder;

            Exception primaryEx = null;
            CoreWebView2Environment env = null;
            try
            {
                env = await CoreWebView2Environment.CreateAsync(null, sessionFolder, null);
            }
            catch (Exception ex)
            {
                primaryEx = ex;
            }

            if (primaryEx != null)
            {
                // Fallback sicuro con ID di processo
                string fallbackFolder = Path.Combine(tempRoot, "PV_TempSession_Fallback_" + Process.GetCurrentProcess().Id);
                Directory.CreateDirectory(fallbackFolder);
                activeSessionFolder = fallbackFolder;
                env = await CoreWebView2Environment.CreateAsync(null, fallbackFolder, null);
            }

            return env;
        }

        // ==============================================================================
        // AGGIUNTA SCHEDE BROWSER (Inserite prima del pulsante permanente [+])
        // ==============================================================================
        public async void AddNewBrowserTab(string initialUrl)
        {
            TabPage tabPage = new TabPage("🌐 Scheda " + tabCounter++);
            tabPage.BackColor = Color.FromArgb(20, 20, 20);

            // Barra di navigazione responsive con TableLayoutPanel
            TableLayoutPanel navTable = new TableLayoutPanel();
            navTable.Dock = DockStyle.Top;
            navTable.Height = 36;
            navTable.BackColor = Color.FromArgb(30, 30, 30);
            navTable.Padding = new Padding(2);
            navTable.ColumnCount = 7;
            navTable.RowCount = 1;

            navTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32)); // Back
            navTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32)); // Forward
            navTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32)); // Reload
            navTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32)); // Home
            navTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // URL Box
            navTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42)); // Go
            navTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36)); // Close Tab

            Button btnBack = CreateNavButton("◀", "Indietro (Alt+Freccia Sinistra)");
            btnBack.Enabled = false;

            Button btnForward = CreateNavButton("▶", "Avanti (Alt+Freccia Destra)");
            btnForward.Enabled = false;

            Button btnReload = CreateNavButton("🔄", "Ricarica (F5)");
            Button btnHome = CreateNavButton("🏠", "Pagina iniziale (Google)");

            TextBox txtUrl = new TextBox();
            txtUrl.Text = initialUrl;
            txtUrl.Font = new Font("Segoe UI", 9.5f);
            txtUrl.Dock = DockStyle.Fill;
            txtUrl.BackColor = Color.FromArgb(20, 20, 20);
            txtUrl.ForeColor = Color.White;
            txtUrl.BorderStyle = BorderStyle.FixedSingle;
            txtUrl.Margin = new Padding(4, 5, 4, 3);
            txtUrl.Click += (s, e) => { txtUrl.SelectAll(); };

            Button btnGo = new Button();
            btnGo.Text = "➔";
            btnGo.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnGo.FlatStyle = FlatStyle.Flat;
            btnGo.FlatAppearance.BorderSize = 0;
            btnGo.ForeColor = Color.White;
            btnGo.BackColor = Color.FromArgb(0, 122, 204);
            btnGo.Dock = DockStyle.Fill;
            btnGo.Margin = new Padding(2);
            btnGo.Cursor = Cursors.Hand;
            toolTip.SetToolTip(btnGo, "Naviga o cerca su Google (Invio)");

            Button btnCloseTab = new Button();
            btnCloseTab.Text = "✕";
            btnCloseTab.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnCloseTab.FlatStyle = FlatStyle.Flat;
            btnCloseTab.FlatAppearance.BorderSize = 0;
            btnCloseTab.ForeColor = Color.LightGray;
            btnCloseTab.BackColor = Color.FromArgb(65, 30, 30);
            btnCloseTab.Dock = DockStyle.Fill;
            btnCloseTab.Margin = new Padding(2);
            btnCloseTab.Cursor = Cursors.Hand;
            toolTip.SetToolTip(btnCloseTab, "Chiudi questa scheda (Ctrl + W o rotellina)");

            navTable.Controls.Add(btnBack, 0, 0);
            navTable.Controls.Add(btnForward, 1, 0);
            navTable.Controls.Add(btnReload, 2, 0);
            navTable.Controls.Add(btnHome, 3, 0);
            navTable.Controls.Add(txtUrl, 4, 0);
            navTable.Controls.Add(btnGo, 5, 0);
            navTable.Controls.Add(btnCloseTab, 6, 0);

            WebView2 webView = new WebView2();
            webView.Dock = DockStyle.Fill;
            webView.DefaultBackgroundColor = Color.FromArgb(20, 20, 20);

            bool isTabLoading = false;

            Action executeNavigate = () => {
                string q = txtUrl.Text.Trim();
                if (string.IsNullOrEmpty(q)) return;
                if (q.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    q.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                    q.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                {
                    try { webView.CoreWebView2.Navigate(q); } catch { }
                }
                else if (!q.Contains(" ") && (q.Contains(".") || q.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)))
                {
                    try { webView.CoreWebView2.Navigate("https://" + q); } catch { }
                }
                else
                {
                    try { webView.CoreWebView2.Navigate("https://www.google.com/search?q=" + Uri.EscapeDataString(q)); } catch { }
                }
            };

            txtUrl.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    executeNavigate();
                    e.SuppressKeyPress = true;
                }
            };
            btnGo.Click += (s, e) => { executeNavigate(); };

            btnBack.Click += (s, e) => { if (webView.CanGoBack) webView.GoBack(); };
            btnForward.Click += (s, e) => { if (webView.CanGoForward) webView.GoForward(); };
            btnHome.Click += (s, e) => { if (webView.CoreWebView2 != null) webView.CoreWebView2.Navigate("https://www.google.com"); };
            btnReload.Click += (s, e) => {
                if (webView.CoreWebView2 != null)
                {
                    if (isTabLoading) webView.Stop();
                    else webView.Reload();
                }
            };

            btnCloseTab.Click += (s, e) => { CloseBrowserTab(tabPage); };

            tabPage.Controls.Add(webView);
            tabPage.Controls.Add(navTable);

            // Inserisci prima della scheda permanente [+]
            int insertIndex = tabControl.TabPages.IndexOf(tabAddButton);
            if (insertIndex >= 0)
            {
                tabControl.TabPages.Insert(insertIndex, tabPage);
            }
            else
            {
                tabControl.TabPages.Add(tabPage);
            }
            tabControl.SelectedTab = tabPage;

            try
            {
                await wvInitSemaphore.WaitAsync();
                try
                {
                    sharedEnv = await GetSharedEnvAsync();
                    await webView.EnsureCoreWebView2Async(sharedEnv);
                }
                finally
                {
                    wvInitSemaphore.Release();
                }

                webView.NavigationStarting += (s, e) => {
                    isTabLoading = true;
                    btnReload.Text = "✕";
                };

                webView.NavigationCompleted += (s, e) => {
                    isTabLoading = false;
                    btnReload.Text = "🔄";
                    btnBack.Enabled = webView.CanGoBack;
                    btnBack.ForeColor = btnBack.Enabled ? Color.White : Color.Gray;
                    btnForward.Enabled = webView.CanGoForward;
                    btnForward.ForeColor = btnForward.Enabled ? Color.White : Color.Gray;

                    if (isForcedDark)
                    {
                        ApplyDarkModeToTab(webView, true);
                    }
                };

                webView.SourceChanged += (s, e) => {
                    if (webView.Source != null && !txtUrl.Focused)
                    {
                        txtUrl.Text = webView.Source.ToString();
                    }
                };

                webView.KeyDown += (s, e) => {
                    bool ctrl = (Control.ModifierKeys & Keys.Control) == Keys.Control;
                    bool shift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
                    bool alt = (Control.ModifierKeys & Keys.Alt) == Keys.Alt;

                    if (e.KeyCode == Keys.Escape)
                    {
                        HideToTray();
                        e.Handled = true;
                    }
                    else if (e.KeyCode == Keys.Q && ctrl && (alt || shift))
                    {
                        PanicExit();
                        e.Handled = true;
                    }
                    else if (e.KeyCode == Keys.X && ctrl && (alt || shift))
                    {
                        ToggleClickThrough();
                        e.Handled = true;
                    }
                    else if (e.KeyCode == Keys.D && ctrl && shift)
                    {
                        ToggleDarkMode();
                        e.Handled = true;
                    }
                    else if ((e.KeyCode == Keys.D0 || e.KeyCode == Keys.NumPad0) && ctrl && (alt || shift))
                    {
                        SetOpacityExplicit(0);
                        e.Handled = true;
                    }
                    else if ((e.KeyCode == Keys.D9 || e.KeyCode == Keys.NumPad9) && ctrl && (alt || shift))
                    {
                        SetOpacityExplicit(100);
                        e.Handled = true;
                    }
                    else if (e.KeyCode == Keys.F11)
                    {
                        ToggleFullScreen();
                        e.Handled = true;
                    }
                    else if (ctrl && shift && e.KeyCode == Keys.M)
                    {
                        ToggleMiniMode();
                        e.Handled = true;
                    }
                    else if (ctrl && e.KeyCode == Keys.M)
                    {
                        ToggleMenuVisibility();
                        e.Handled = true;
                    }
                    else if (ctrl && e.KeyCode == Keys.T)
                    {
                        AddNewBrowserTab("https://www.google.com");
                        e.Handled = true;
                    }
                    else if (ctrl && e.KeyCode == Keys.W)
                    {
                        CloseCurrentBrowserTab();
                        e.Handled = true;
                    }
                    else if (ctrl && (e.KeyCode == Keys.Up || e.KeyCode == Keys.Add || e.KeyCode == Keys.Oemplus))
                    {
                        int newVal = Math.Min(100, tbOpacity.Value + 5);
                        SetOpacityExplicit(newVal);
                        e.Handled = true;
                    }
                    else if (ctrl && (e.KeyCode == Keys.Down || e.KeyCode == Keys.Subtract || e.KeyCode == Keys.OemMinus))
                    {
                        int newVal = Math.Max(0, tbOpacity.Value - 5);
                        SetOpacityExplicit(newVal);
                        e.Handled = true;
                    }
                };

                if (webView.CoreWebView2 != null)
                {
                    // Disattiva salvataggio password, autofill e telemetria
                    webView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
                    webView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;

                    // Blocco Ad & Tracker integrato a livello di richiesta di rete (HTTP 204 No Content)
                    webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                    webView.CoreWebView2.WebResourceRequested += (s, args) => {
                        try
                        {
                            if (IsAdOrTrackerUrl(args.Request.Uri))
                            {
                                args.Response = sharedEnv.CreateWebResourceResponse(new MemoryStream(), 204, "No Content", "");
                            }
                        }
                        catch { }
                    };

                    // Script DOM globale per catturare Boss Key (ESC), Panic Exit, Clic-Attraverso, DarkMode e Opacità ovunque nella pagina
                    await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                        window.addEventListener('keydown', function(e) {
                            if (e.key === 'Escape' || e.keyCode === 27) {
                                window.chrome.webview.postMessage('BOSS_KEY_ESC');
                            } else if (e.ctrlKey && (e.altKey || e.shiftKey) && (e.key === 'q' || e.key === 'Q' || e.keyCode === 81)) {
                                window.chrome.webview.postMessage('PANIC_EXIT');
                            } else if (e.ctrlKey && (e.altKey || e.shiftKey) && (e.key === 'x' || e.key === 'X' || e.keyCode === 88)) {
                                window.chrome.webview.postMessage('TOGGLE_CLICKTHROUGH');
                            } else if (e.ctrlKey && e.shiftKey && (e.key === 'd' || e.key === 'D' || e.keyCode === 68)) {
                                window.chrome.webview.postMessage('TOGGLE_DARKMODE');
                            } else if (e.ctrlKey && (e.altKey || e.shiftKey) && (e.key === '0' || e.keyCode === 48 || e.keyCode === 96)) {
                                window.chrome.webview.postMessage('OPACITY_ZERO');
                            } else if (e.ctrlKey && (e.altKey || e.shiftKey) && (e.key === '9' || e.keyCode === 57 || e.keyCode === 105)) {
                                window.chrome.webview.postMessage('OPACITY_FULL');
                            }
                        }, true);
                    ");

                    webView.CoreWebView2.WebMessageReceived += (s, args) => {
                        try
                        {
                            string msg = args.TryGetWebMessageAsString();
                            if (msg == "BOSS_KEY_ESC")
                            {
                                this.BeginInvoke((Action)(() => HideToTray()));
                            }
                            else if (msg == "PANIC_EXIT")
                            {
                                this.BeginInvoke((Action)(() => PanicExit()));
                            }
                            else if (msg == "TOGGLE_CLICKTHROUGH")
                            {
                                this.BeginInvoke((Action)(() => ToggleClickThrough()));
                            }
                            else if (msg == "TOGGLE_DARKMODE")
                            {
                                this.BeginInvoke((Action)(() => ToggleDarkMode()));
                            }
                            else if (msg == "OPACITY_ZERO")
                            {
                                this.BeginInvoke((Action)(() => SetOpacityExplicit(0)));
                            }
                            else if (msg == "OPACITY_FULL")
                            {
                                this.BeginInvoke((Action)(() => SetOpacityExplicit(100)));
                            }
                        }
                        catch { }
                    };

                    // Apri link esterni o target="_blank" in nuove schede anziché bloccare o crashare
                    webView.CoreWebView2.NewWindowRequested += (s, args) => {
                        args.Handled = true;
                        if (!string.IsNullOrEmpty(args.Uri))
                        {
                            this.BeginInvoke((Action)(() => AddNewBrowserTab(args.Uri)));
                        }
                    };

                    webView.CoreWebView2.DocumentTitleChanged += (s, e) => {
                        string title = webView.CoreWebView2.DocumentTitle;
                        if (!string.IsNullOrWhiteSpace(title))
                        {
                            if (title.Length > 20) title = title.Substring(0, 17) + "...";
                            tabPage.Text = "🌐 " + title;
                        }
                    };
                    webView.CoreWebView2.Navigate(initialUrl);
                }
            }
            catch (Exception ex)
            {
                // Nessun popup rumoroso a schermo intero: mostra un messaggio sobrio direttamente nella scheda
                Label lblErr = new Label();
                lblErr.Dock = DockStyle.Fill;
                lblErr.BackColor = Color.FromArgb(20, 20, 20);
                lblErr.ForeColor = Color.OrangeRed;
                lblErr.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                lblErr.TextAlign = ContentAlignment.MiddleCenter;
                lblErr.Text = "⚠️ Impossibile inizializzare WebView2 su questo PC.\r\n" + ex.Message;
                tabPage.Controls.Add(lblErr);
                lblErr.BringToFront();
            }
        }

        public void CloseBrowserTab(TabPage tab)
        {
            if (tab == tabNotes || tab == tabAddButton) return;

            WebView2 wv = null;
            foreach (Control c in tab.Controls)
            {
                if (c is WebView2) { wv = (WebView2)c; break; }
            }

            tabControl.TabPages.Remove(tab);
            if (wv != null) { try { wv.Dispose(); } catch { } }
            try { tab.Dispose(); } catch { }

            int browserCount = 0;
            foreach (TabPage t in tabControl.TabPages)
            {
                if (t != tabNotes && t != tabAddButton) browserCount++;
            }
            if (browserCount == 0)
            {
                AddNewBrowserTab("https://www.google.com");
            }
        }

        private void CloseCurrentBrowserTab()
        {
            if (tabControl.SelectedTab != null && tabControl.SelectedTab != tabNotes && tabControl.SelectedTab != tabAddButton)
            {
                CloseBrowserTab(tabControl.SelectedTab);
            }
        }

        private Button CreateNavButton(string text, string tip)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.FromArgb(45, 45, 45);
            btn.ForeColor = Color.White;
            btn.Dock = DockStyle.Fill;
            btn.Margin = new Padding(2);
            btn.Cursor = Cursors.Hand;
            toolTip.SetToolTip(btn, tip);
            return btn;
        }

        private void ApplyProtection(bool enable)
        {
            if (!enable)
            {
                SetWindowDisplayAffinity(this.Handle, WDA_NONE);
                isProtected = false;
                btnToggleAffinity.Text = "⚠️ Visibile (Disattivo)";
                btnToggleAffinity.BackColor = Color.FromArgb(160, 60, 60);
                return;
            }

            // 1. Tenta WDA_EXCLUDEFROMCAPTURE (Windows 10 2004+ / Windows 11 - trasparente)
            bool ok = SetWindowDisplayAffinity(this.Handle, WDA_EXCLUDEFROMCAPTURE);
            if (!ok)
            {
                // 2. Fallback su WDA_MONITOR (Windows 7/8/10 LTSC/scuola/VM - schermo nero alla cattura)
                ok = SetWindowDisplayAffinity(this.Handle, WDA_MONITOR);
            }

            if (ok)
            {
                isProtected = true;
                btnToggleAffinity.Text = "🛡️ Protetto da Cattura";
                btnToggleAffinity.BackColor = Color.FromArgb(34, 139, 34);
            }
            else
            {
                // 3. Se il monitor/driver del PC della scuola non supporta l'hardware affinity,
                // degrada silenziosamente SENZA bloccare l'interfaccia con popup di errore!
                isProtected = false;
                btnToggleAffinity.Text = "⚠️ Monitor Senza Affinity";
                btnToggleAffinity.BackColor = Color.FromArgb(120, 80, 0);
            }
        }

        private void BtnToggleAffinity_Click(object sender, EventArgs e)
        {
            ApplyProtection(!isProtected);
        }

        // ==============================================================================
        // GESTIONE RIPRISTINO / VISIBILITÀ DA TRAY ICON
        // ==============================================================================
        public void RestoreWindow()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            ShowWindow(this.Handle, SW_RESTORE);
            ShowWindow(this.Handle, SW_SHOW);
            if (tbOpacity != null && tbOpacity.Value == 0)
            {
                SetOpacityExplicit(15);
            }
            else
            {
                this.Opacity = tbOpacity != null ? tbOpacity.Value / 100.0 : 0.15;
            }
            SetForegroundWindow(this.Handle);
            this.BringToFront();
            this.Activate();
        }

        private void ToggleWindowFromTray()
        {
            if (!this.Visible || this.WindowState == FormWindowState.Minimized)
            {
                RestoreWindow();
            }
            else
            {
                if (Form.ActiveForm == this)
                {
                    HideToTray();
                }
                else
                {
                    RestoreWindow();
                }
            }
        }

        private void HideToTray()
        {
            this.Opacity = 0.0;
            ShowWindow(this.Handle, SW_HIDE);
            this.Visible = false;
            trayIcon.Visible = true;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Opacity = 0.0;
                ShowWindow(this.Handle, SW_HIDE);
                this.Visible = false;
            }
            if (isMiniMode && tbMiniSize != null && this.WindowState == FormWindowState.Normal)
            {
                int clamped = Math.Max(tbMiniSize.Minimum, Math.Min(tbMiniSize.Maximum, this.Width));
                tbMiniSize.Value = clamped;
                lblMiniSize.Text = string.Format("Dim: {0}", clamped);
            }
        }

        // ==============================================================================
        // GESTIONE SCORCIATOIE DA TASTIERA GLOBALI (Senza conflitti con scorciatoie di Windows)
        // ==============================================================================
        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_KEYDOWN)
            {
                Keys key = (Keys)(int)m.WParam & Keys.KeyCode;

                // Boss Key: ESC (solo se l'app ha il focus o nella pagina)
                if (key == Keys.Escape)
                {
                    HideToTray();
                    return true;
                }

                bool ctrlPressed = (Control.ModifierKeys & Keys.Control) == Keys.Control;
                bool shiftPressed = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
                bool altPressed = (Control.ModifierKeys & Keys.Alt) == Keys.Alt;

                // Panic Exit: Ctrl + Alt + Q oppure Ctrl + Shift + Q
                if (key == Keys.Q && ctrlPressed && (altPressed || shiftPressed))
                {
                    PanicExit();
                    return true;
                }

                // Clic-Attraverso: Ctrl + Alt + X oppure Ctrl + Shift + X
                if (key == Keys.X && ctrlPressed && (altPressed || shiftPressed))
                {
                    ToggleClickThrough();
                    return true;
                }

                // Dark Mode Forzata: Ctrl + Shift + D (senza toccare Ctrl+D dei preferiti)
                if (key == Keys.D && ctrlPressed && shiftPressed)
                {
                    ToggleDarkMode();
                    return true;
                }

                // Opacità 0% (Completamente Trasparente): Ctrl + Alt + 0 oppure Ctrl + Shift + 0
                if ((key == Keys.D0 || key == Keys.NumPad0) && ctrlPressed && (altPressed || shiftPressed))
                {
                    SetOpacityExplicit(0);
                    return true;
                }

                // Opacità 100%: Ctrl + Alt + 9 oppure Ctrl + Shift + 9
                if ((key == Keys.D9 || key == Keys.NumPad9) && ctrlPressed && (altPressed || shiftPressed))
                {
                    SetOpacityExplicit(100);
                    return true;
                }

                // F11: Fullscreen immersivo
                if (key == Keys.F11)
                {
                    ToggleFullScreen();
                    return true;
                }

                if (ctrlPressed)
                {
                    // Ctrl + Shift + M: Schermata Minuscola On/Off | Ctrl + M: Mostra/Nascondi Barra (Zen Mode)
                    if (key == Keys.M)
                    {
                        if (shiftPressed)
                        {
                            ToggleMiniMode();
                        }
                        else
                        {
                            ToggleMenuVisibility();
                        }
                        return true;
                    }
                    else if (key == Keys.T)
                    {
                        AddNewBrowserTab("https://www.google.com");
                        return true;
                    }
                    else if (key == Keys.W)
                    {
                        CloseCurrentBrowserTab();
                        return true;
                    }
                    else if (key == Keys.Up || key == Keys.Add || key == Keys.Oemplus)
                    {
                        int newVal = Math.Min(100, tbOpacity.Value + 5);
                        SetOpacityExplicit(newVal);
                        return true;
                    }
                    else if (key == Keys.Down || key == Keys.Subtract || key == Keys.OemMinus)
                    {
                        int newVal = Math.Max(0, tbOpacity.Value - 5);
                        SetOpacityExplicit(newVal);
                        return true;
                    }
                    else if (key == Keys.Tab)
                    {
                        if (tabControl.TabCount > 2)
                        {
                            int nextIdx = (tabControl.SelectedIndex + 1) % (tabControl.TabCount - 1);
                            tabControl.SelectedIndex = nextIdx;
                            return true;
                        }
                    }
                }
                else if (key == Keys.F1)
                {
                    ToggleMenuVisibility();
                    return true;
                }
            }
            return false;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                HideToTray();
                return true;
            }
            if (keyData == (Keys.Control | Keys.Alt | Keys.Q) || keyData == (Keys.Control | Keys.Shift | Keys.Q))
            {
                PanicExit();
                return true;
            }
            if (keyData == (Keys.Control | Keys.Alt | Keys.X) || keyData == (Keys.Control | Keys.Shift | Keys.X))
            {
                ToggleClickThrough();
                return true;
            }
            if (keyData == (Keys.Control | Keys.Shift | Keys.D))
            {
                ToggleDarkMode();
                return true;
            }
            if (keyData == (Keys.Control | Keys.Alt | Keys.D0) || keyData == (Keys.Control | Keys.Shift | Keys.D0) ||
                keyData == (Keys.Control | Keys.Alt | Keys.NumPad0) || keyData == (Keys.Control | Keys.Shift | Keys.NumPad0))
            {
                SetOpacityExplicit(0);
                return true;
            }
            if (keyData == (Keys.Control | Keys.Alt | Keys.D9) || keyData == (Keys.Control | Keys.Shift | Keys.D9) ||
                keyData == (Keys.Control | Keys.Alt | Keys.NumPad9) || keyData == (Keys.Control | Keys.Shift | Keys.NumPad9))
            {
                SetOpacityExplicit(100);
                return true;
            }
            if (keyData == Keys.F11)
            {
                ToggleFullScreen();
                return true;
            }
            if (keyData == (Keys.Control | Keys.Shift | Keys.M))
            {
                ToggleMiniMode();
                return true;
            }
            if (keyData == (Keys.Control | Keys.M) || keyData == Keys.F1)
            {
                ToggleMenuVisibility();
                return true;
            }
            if (keyData == (Keys.Control | Keys.T))
            {
                AddNewBrowserTab("https://www.google.com");
                return true;
            }
            if (keyData == (Keys.Control | Keys.W))
            {
                CloseCurrentBrowserTab();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 1. Rendi immediatamente invisibile e azzera l'opacità PRIMA di qualsiasi altra operazione!
            // Questo impedisce al 100% che la finestra ricompaia o sfarfalli sullo schermo mentre i controlli vengono rilasciati.
            try
            {
                this.Opacity = 0.0;
                ShowWindow(this.Handle, SW_HIDE);
                this.Visible = false;
            }
            catch { }

            Application.RemoveMessageFilter(this);
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }

            // Svuota la clipboard per eliminare eventuali frammenti di testo o credenziali copiate
            try { Clipboard.Clear(); } catch { }

            // Rilascia e distruggi tutti i controlli WebView2
            try
            {
                foreach (TabPage tab in tabControl.TabPages)
                {
                    foreach (Control c in tab.Controls)
                    {
                        if (c is WebView2)
                        {
                            try { ((WebView2)c).Dispose(); } catch { }
                        }
                    }
                }
            }
            catch { }

            // Cancellazione e distruzione totale della cartella della sessione
            CleanupSessionFolder();

            base.OnFormClosing(e);
        }

        private static void CleanupSessionFolder()
        {
            if (string.IsNullOrEmpty(activeSessionFolder)) return;

            // 1. Tenta la cancellazione immediata in-process
            try
            {
                if (Directory.Exists(activeSessionFolder))
                {
                    Directory.Delete(activeSessionFolder, true);
                }
            }
            catch
            {
                // 2. Se WebView2 sta ancora rilasciando i file aperti, avvia un worker nascosto ritardato di 1s che elimina la cartella
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = "cmd.exe";
                    psi.Arguments = string.Format("/c ping 127.0.0.1 -n 2 > nul & rd /s /q \"{0}\"", activeSessionFolder);
                    psi.WindowStyle = ProcessWindowStyle.Hidden;
                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = false;
                    Process.Start(psi);
                }
                catch { }
            }

            // 3. Rimuovi anche l'eventuale vecchia cartella residua in LocalAppData
            try
            {
                string oldAppData = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PrivateViewer"
                );
                if (Directory.Exists(oldAppData))
                {
                    Directory.Delete(oldAppData, true);
                }
            }
            catch { }
        }

        private static void CleanOrphanedTempSessions()
        {
            try
            {
                string tempDir = Path.GetTempPath();
                foreach (string dir in Directory.GetDirectories(tempDir, "PV_TempSession_*"))
                {
                    try { Directory.Delete(dir, true); } catch { }
                }

                string oldAppData = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PrivateViewer"
                );
                if (Directory.Exists(oldAppData))
                {
                    Directory.Delete(oldAppData, true);
                }
            }
            catch { }
        }

        protected override void WndProc(ref Message m)
        {
            if (WM_SHOW_FIRST_INSTANCE != 0 && (uint)m.Msg == WM_SHOW_FIRST_INSTANCE)
            {
                RestoreWindow();
                return;
            }
            if (m.Msg == WM_HOTKEY)
            {
                int hotkeyId = m.WParam.ToInt32();
                if (hotkeyId == HOTKEY_CLICKTHROUGH)
                {
                    ToggleClickThrough();
                    return;
                }
                if (hotkeyId == HOTKEY_PANIC)
                {
                    PanicExit();
                    return;
                }
                if (hotkeyId == HOTKEY_OPACITY_ZERO)
                {
                    SetOpacityExplicit(0);
                    return;
                }
                if (hotkeyId == HOTKEY_OPACITY_FULL)
                {
                    SetOpacityExplicit(100);
                    return;
                }
            }
            if (m.Msg == WM_SYSCOMMAND)
            {
                int cmd = (int)m.WParam & 0xFFF0;
                if (cmd == SC_MINIMIZE)
                {
                    // Evita l'animazione di Windows che fa ricomparire o lampeggiare la finestra
                    HideToTray();
                    return;
                }
                if (cmd == SC_CLOSE)
                {
                    this.Opacity = 0.0;
                    ShowWindow(this.Handle, SW_HIDE);
                    this.Visible = false;
                }
            }
            base.WndProc(ref m);
        }
    }
}
