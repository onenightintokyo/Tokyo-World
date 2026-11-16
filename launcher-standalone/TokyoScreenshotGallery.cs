using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;

namespace TokyoWorldLauncher;

/// <summary>
/// Fully offline screenshot gallery for the selected Tokyo World installation.
/// Paged thumbnails keep the UI responsive and every preview is loaded without file locks.
/// </summary>
internal sealed class TokyoScreenshotGallery : UserControl
{
    private readonly string gameRoot;
    private readonly FlowLayoutPanel thumbnails = new();
    private readonly PictureBox large = new();
    private readonly Label details = CrimsonTheme.Label("Wybierz zrzut ekranu", 10);
    private readonly Label count = CrimsonTheme.Label("", 9);
    private readonly Button previous = CrimsonTheme.Button("‹ Poprzednie", 125);
    private readonly Button next = CrimsonTheme.Button("Następne ›", 125);
    private readonly Button open = CrimsonTheme.Button("Otwórz", 105);
    private readonly Button export = CrimsonTheme.Button("Eksportuj", 125);
    private readonly Button delete = CrimsonTheme.Button("Usuń", 105);
    private readonly List<string> paths = new();
    private string? selected;
    private int page;
    private const int PageSize = 36;

    private string ScreenshotFolder => Path.Combine(gameRoot, "screenshots");

    public TokyoScreenshotGallery(string root)
    {
        gameRoot = root;
        BackColor = CrimsonTheme.Background;
        // WinForms applies Anchor offsets relative to the size at creation.
        // Use the actual page size BEFORE positioning anchored controls.
        Size = new Size(1067, 505);
        Dock = DockStyle.Fill;
        var title = CrimsonTheme.Label("SCREENSHOTY", 20, true);
        title.SetBounds(24, 16, 400, 39);
        title.AutoSize = false;
        Controls.Add(title);

        var refresh = CrimsonTheme.Button("Odśwież", 115);
        refresh.SetBounds(20, 62, 116, 37);
        refresh.Click += (_, _) => Reload();
        Controls.Add(refresh);
        var openFolder = CrimsonTheme.Button("Folder zdjęć", 144);
        openFolder.SetBounds(144, 62, 145, 37);
        openFolder.Click += (_, _) =>
        {
            Directory.CreateDirectory(ScreenshotFolder);
            Process.Start(new ProcessStartInfo(ScreenshotFolder) { UseShellExecute = true });
        };
        Controls.Add(openFolder);

        count.AutoSize = false;
        count.SetBounds(306, 68, 435, 26);
        Controls.Add(count);

        thumbnails.SetBounds(18, 114, 606, 338);
        thumbnails.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
        thumbnails.AutoScroll = true;
        thumbnails.BackColor = CrimsonTheme.Surface;
        thumbnails.BorderStyle = BorderStyle.FixedSingle;
        thumbnails.Padding = new Padding(8);
        Controls.Add(thumbnails);

        var preview = new CrimsonCard();
        preview.SetBounds(638, 114, 379, 338);
        preview.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        Controls.Add(preview);

        large.SetBounds(18, 18, 343, 230);
        large.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        large.SizeMode = PictureBoxSizeMode.Zoom;
        large.BackColor = CrimsonTheme.Background;
        large.DoubleClick += (_, _) => OpenSelected();
        preview.Controls.Add(large);

        details.AutoSize = false;
        details.SetBounds(18, 254, 343, 33);
        details.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        details.AutoEllipsis = true;
        details.ForeColor = CrimsonTheme.Foreground;
        preview.Controls.Add(details);

        open.SetBounds(14, 288, 96, 35);
        open.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        open.Click += (_, _) => OpenSelected();
        preview.Controls.Add(open);
        export.SetBounds(114, 288, 110, 35);
        export.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        export.Click += (_, _) => ExportSelected();
        preview.Controls.Add(export);
        delete.SetBounds(228, 288, 96, 35);
        delete.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        delete.Click += (_, _) => DeleteSelected();
        preview.Controls.Add(delete);

        previous.SetBounds(20, 461, 125, 35);
        previous.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
        previous.Click += (_, _) => { if (page > 0) { page--; DrawPage(); } };
        Controls.Add(previous);
        next.SetBounds(158, 461, 125, 35);
        next.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
        next.Click += (_, _) => { if ((page + 1) * PageSize < paths.Count) { page++; DrawPage(); } };
        Controls.Add(next);
        Reload();
    }

