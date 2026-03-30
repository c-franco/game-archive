namespace GameArchive.Application.Resources;

/// <summary>
/// Centralised string resources for the Application, Infrastructure and API layers.
/// Only user-facing messages (error responses, validation failures) are kept here —
/// internal technical keys, route templates and log categories are intentionally excluded.
/// </summary>
public static class ServerStrings
{
    // ── Import service ────────────────────────────────────────────────────────
    public static class Import
    {
        public const string ErrInvalidJson       = "El fichero JSON no es válido: ";
        public const string ErrInvalidCsv        = "El fichero CSV no es válido: ";

        public static string ErrRowNameMissing(int row)
            => $"Fila {row}: el campo 'Name' es obligatorio.";

        public static string ErrUnknownType(int row, string name, string type)
            => $"Fila {row} ({name}): tipo '{type}' no reconocido (valores válidos: Game, Console).";
    }

    // ── ImportController ──────────────────────────────────────────────────────
    public static class ImportController
    {
        public const string ErrNoFileProvided = "No se ha enviado ningún fichero.";
    }

    // ── ItemsController / handlers ────────────────────────────────────────────
    public static class Items
    {
        public static string NotFoundFmt(Guid id) => $"Item {id} not found";
    }
}
