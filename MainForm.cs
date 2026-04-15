using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace SteganografiApp
{
    public partial class MainForm : Form
    {
        private Bitmap? _loadedImage = null;
        private string? _loadedImagePath = null;

        // ── Renkler ──────────────────────────────────────────────
        private readonly Color BG_DARK   = Color.FromArgb(13,  17,  23);
        private readonly Color BG_PANEL  = Color.FromArgb(22,  27,  34);
        private readonly Color BG_CARD   = Color.FromArgb(30,  37,  46);
        private readonly Color BG_INPUT  = Color.FromArgb(13,  17,  23);
        private readonly Color ACCENT    = Color.FromArgb(88, 166, 255);
        private readonly Color ACCENT2   = Color.FromArgb(63, 185, 80);
        private readonly Color ACCENT3   = Color.FromArgb(248, 81, 73);
        private readonly Color TEXT_PRI  = Color.FromArgb(230, 237, 243);
        private readonly Color TEXT_SEC  = Color.FromArgb(125, 133, 144);
        private readonly Color BORDER    = Color.FromArgb(48,  54,  61);

        // ── Kontroller ────────────────────────────────────────────
        private TabControl tabControl = null!;
        private TabPage tabEmbed = null!, tabExtract = null!, tabAbout = null!;

        // Embed Tab
        private Panel pnlImagePreview = null!;
        private PictureBox pbPreview = null!;
        private Label lblCapacity = null!;
        private Label lblImagePath = null!;
        private RichTextBox rtbMessage = null!;
        private TextBox txtPassword = null!;
        private CheckBox chkEncrypt = null!;
        private Button btnLoadImage = null!, btnEmbed = null!, btnSaveResult = null!;
        private Bitmap? _resultBitmap = null;
        private ProgressBar pbProgress = null!;
        private Label lblStatus = null!;

        // Extract Tab
        private Panel pnlExtractPreview = null!;
        private PictureBox pbExtractPreview = null!;
        private Label lblExtractImagePath = null!;
        private TextBox txtExtractPassword = null!;
        private CheckBox chkDecrypt = null!;
        private RichTextBox rtbExtracted = null!;
        private Button btnLoadStegoImage = null!, btnExtract = null!, btnCopyMessage = null!;
        private Label lblExtractStatus = null!;

        public MainForm()
        {
            InitializeComponent();
            BuildUI();
        }

        private void BuildUI()
        {
            // ── Form ayarları ─────────────────────────────────────
            this.Text = "🔒 Steganografi - Gizli Mesaj Gömme Aracı";
            this.Size = new Size(1000, 720);
            this.MinimumSize = new Size(900, 650);
            this.BackColor = BG_DARK;
            this.ForeColor = TEXT_PRI;
            this.Font = new Font("Segoe UI", 9.5f);
            this.StartPosition = FormStartPosition.CenterScreen;

            // ── Başlık ────────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = BG_PANEL,
                Padding = new Padding(20, 0, 20, 0)
            };
            pnlHeader.Paint += (s, e) =>
            {
                var pen = new Pen(BORDER, 1);
                e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                pen.Dispose();
            };

            var lblTitle = new Label
            {
                Text = "🔏  STEGANOGRAFİ ARACI",
                ForeColor = TEXT_PRI,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 18)
            };

            var lblSubtitle = new Label
            {
                Text = "LSB Yöntemi ile Görüntüye Gizli Mesaj Gömme",
                ForeColor = TEXT_SEC,
                Font = new Font("Segoe UI", 9f),
                AutoSize = true,
                Location = new Point(226, 24)
            };

            var lblVer = new Label
            {
                Text = "v1.0",
                ForeColor = TEXT_SEC,
                Font = new Font("Segoe UI", 8f),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            lblVer.Location = new Point(pnlHeader.Width - 60, 25);
            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSubtitle, lblVer });
            pnlHeader.Resize += (s, e) => lblVer.Location = new Point(pnlHeader.Width - 60, 25);

            // ── Tab Control ───────────────────────────────────────
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(16, 8),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };

            tabEmbed   = new TabPage("  📥  MESAJ GÖMER  ");
            tabExtract = new TabPage("  📤  MESAJ ÇIKAR  ");
            tabAbout   = new TabPage("  ℹ️  HAKKINDA  ");

            foreach (var tab in new[] { tabEmbed, tabExtract, tabAbout })
            {
                tab.BackColor = BG_DARK;
                tab.ForeColor = TEXT_PRI;
                tab.BorderStyle = BorderStyle.None;
            }

            tabControl.Controls.AddRange(new TabPage[] { tabEmbed, tabExtract, tabAbout });
            DrawEmbedTab();
            DrawExtractTab();
            DrawAboutTab();

            this.Controls.Add(tabControl);
            this.Controls.Add(pnlHeader);

            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem += TabControl_DrawItem;
            tabControl.ItemSize = new Size(160, 40);
            tabControl.SizeMode = TabSizeMode.Fixed;
            tabControl.Appearance = TabAppearance.FlatButtons;
        }

        private void TabControl_DrawItem(object? sender, DrawItemEventArgs e)
        {
            var g = e.Graphics;
            var tab = tabControl.TabPages[e.Index];
            var bounds = tabControl.GetTabRect(e.Index);

            bool selected = e.Index == tabControl.SelectedIndex;

            var bgColor = selected ? ACCENT : BG_PANEL;
            var fgColor = selected ? Color.White : TEXT_SEC;

            g.FillRectangle(new SolidBrush(bgColor), bounds);

            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(tab.Text, new Font("Segoe UI", 9.5f, FontStyle.Bold), new SolidBrush(fgColor), bounds, sf);
        }

        // ══════════════════════════════════════════════════════════
        // EMBED TAB
        // ══════════════════════════════════════════════════════════
        private void DrawEmbedTab()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(15),
                BackColor = BG_DARK
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // --- Sol panel: Resim --------------------------------
            var leftPanel = CreateCard();
            leftPanel.Dock = DockStyle.Fill;
            leftPanel.Padding = new Padding(15);

            var lblImgTitle = CreateSectionLabel("🖼   KAYNAK RESİM");

            pbPreview = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = BG_INPUT,
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand
            };
            pbPreview.Paint += (s, e) =>
            {
                if (pbPreview.Image == null)
                {
                    var r = pbPreview.ClientRectangle;
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("Resim yüklemek için\ntıklayın veya sürükleyin\n\n🖼",
                        new Font("Segoe UI", 11f), new SolidBrush(TEXT_SEC), r, sf);
                }
            };
            pbPreview.Click += (s, e) => BtnLoadImage_Click(s!, e);
            pbPreview.AllowDrop = true;
            pbPreview.DragEnter += (s, e) => {
                if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                    e.Effect = DragDropEffects.Copy;
            };
            pbPreview.DragDrop += (s, e) => {
                var files = (string[]?)e.Data?.GetData(DataFormats.FileDrop);
                if (files?.Length > 0) LoadImage(files[0]);
            };

            lblImagePath = new Label
            {
                Text = "Henüz resim yüklenmedi",
                ForeColor = TEXT_SEC,
                Font = new Font("Segoe UI", 8.5f),
                Dock = DockStyle.Bottom,
                Height = 20,
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblCapacity = new Label
            {
                Text = "Kapasite: —",
                ForeColor = ACCENT2,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Dock = DockStyle.Bottom,
                Height = 20,
                TextAlign = ContentAlignment.MiddleLeft
            };

            btnLoadImage = CreateButton("📂  Resim Yükle", ACCENT, Color.White);
            btnLoadImage.Dock = DockStyle.Bottom;
            btnLoadImage.Click += BtnLoadImage_Click;

            var pnlImgContainer = new Panel { Dock = DockStyle.Fill };
            pnlImgContainer.Controls.Add(pbPreview);

            leftPanel.Controls.Add(lblImgTitle);
            leftPanel.Controls.Add(pnlImgContainer);
            leftPanel.Controls.Add(lblCapacity);
            leftPanel.Controls.Add(lblImagePath);
            leftPanel.Controls.Add(btnLoadImage);

            // --- Sağ panel: Mesaj & seçenekler ------------------
            var rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = BG_DARK };
            rightPanel.Padding = new Padding(10, 0, 0, 0);

            var msgCard = CreateCard();
            msgCard.Dock = DockStyle.Fill;
            msgCard.Padding = new Padding(15);

            var lblMsgTitle = CreateSectionLabel("✉   GİZLİ MESAJ");

            var lblMsgHint = new Label
            {
                Text = "Resme gömmek istediğiniz mesajı girin:",
                ForeColor = TEXT_SEC,
                Font = new Font("Segoe UI", 8.5f),
                Dock = DockStyle.Top,
                Height = 22
            };

            rtbMessage = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = BG_INPUT,
                ForeColor = TEXT_PRI,
                Font = new Font("Consolas", 10f),
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Padding = new Padding(5)
            };
            rtbMessage.TextChanged += (s, e) => UpdateMessageStats();

            var lblMsgStats = new Label
            {
                Text = "0 karakter  •  0 byte",
                ForeColor = TEXT_SEC,
                Font = new Font("Segoe UI", 8f),
                Dock = DockStyle.Bottom,
                Height = 20,
                Name = "lblMsgStats"
            };

            // Şifreleme seçenekleri
            var encCard = CreateCard();
            encCard.Dock = DockStyle.Bottom;
            encCard.Height = 110;
            encCard.Padding = new Padding(15, 10, 15, 10);

            var lblEncTitle = CreateSectionLabel("🔐   ŞİFRELEME (İSTEĞE BAĞLI)");

            chkEncrypt = new CheckBox
            {
                Text = "AES-256 Şifreleme Kullan",
                ForeColor = TEXT_PRI,
                Font = new Font("Segoe UI", 9.5f),
                Dock = DockStyle.Top,
                Height = 24,
                Checked = false,
                Cursor = Cursors.Hand
            };
            chkEncrypt.CheckedChanged += (s, e) => txtPassword.Enabled = chkEncrypt.Checked;

            txtPassword = new TextBox
            {
                PlaceholderText = "Parola girin...",
                PasswordChar = '●',
                Dock = DockStyle.Top,
                BackColor = BG_INPUT,
                ForeColor = TEXT_PRI,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                Height = 28,
                Enabled = false
            };

            encCard.Controls.Add(txtPassword);
            encCard.Controls.Add(chkEncrypt);
            encCard.Controls.Add(lblEncTitle);

            // Aksiyon butonları
            var btnCard = CreateCard();
            btnCard.Dock = DockStyle.Bottom;
            btnCard.Height = 60;
            btnCard.Padding = new Padding(15, 8, 15, 8);

            var btnFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            btnEmbed = CreateButton("🔒  Mesajı Göm", ACCENT2, Color.White);
            btnEmbed.Width = 160;
            btnEmbed.Click += BtnEmbed_Click;

            btnSaveResult = CreateButton("💾  Kaydet", ACCENT, Color.White);
            btnSaveResult.Width = 130;
            btnSaveResult.Enabled = false;
            btnSaveResult.Click += BtnSaveResult_Click;

            pbProgress = new ProgressBar
            {
                Width = 120,
                Height = 34,
                Style = ProgressBarStyle.Continuous,
                Visible = false,
                BackColor = BG_INPUT
            };

            btnFlow.Controls.AddRange(new Control[] { btnEmbed, btnSaveResult, pbProgress });
            btnCard.Controls.Add(btnFlow);

            lblStatus = new Label
            {
                Text = "",
                ForeColor = ACCENT2,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Dock = DockStyle.Bottom,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var msgContainer = new Panel { Dock = DockStyle.Fill };
            msgContainer.Controls.Add(rtbMessage);
            msgContainer.Controls.Add(lblMsgStats);
            msgContainer.Controls.Add(lblMsgHint);

            msgCard.Controls.Add(msgContainer);
            msgCard.Controls.Add(lblMsgTitle);

            rightPanel.Controls.Add(msgCard);
            rightPanel.Controls.Add(encCard);
            rightPanel.Controls.Add(btnCard);
            rightPanel.Controls.Add(lblStatus);

            layout.Controls.Add(leftPanel, 0, 0);
            layout.Controls.Add(rightPanel, 1, 0);

            tabEmbed.Controls.Add(layout);
        }

        // ══════════════════════════════════════════════════════════
        // EXTRACT TAB
        // ══════════════════════════════════════════════════════════
        private void DrawExtractTab()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(15),
                BackColor = BG_DARK
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // Sol: Stego resim
            var leftCard = CreateCard();
            leftCard.Dock = DockStyle.Fill;
            leftCard.Padding = new Padding(15);

            var lblExtImgTitle = CreateSectionLabel("🖼   STEGANOGRAFİK RESİM");

            pbExtractPreview = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = BG_INPUT,
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand
            };
            pbExtractPreview.Paint += (s, e) =>
            {
                if (pbExtractPreview.Image == null)
                {
                    var r = pbExtractPreview.ClientRectangle;
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("Steganografik resmi\nyüklemek için tıklayın\n\n🔍",
                        new Font("Segoe UI", 11f), new SolidBrush(TEXT_SEC), r, sf);
                }
            };
            pbExtractPreview.Click += (s, e) => BtnLoadStegoImage_Click(s!, e);
            pbExtractPreview.AllowDrop = true;
            pbExtractPreview.DragEnter += (s, e) => {
                if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                    e.Effect = DragDropEffects.Copy;
            };
            pbExtractPreview.DragDrop += (s, e) => {
                var files = (string[]?)e.Data?.GetData(DataFormats.FileDrop);
                if (files?.Length > 0) LoadStegoImage(files[0]);
            };

            lblExtractImagePath = new Label
            {
                Text = "Henüz resim yüklenmedi",
                ForeColor = TEXT_SEC,
                Font = new Font("Segoe UI", 8.5f),
                Dock = DockStyle.Bottom,
                Height = 20
            };

            btnLoadStegoImage = CreateButton("📂  Resim Yükle", ACCENT, Color.White);
            btnLoadStegoImage.Dock = DockStyle.Bottom;
            btnLoadStegoImage.Click += BtnLoadStegoImage_Click;

            var extImgContainer = new Panel { Dock = DockStyle.Fill };
            extImgContainer.Controls.Add(pbExtractPreview);

            leftCard.Controls.Add(lblExtImgTitle);
            leftCard.Controls.Add(extImgContainer);
            leftCard.Controls.Add(lblExtractImagePath);
            leftCard.Controls.Add(btnLoadStegoImage);

            // Sağ: Çıkarılan mesaj
            var rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = BG_DARK };
            rightPanel.Padding = new Padding(10, 0, 0, 0);

            var decCard = CreateCard();
            decCard.Dock = DockStyle.Bottom;
            decCard.Height = 110;
            decCard.Padding = new Padding(15, 10, 15, 10);

            var lblDecTitle = CreateSectionLabel("🔐   ŞİFRE ÇÖZME");

            chkDecrypt = new CheckBox
            {
                Text = "Şifreli Mesaj (AES-256 ile Çöz)",
                ForeColor = TEXT_PRI,
                Font = new Font("Segoe UI", 9.5f),
                Dock = DockStyle.Top,
                Height = 24,
                Cursor = Cursors.Hand
            };
            chkDecrypt.CheckedChanged += (s, e) => txtExtractPassword.Enabled = chkDecrypt.Checked;

            txtExtractPassword = new TextBox
            {
                PlaceholderText = "Parola girin...",
                PasswordChar = '●',
                Dock = DockStyle.Top,
                BackColor = BG_INPUT,
                ForeColor = TEXT_PRI,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f),
                Height = 28,
                Enabled = false
            };

            decCard.Controls.Add(txtExtractPassword);
            decCard.Controls.Add(chkDecrypt);
            decCard.Controls.Add(lblDecTitle);

            // Sonuç
            var resultCard = CreateCard();
            resultCard.Dock = DockStyle.Fill;
            resultCard.Padding = new Padding(15);

            var lblResultTitle = CreateSectionLabel("📋   ÇIKARILAN MESAJ");

            rtbExtracted = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = BG_INPUT,
                ForeColor = TEXT_PRI,
                Font = new Font("Consolas", 10f),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            resultCard.Controls.Add(rtbExtracted);
            resultCard.Controls.Add(lblResultTitle);

            // Butonlar
            var btnCard2 = CreateCard();
            btnCard2.Dock = DockStyle.Bottom;
            btnCard2.Height = 60;
            btnCard2.Padding = new Padding(15, 8, 15, 8);

            var btnFlow2 = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight
            };

            btnExtract = CreateButton("🔍  Mesajı Çıkar", ACCENT3, Color.White);
            btnExtract.Width = 160;
            btnExtract.Click += BtnExtract_Click;

            btnCopyMessage = CreateButton("📋  Kopyala", ACCENT, Color.White);
            btnCopyMessage.Width = 120;
            btnCopyMessage.Enabled = false;
            btnCopyMessage.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(rtbExtracted.Text))
                {
                    Clipboard.SetText(rtbExtracted.Text);
                    lblExtractStatus.Text = "✅ Mesaj panoya kopyalandı!";
                }
            };

            lblExtractStatus = new Label
            {
                Text = "",
                ForeColor = ACCENT2,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Dock = DockStyle.Bottom,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft
            };

            btnFlow2.Controls.AddRange(new Control[] { btnExtract, btnCopyMessage });
            btnCard2.Controls.Add(btnFlow2);

            rightPanel.Controls.Add(resultCard);
            rightPanel.Controls.Add(decCard);
            rightPanel.Controls.Add(btnCard2);
            rightPanel.Controls.Add(lblExtractStatus);

            layout.Controls.Add(leftCard, 0, 0);
            layout.Controls.Add(rightPanel, 1, 0);
            tabExtract.Controls.Add(layout);
        }

        // ══════════════════════════════════════════════════════════
        // ABOUT TAB
        // ══════════════════════════════════════════════════════════
        private void DrawAboutTab()
        {
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = BG_DARK,
                Padding = new Padding(40, 30, 40, 30)
            };

            var card = CreateCard();
            card.Dock = DockStyle.Top;
            card.AutoSize = true;
            card.Padding = new Padding(40, 30, 40, 30);

            var content = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            void AddLabel(string text, Font font, Color color, int padding = 0)
            {
                var lbl = new Label
                {
                    Text = text,
                    Font = font,
                    ForeColor = color,
                    AutoSize = true,
                    Padding = new Padding(0, padding, 0, 0),
                    MaximumSize = new Size(700, 0)
                };
                content.Controls.Add(lbl);
            }

            AddLabel("🔏 STEGANOGRAFİ ARACI", new Font("Segoe UI", 20f, FontStyle.Bold), TEXT_PRI, 0);
            AddLabel("Görüntülere Gizli Mesaj Gömme Uygulaması", new Font("Segoe UI", 11f), TEXT_SEC, 4);

            AddLabel("──────────────────────────────────────────", new Font("Segoe UI", 10f), BORDER, 15);

            AddLabel("📌 HAKKINDA", new Font("Segoe UI", 12f, FontStyle.Bold), ACCENT, 15);
            AddLabel("Bu uygulama, LSB (Least Significant Bit) steganografi yöntemiyle\nresim dosyalarına gizli metin mesajları gömmenizi sağlar.\nGömülen mesajlar çıplak gözle fark edilemez.", new Font("Segoe UI", 10f), TEXT_PRI, 6);

            AddLabel("🔬 LSB YÖNTEMİ NASIL ÇALIŞIR?", new Font("Segoe UI", 12f, FontStyle.Bold), ACCENT, 20);
            AddLabel("Her piksel R, G, B kanallarından oluşur (0-255 arası).\nHer kanalın en anlamsız biti (LSB) değiştirilerek gizli veri saklanır.\nBu değişiklik görsel kaliteyi neredeyse hiç etkilemez.\nÖrnek: 11001010 → 11001011 (1 bit değişti, renk farkı: 1/255)", new Font("Segoe UI", 10f), TEXT_PRI, 6);

            AddLabel("🔐 ŞİFRELEME", new Font("Segoe UI", 12f, FontStyle.Bold), ACCENT, 20);
            AddLabel("İsteğe bağlı olarak AES-256-CBC şifrelemesi kullanılır.\nAnahtar türetme için PBKDF2-SHA256 (10.000 iterasyon) uygulanır.\nBu sayede mesajınız hem gizlenir hem de şifrelenir.", new Font("Segoe UI", 10f), TEXT_PRI, 6);

            AddLabel("📋 DESTEKLENEN FORMATLAR", new Font("Segoe UI", 12f, FontStyle.Bold), ACCENT, 20);
            AddLabel("Giriş:  PNG, BMP, TIFF (kayıpsız sıkıştırma)\nÇıkış: PNG (zorunlu - JPEG kalite kaybı verileri bozar!)\n⚠️ JPEG formatı veri kaybına neden olduğundan desteklenmez.", new Font("Segoe UI", 10f), TEXT_PRI, 6);

            AddLabel("──────────────────────────────────────────", new Font("Segoe UI", 10f), BORDER, 15);
            AddLabel("Geliştirici: Steganografi Uygulaması  •  C# .NET  •  WinForms", new Font("Segoe UI", 8.5f), TEXT_SEC, 10);

            card.Controls.Add(content);
            scroll.Controls.Add(card);
            tabAbout.Controls.Add(scroll);
        }

        // ══════════════════════════════════════════════════════════
        // OLAY YÖNETİCİLERİ
        // ══════════════════════════════════════════════════════════

        private void BtnLoadImage_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Kaynak Resim Seç",
                Filter = "Resim Dosyaları|*.png;*.bmp;*.tiff;*.tif|Tüm Dosyalar|*.*"
            };
            if (ofd.ShowDialog() == DialogResult.OK)
                LoadImage(ofd.FileName);
        }

        private void LoadImage(string path)
        {
            try
            {
                _loadedImage?.Dispose();
                _loadedImage = new Bitmap(path);
                _loadedImagePath = path;
                pbPreview.Image = _loadedImage;

                string fileName = Path.GetFileName(path);
                lblImagePath.Text = $"📄 {fileName}  ({_loadedImage.Width}×{_loadedImage.Height})";

                // SteganographyEngine artık Türkçe isimlerle erişiliyor
                int cap = SteganographyEngine.KapasiteHesapla(_loadedImage);
                lblCapacity.Text = $"📦 Kapasite: ~{cap:N0} byte  ({cap / 1024:N0} KB)";
                lblCapacity.ForeColor = ACCENT2;

                lblStatus.Text = "✅ Resim yüklendi!";
                _resultBitmap = null;
                btnSaveResult.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Resim yüklenemedi:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnEmbed_Click(object sender, EventArgs e)
        {
            if (_loadedImage == null)
            {
                lblStatus.Text = "⚠️ Önce bir resim yükleyin!";
                lblStatus.ForeColor = Color.Orange;
                return;
            }
            if (string.IsNullOrWhiteSpace(rtbMessage.Text))
            {
                lblStatus.Text = "⚠️ Mesaj boş olamaz!";
                lblStatus.ForeColor = Color.Orange;
                return;
            }
            if (chkEncrypt.Checked && string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                lblStatus.Text = "⚠️ Şifreleme için parola girin!";
                lblStatus.ForeColor = Color.Orange;
                return;
            }

            try
            {
                btnEmbed.Enabled = false;
                pbProgress.Visible = true;
                pbProgress.Style = ProgressBarStyle.Marquee;
                lblStatus.Text = "⏳ Mesaj gömülüyor...";
                lblStatus.ForeColor = ACCENT;

                string message = rtbMessage.Text;

                if (chkEncrypt.Checked)
                {
                    // EncryptionHelper artık Sifrele adındaki metodu kullanıyor
                    message = "ENC:" + EncryptionHelper.Sifrele(message, txtPassword.Text);
                }

                _resultBitmap?.Dispose();
                // SteganographyEngine artık MesajGom adındaki metodu kullanıyor
                _resultBitmap = SteganographyEngine.MesajGom(_loadedImage, message);

                lblStatus.Text = "✅ Mesaj başarıyla gömüldü! Kaydetmek için 💾 butonuna tıklayın.";
                lblStatus.ForeColor = ACCENT2;
                btnSaveResult.Enabled = true;
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"❌ Hata: {ex.Message}";
                lblStatus.ForeColor = ACCENT3;
            }
            finally
            {
                btnEmbed.Enabled = true;
                pbProgress.Visible = false;
            }
        }

        private void BtnSaveResult_Click(object sender, EventArgs e)
        {
            if (_resultBitmap == null) return;

            using var sfd = new SaveFileDialog
            {
                Title = "Sonuç Resmi Kaydet",
                Filter = "PNG Dosyası|*.png",
                DefaultExt = "png",
                FileName = "stego_image"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    _resultBitmap.Save(sfd.FileName, ImageFormat.Png);
                    lblStatus.Text = $"✅ Kaydedildi: {Path.GetFileName(sfd.FileName)}";
                    lblStatus.ForeColor = ACCENT2;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Kaydetme hatası:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnLoadStegoImage_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Steganografik Resim Seç",
                Filter = "Resim Dosyaları|*.png;*.bmp;*.tiff;*.tif|Tüm Dosyalar|*.*"
            };
            if (ofd.ShowDialog() == DialogResult.OK)
                LoadStegoImage(ofd.FileName);
        }

        private void LoadStegoImage(string path)
        {
            try
            {
                pbExtractPreview.Image?.Dispose();
                pbExtractPreview.Image = new Bitmap(path);
                lblExtractImagePath.Text = $"📄 {Path.GetFileName(path)}";
                lblExtractStatus.Text = "✅ Resim yüklendi!";
                lblExtractStatus.ForeColor = ACCENT2;
                rtbExtracted.Clear();
                btnCopyMessage.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Resim yüklenemedi:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExtract_Click(object sender, EventArgs e)
        {
            if (pbExtractPreview.Image == null)
            {
                lblExtractStatus.Text = "⚠️ Önce bir resim yükleyin!";
                lblExtractStatus.ForeColor = Color.Orange;
                return;
            }
            if (chkDecrypt.Checked && string.IsNullOrWhiteSpace(txtExtractPassword.Text))
            {
                lblExtractStatus.Text = "⚠️ Şifre çözme için parola girin!";
                lblExtractStatus.ForeColor = Color.Orange;
                return;
            }

            try
            {
                btnExtract.Enabled = false;
                lblExtractStatus.Text = "⏳ Mesaj çıkarılıyor...";
                lblExtractStatus.ForeColor = ACCENT;
                rtbExtracted.Clear();

                var stego = (Bitmap)pbExtractPreview.Image;
                // SteganographyEngine artık MesajCikar adındaki metodu kullanıyor
                string extracted = SteganographyEngine.MesajCikar(stego);

                if (extracted.StartsWith("ENC:"))
                {
                    extracted = extracted.Substring(4);
                    if (chkDecrypt.Checked)
                    {
                        // EncryptionHelper artık SifreCoz adındaki metodu kullanıyor
                        extracted = EncryptionHelper.SifreCoz(extracted, txtExtractPassword.Text);
                        lblExtractStatus.Text = "✅ Mesaj başarıyla çıkarıldı ve şifre çözüldü!";
                    }
                    else
                    {
                        lblExtractStatus.Text = "⚠️ Mesaj şifreli! Şifre çözme seçeneğini aktif edin.";
                        lblExtractStatus.ForeColor = Color.Orange;
                    }
                }
                else
                {
                    lblExtractStatus.Text = "✅ Mesaj başarıyla çıkarıldı!";
                    lblExtractStatus.ForeColor = ACCENT2;
                }

                rtbExtracted.Text = extracted;
                btnCopyMessage.Enabled = true;
            }
            catch (Exception ex)
            {
                lblExtractStatus.Text = $"❌ {ex.Message}";
                lblExtractStatus.ForeColor = ACCENT3;
            }
            finally
            {
                btnExtract.Enabled = true;
            }
        }

        private void UpdateMessageStats()
        {
            int chars = rtbMessage.Text.Length;
            int bytes = System.Text.Encoding.UTF8.GetByteCount(rtbMessage.Text);
            var statsLabel = tabEmbed.Controls.Find("lblMsgStats", true);
            if (statsLabel.Length > 0)
                statsLabel[0].Text = $"{chars:N0} karakter  •  {bytes:N0} byte";
        }

        // ══════════════════════════════════════════════════════════
        // YARDIMCI METODLAR
        // ══════════════════════════════════════════════════════════

        private Panel CreateCard()
        {
            var p = new Panel
            {
                BackColor = BG_CARD,
                Padding = new Padding(0)
            };
            p.Paint += (s, e) =>
            {
                var pen = new Pen(BORDER, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
                pen.Dispose();
            };
            return p;
        }

        private Label CreateSectionLabel(string text)
        {
            return new Label
            {
                Text = text,
                ForeColor = TEXT_SEC,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0, 4, 0, 0)
            };
        }

        private Button CreateButton(string text, Color bgColor, Color fgColor)
        {
            var btn = new Button
            {
                Text = text,
                BackColor = bgColor,
                ForeColor = fgColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Height = 36,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(bgColor, 0.15f);
            btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(bgColor, 0.1f);
            return btn;
        }
    }
}