    private void Reload()
    {
        Directory.CreateDirectory(ScreenshotFolder);
        paths.Clear();
        paths.AddRange(Directory.EnumerateFiles(ScreenshotFolder)
            .Where(f => Path.GetExtension(f).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".bmp")
            .OrderByDescending(File.GetLastWriteTimeUtc));
        page = Math.Min(page, Math.Max(0, (paths.Count - 1) / PageSize));
        ClearSelection();
        DrawPage();
    }

    private void DrawPage()
    {
        foreach (Control item in thumbnails.Controls)
        {
            foreach (var pb in item.Controls.OfType<PictureBox>())
            {
                var image = pb.Image;
                pb.Image = null;
                image?.Dispose();
            }
        }
        thumbnails.Controls.Clear();
        previous.Enabled = page > 0;
        next.Enabled = (page + 1) * PageSize < paths.Count;
        count.Text = paths.Count == 0 ? "Brak screenshotów. Zrób zdjęcie w grze klawiszem F2."
            : $"Zapisane obrazy: {paths.Count}   •   Strona {page + 1}/{(paths.Count + PageSize - 1) / PageSize}";

        foreach (string path in paths.Skip(page * PageSize).Take(PageSize))
        {
            string current = path;
            var tile = new Panel
            {
                Size = new Size(180, 146),
                Margin = new Padding(4),
                BackColor = CrimsonTheme.Surface2,
                Cursor = Cursors.Hand
            };
            var picture = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(5, 5),
                Size = new Size(170, 112),
                BackColor = CrimsonTheme.Background
            };
            try
            {
                using var image = LoadImageWithoutLock(path);
                picture.Image = image.GetThumbnailImage(170, 112, () => false, IntPtr.Zero);
            }
            catch (Exception)
            {
                picture.BackColor = Color.FromArgb(79, 27, 46);
            }
            var caption = CrimsonTheme.Label(Path.GetFileName(current), 8);
            caption.SetBounds(5, 122, 168, 20);
            caption.AutoSize = false;
            caption.AutoEllipsis = true;
            tile.Controls.Add(picture);
            tile.Controls.Add(caption);
            tile.Click += (_, _) => Select(current);
            picture.Click += (_, _) => Select(current);
            caption.Click += (_, _) => Select(current);
            picture.DoubleClick += (_, _) => { Select(current); OpenSelected(); };
            thumbnails.Controls.Add(tile);
        }
    }

    private static Image LoadImageWithoutLock(string path)
    {
        using var stream = new MemoryStream(File.ReadAllBytes(path));
        using var source = Image.FromStream(stream);
        return new Bitmap(source);
    }

    private void Select(string path)
    {
        if (!File.Exists(path)) { Reload(); return; }
        selected = path;
        try
        {
            var replacement = LoadImageWithoutLock(path);
            var old = large.Image;
            large.Image = replacement;
            old?.Dispose();
            var file = new FileInfo(path);
            details.Text = $"{file.Name}  •  {file.Length / 1024d / 1024d:0.0} MB";
        }
        catch (Exception e)
        {
            details.Text = "Nie można otworzyć: " + e.Message;
        }
    }

    private void ClearSelection()
    {
        selected = null;
        var old = large.Image;
        large.Image = null;
        old?.Dispose();
        details.Text = "Wybierz screenshot, aby zobaczyć podgląd.";
    }

    private void OpenSelected()
    {
        if (selected == null) return;
        if (!File.Exists(selected)) { Reload(); return; }
        Process.Start(new ProcessStartInfo(selected) { UseShellExecute = true });
    }

    private void ExportSelected()
    {
        if (selected == null || !File.Exists(selected)) return;
        using var save = new SaveFileDialog
        {
            Title = "Eksportuj screenshot",
            Filter = "Obrazy|*.png;*.jpg;*.jpeg;*.bmp|Wszystkie pliki|*.*",
            FileName = Path.GetFileName(selected)
        };
        if (save.ShowDialog(this) == DialogResult.OK)
        {
            try { File.Copy(selected, save.FileName, overwrite: false); }
            catch (Exception e) { MessageBox.Show(this, e.Message, "Eksport screenshotu"); }
        }
    }

    private void DeleteSelected()
    {
        if (selected == null || !File.Exists(selected)) return;
        if (MessageBox.Show(this, "Przenieść screenshot do kosza Windows?",
            "Tokyo World — screenshoty", MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            FileSystem.DeleteFile(selected, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            Reload();
        }
        catch (Exception e) { MessageBox.Show(this, e.Message, "Usuwanie screenshotu"); }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ClearSelection();
            foreach (Control item in thumbnails.Controls)
                foreach (var pb in item.Controls.OfType<PictureBox>())
                    pb.Image?.Dispose();
        }
        base.Dispose(disposing);
    }
}
