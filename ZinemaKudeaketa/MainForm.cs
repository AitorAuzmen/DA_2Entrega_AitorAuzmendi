namespace ZinemaKudeaketa;

public sealed class MainForm : Form
{
    private static readonly Color Fondo = Color.FromArgb(18, 24, 34);
    private static readonly Color Panela = Color.FromArgb(28, 36, 50);
    private static readonly Color PanelaArgia = Color.FromArgb(36, 46, 63);
    private static readonly Color Testua = Color.FromArgb(235, 240, 248);
    private static readonly Color TestuApala = Color.FromArgb(170, 181, 198);
    private static readonly Color Urdina = Color.FromArgb(74, 144, 226);
    private static readonly Color Gorria = Color.FromArgb(210, 77, 77);

    private readonly ZinemaRepository repository = new();
    private readonly ListView movieList = new();
    private readonly DataGridView adminGrid = new();
    private readonly Label selectedMovieLabel = new();
    private readonly Label seatsLabel = new();
    private readonly TextBox reservationNameInput = new();
    private readonly NumericUpDown reservationSeatsInput = new();
    private readonly TextBox movieNameInput = new();
    private readonly NumericUpDown movieSeatsInput = new();
    private readonly CheckBox deletedInput = new();
    private readonly Button reserveButton = new();
    private readonly Button saveButton = new();
    private readonly Button clearButton = new();
    private readonly Button softDeleteButton = new();
    private readonly Button hardDeleteButton = new();

    private Pelikula? selectedMovie;
    private Pelikula? selectedAdminMovie;

    public MainForm()
    {
        Text = "Zinema Kudeaketa";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1060, 680);
        BackColor = Fondo;
        Font = new Font("Segoe UI", 10F);
        DoubleBuffered = true;

