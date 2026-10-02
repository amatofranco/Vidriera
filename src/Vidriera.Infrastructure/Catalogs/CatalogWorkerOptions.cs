namespace Vidriera.Infrastructure.Catalogs;

public class CatalogWorkerOptions
{
    public string? WorkerUrl { get; set; }
    public string? ApiKey { get; set; }

    // true en el despliegue de Cloud Run: evita que esa instancia levante su propio
    // CatalogGenerationWorker (polling interno), que podría tomar un job distinto al que
    // llegó por HTTP y correrlo en paralelo en el mismo proceso, algo que PDFium no soporta.
    public bool IsWorkerNode { get; set; }
}
