namespace ZinemaKudeaketa;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Ezin da aplikazioa ireki. Begiratu MySQL Server martxan dagoela eta konexioa zuzena dela.\n\n" + ex.Message,
                "Konexio errorea",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