        Controls.Add(BuildLayout());
        LoadData();
    }

    private Control BuildLayout()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(12, 6) };
        StyleTabs(tabs);
        tabs.TabPages.Add(BuildReservationTab());
        tabs.TabPages.Add(BuildAdminTab());
        return tabs;
    }

    private TabPage BuildReservationTab()
    {
        var page = new TabPage("Erreserbak")
        {
            BackColor = Fondo,
            ForeColor = Testua,
            UseVisualStyleBackColor = false
        };
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(18),
            BackColor = Fondo
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Panela, Padding = new Padding(22, 12, 22, 12) };
        var title = new Label
        {
            Text = "GOIERRI ZINEMA",
            Dock = DockStyle.Top,
            Height = 38,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 22F, FontStyle.Bold)
        };
        var subtitle = new Label
        {
            Text = "Aukeratu pelikula bat, ikusi eserleku libreak eta egin erreserba.",
            Dock = DockStyle.Top,
            ForeColor = TestuApala
        };
        header.Controls.Add(subtitle);
        header.Controls.Add(title);
        root.Controls.Add(header, 0, 0);
        root.SetColumnSpan(header, 2);

        var reservationPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 9,
            Padding = new Padding(18),
            BackColor = Panela
        };
        reservationPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        reservationPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        reservationPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        reservationPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        reservationPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        reservationPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        reservationPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        reservationPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        reservationPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        selectedMovieLabel.Text = "Pelikula bat aukeratu";
        selectedMovieLabel.Dock = DockStyle.Fill;
        selectedMovieLabel.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        selectedMovieLabel.ForeColor = Testua;

        seatsLabel.Dock = DockStyle.Fill;
        seatsLabel.TextAlign = ContentAlignment.MiddleLeft;
        seatsLabel.ForeColor = TestuApala;

        reservationNameInput.Dock = DockStyle.Fill;
        reservationNameInput.PlaceholderText = "Erreserbaren izena";
        StyleTextBox(reservationNameInput);
        reservationSeatsInput.Minimum = 1;
        reservationSeatsInput.Maximum = 5;
        reservationSeatsInput.Value = 1;
        reservationSeatsInput.Dock = DockStyle.Left;
        reservationSeatsInput.Width = 120;
        StyleNumeric(reservationSeatsInput);

        reserveButton.Text = "Erreserba egin";
        reserveButton.Dock = DockStyle.Fill;
        StyleButton(reserveButton, Urdina);
        reserveButton.Click += (_, _) => ReserveSeats();

        reservationPanel.Controls.Add(selectedMovieLabel, 0, 0);
        reservationPanel.Controls.Add(seatsLabel, 0, 1);
        reservationPanel.Controls.Add(DarkLabel("Erreserbaren izena"), 0, 2);
        reservationPanel.Controls.Add(reservationNameInput, 0, 3);
        reservationPanel.Controls.Add(DarkLabel("Eserlekuak (1-5)"), 0, 4);
        reservationPanel.Controls.Add(reservationSeatsInput, 0, 5);
        reservationPanel.Controls.Add(reserveButton, 0, 7);
        root.Controls.Add(reservationPanel, 0, 1);

        movieList.Dock = DockStyle.Fill;
        movieList.View = View.Details;
        movieList.FullRowSelect = true;
        movieList.MultiSelect = false;
        movieList.GridLines = false;
        movieList.HideSelection = false;
        movieList.BackColor = Panela;
        movieList.ForeColor = Testua;
        movieList.Font = new Font("Segoe UI", 11F);
        movieList.BorderStyle = BorderStyle.None;
        movieList.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        movieList.Columns.Add(new ColumnHeader { Text = "Pelikula", Width = 360 });
        movieList.Columns.Add(new ColumnHeader { Text = "Libreak", Width = 90, TextAlign = HorizontalAlignment.Right });
        movieList.Columns.Add(new ColumnHeader { Text = "Guztira", Width = 90, TextAlign = HorizontalAlignment.Right });
        movieList.SelectedIndexChanged += (_, _) => SelectMovieFromList();
        root.Controls.Add(movieList, 1, 1);

        page.Controls.Add(root);
        return page;
    }

    private TabPage BuildAdminTab()
    {
        var page = new TabPage("Kudeaketa")
        {
            BackColor = Fondo,
            ForeColor = Testua,
            UseVisualStyleBackColor = false
        };
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(18),
            BackColor = Fondo
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));

        adminGrid.Dock = DockStyle.Fill;
        adminGrid.ReadOnly = true;
        adminGrid.AllowUserToAddRows = false;
        adminGrid.AllowUserToDeleteRows = false;
        adminGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        adminGrid.MultiSelect = false;
        adminGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        adminGrid.BackgroundColor = Panela;
        adminGrid.RowHeadersVisible = false;
        StyleGrid(adminGrid);
        adminGrid.SelectionChanged += (_, _) => SelectAdminMovie();
        root.Controls.Add(adminGrid, 0, 0);

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 10,
            Padding = new Padding(18),
            BackColor = Panela
        };
        for (var i = 0; i < 9; i++)
        {
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, i is 1 or 3 ? 38 : 32));
        }
        form.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        movieNameInput.Dock = DockStyle.Fill;
        StyleTextBox(movieNameInput);
        movieSeatsInput.Minimum = 1;
        movieSeatsInput.Maximum = 500;
        movieSeatsInput.Value = 80;
        movieSeatsInput.Dock = DockStyle.Fill;
        StyleNumeric(movieSeatsInput);
        deletedInput.Text = "Desaktibatuta";
        deletedInput.Dock = DockStyle.Fill;
        deletedInput.ForeColor = TestuApala;
        deletedInput.Enabled = false;

        saveButton.Text = "Gehitu";
        saveButton.Dock = DockStyle.Fill;
        StyleButton(saveButton, Urdina);
        saveButton.Click += (_, _) => SaveMovie();

        clearButton.Text = "Inprimakia garbitu";
        clearButton.Dock = DockStyle.Fill;
        StyleButton(clearButton, PanelaArgia);
        clearButton.Click += (_, _) => ClearAdminForm();

        softDeleteButton.Text = "Desaktibatu";
        softDeleteButton.Dock = DockStyle.Fill;
        StyleButton(softDeleteButton, PanelaArgia);
        softDeleteButton.Click += (_, _) => ToggleSoftDelete();
        hardDeleteButton.Text = "Behin betiko ezabatu";
        hardDeleteButton.Dock = DockStyle.Fill;
        StyleButton(hardDeleteButton, Gorria);
        hardDeleteButton.Click += (_, _) => HardDeleteMovie();
        softDeleteButton.Enabled = false;
        hardDeleteButton.Enabled = false;

        form.Controls.Add(DarkLabel("Pelikula"), 0, 0);
        form.Controls.Add(movieNameInput, 0, 1);
        form.Controls.Add(DarkLabel("Eserleku kopurua"), 0, 2);
        form.Controls.Add(movieSeatsInput, 0, 3);
        form.Controls.Add(deletedInput, 0, 4);
        form.Controls.Add(saveButton, 0, 5);
        form.Controls.Add(clearButton, 0, 6);
        form.Controls.Add(softDeleteButton, 0, 7);
        form.Controls.Add(hardDeleteButton, 0, 8);
        root.Controls.Add(form, 1, 0);

        page.Controls.Add(root);
        return page;
    }

    private void LoadData()
    {
        LoadMovieList();
        LoadAdminGrid();
        SelectMovie(null);
    }

    private void LoadMovieList()
    {
        movieList.Items.Clear();
        foreach (var movie in repository.GetActiveMovies())
        {
            var item = new ListViewItem(movie.Izena);
            item.SubItems.Add(movie.EserlekuLibreak.ToString());
            item.SubItems.Add(movie.EserlekuKopurua.ToString());
            item.Tag = movie;
            movieList.Items.Add(item);
        }
    }

    private void LoadAdminGrid()
    {
        adminGrid.DataSource = repository.GetAllMovies()
            .Select(movie => new
            {
                movie.Id,
                Pelikula = movie.Izena,
                Eserlekuak = movie.EserlekuKopurua,
                Libreak = movie.EserlekuLibreak,
                Egoera = movie.Ezabatuta ? "Ezabatuta" : "Erabilgarri"
            })
            .ToList();
    }

    private void SelectMovieFromList()
    {
        if (movieList.SelectedItems.Count == 0)
        {
            SelectMovie(null);
            return;
        }

        SelectMovie(movieList.SelectedItems[0].Tag as Pelikula);
    }

    private void SelectMovie(Pelikula? movie)
    {
        selectedMovie = movie;
        selectedMovieLabel.Text = movie == null ? "Pelikula bat aukeratu" : movie.Izena;
        seatsLabel.Text = movie == null ? "" : $"Eserleku libreak: {movie.EserlekuLibreak}";
    }

    private void SelectAdminMovie()
    {
        if (adminGrid.SelectedRows.Count == 0)
        {
            selectedAdminMovie = null;
            saveButton.Text = "Gehitu";
            softDeleteButton.Enabled = false;
            hardDeleteButton.Enabled = false;
            return;
        }

        var id = Convert.ToInt32(adminGrid.SelectedRows[0].Cells["Id"].Value);
        selectedAdminMovie = repository.GetAllMovies().FirstOrDefault(movie => movie.Id == id);
        if (selectedAdminMovie == null)
        {
            return;
        }

        movieNameInput.Text = selectedAdminMovie.Izena;
        movieSeatsInput.Value = selectedAdminMovie.EserlekuKopurua;
        deletedInput.Checked = selectedAdminMovie.Ezabatuta;
        saveButton.Text = "Aldatu";
        softDeleteButton.Text = selectedAdminMovie.Ezabatuta ? "Berrezarri" : "Desaktibatu";
        softDeleteButton.Enabled = true;
        hardDeleteButton.Enabled = true;
    }

    private void ReserveSeats()
    {
        if (selectedMovie == null)
        {
            MessageBox.Show("Aukeratu pelikula bat.", "Abisua", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var reservationName = reservationNameInput.Text.Trim();
        if (reservationName.Length == 0)
        {
            MessageBox.Show("Idatzi erreserbaren izena.", "Abisua", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            repository.CreateReservation(selectedMovie.Id, reservationName, (int)reservationSeatsInput.Value);
            reservationNameInput.Clear();
            reservationSeatsInput.Value = 1;
            LoadData();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Ezin da erreserbatu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void SaveMovie()
    {
        var name = movieNameInput.Text.Trim();
        if (name.Length == 0)
        {
            MessageBox.Show("Idatzi pelikularen izena.", "Abisua", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (selectedAdminMovie == null)
        {
            repository.CreateMovie(name, (int)movieSeatsInput.Value);
        }
        else
        {
            repository.UpdateMovie(selectedAdminMovie.Id, name, (int)movieSeatsInput.Value, deletedInput.Checked);
        }

        ClearAdminForm();
        LoadData();
    }

    private void ToggleSoftDelete()
    {
        if (selectedAdminMovie == null)
        {
            MessageBox.Show("Aukeratu pelikula bat.", "Abisua", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (selectedAdminMovie.Ezabatuta)
        {
            repository.RestoreMovie(selectedAdminMovie.Id);
        }
        else
        {
            repository.SoftDeleteMovie(selectedAdminMovie.Id);
        }

        ClearAdminForm();
        LoadData();
    }

    private void HardDeleteMovie()
    {
        if (selectedAdminMovie == null)
        {
            MessageBox.Show("Aukeratu pelikula bat.", "Abisua", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var answer = MessageBox.Show(
            "Pelikula eta bere erreserbak behin betiko ezabatuko dira. Jarraitu?",
            "Behin betiko ezabatu",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (answer != DialogResult.Yes)
        {
            return;
        }

        repository.HardDeleteMovie(selectedAdminMovie.Id);
        ClearAdminForm();
        LoadData();
    }

    private void ClearAdminForm()
    {
        selectedAdminMovie = null;
        adminGrid.ClearSelection();
        movieNameInput.Clear();
        movieSeatsInput.Value = 80;
        deletedInput.Checked = false;
        saveButton.Text = "Gehitu";
        softDeleteButton.Text = "Desaktibatu";
        softDeleteButton.Enabled = false;
        hardDeleteButton.Enabled = false;
    }

    private static Label DarkLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            ForeColor = TestuApala,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private static Button DarkButton(string text, Color backColor)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            BackColor = backColor
        };
        StyleButton(button, backColor);
        return button;
    }

    private static void StyleTabs(TabControl tabs)
    {
        tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        tabs.SizeMode = TabSizeMode.Fixed;
        tabs.ItemSize = new Size(170, 36);
        tabs.BackColor = Fondo;
        tabs.ForeColor = Testua;
        tabs.Appearance = TabAppearance.Normal;
        tabs.DrawItem += (_, e) =>
        {
            var isSelected = e.Index == tabs.SelectedIndex;
            var rect = e.Bounds;
            using var background = new SolidBrush(isSelected ? Panela : Fondo);
            e.Graphics.FillRectangle(background, rect);

            var text = tabs.TabPages[e.Index].Text;
            var textColor = isSelected ? Color.White : TestuApala;
            TextRenderer.DrawText(
                e.Graphics,
                text,
                tabs.Font,
                rect,
                textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };
    }

    private static void StyleButton(Button button, Color backColor)
    {
        button.BackColor = backColor;
        button.ForeColor = Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Blend(backColor, Color.White, 0.10F);
        button.FlatAppearance.MouseDownBackColor = Blend(backColor, Color.Black, 0.10F);
    }

    private static void StyleTextBox(TextBox textBox)
    {
        textBox.BackColor = PanelaArgia;
        textBox.ForeColor = Testua;
        textBox.BorderStyle = BorderStyle.FixedSingle;
    }

    private static void StyleNumeric(NumericUpDown numeric)
    {
        numeric.BackColor = PanelaArgia;
        numeric.ForeColor = Testua;
    }

    private static void StyleGrid(DataGridView grid)
    {
        grid.BackgroundColor = Panela;
        grid.GridColor = Color.FromArgb(65, 78, 100);
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.DefaultCellStyle.BackColor = Panela;
        grid.DefaultCellStyle.ForeColor = Testua;
        grid.DefaultCellStyle.SelectionBackColor = Urdina;
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.BackColor = PanelaArgia;

        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(32, 41, 56);
        grid.AlternatingRowsDefaultCellStyle.ForeColor = Testua;
        grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Urdina;
        grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;

        grid.ColumnHeadersDefaultCellStyle.ForeColor = Testua;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = PanelaArgia;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Testua;
        grid.ColumnHeadersHeight = 38;
        grid.RowTemplate.Height = 34;
    }

    private static Color Blend(Color baseColor, Color overlay, float overlayAmount)
    {
        overlayAmount = Math.Clamp(overlayAmount, 0F, 1F);
        var r = (int)(baseColor.R + (overlay.R - baseColor.R) * overlayAmount);
        var g = (int)(baseColor.G + (overlay.G - baseColor.G) * overlayAmount);
        var b = (int)(baseColor.B + (overlay.B - baseColor.B) * overlayAmount);
        return Color.FromArgb(baseColor.A, r, g, b);
    }
}
