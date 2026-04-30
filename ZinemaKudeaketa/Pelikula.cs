namespace ZinemaKudeaketa;

public sealed class Pelikula
{
    public int Id { get; set; }
    public string Izena { get; set; } = "";
    public int EserlekuKopurua { get; set; }
    public bool Ezabatuta { get; set; }
    public int Erreserbatuta { get; set; }
    public int EserlekuLibreak => Math.Max(0, EserlekuKopurua - Erreserbatuta);
}
