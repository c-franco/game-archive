namespace GameArchive.Web.Resources;

/// <summary>
/// Centralised UI string resources for GameArchive.
/// All user-visible text is defined here; components reference these constants
/// so that wording changes and future localisation only require editing one file.
/// </summary>
public static class AppStrings
{
    // ── Navigation ────────────────────────────────────────────────────────────
    public static class Nav
    {
        public const string BrandName    = "GameArchive";
        public const string Dashboard    = "Dashboard";
        public const string Collection   = "Colección";
        public const string Wishlist     = "Lista de deseos";
        public const string Settings     = "Ajustes";
    }

    // ── Common / shared ───────────────────────────────────────────────────────
    public static class Common
    {
        public const string Loading          = "Cargando…";
        public const string Saving           = "Guardando…";
        public const string Save             = "Guardar";
        public const string Cancel           = "Cancelar";
        public const string Delete           = "Eliminar";
        public const string Edit             = "Editar";
        public const string Back             = "← Volver";
        public const string Add              = "+ Añadir";
        public const string Confirm          = "Confirmar";
        public const string GoHome           = "Go Home";        // 404 page (kept in English as per original)
        public const string PageNotFound     = "Page not found"; // 404 page (kept in English as per original)

        public const string TypeGame         = "Juego";
        public const string TypeConsole      = "Consola";

        public const string StatusOwned      = "Adquirido";
        public const string StatusWishlist   = "Lista de deseos";

        public const string ConditionNew     = "Nuevo";
        public const string ConditionGood    = "Bueno";
        public const string ConditionFair    = "Regular";
        public const string ConditionPoor    = "Malo";

        public const string RegionPal        = "PAL";
        public const string RegionNtscU      = "NTSC-U";
        public const string RegionNtscJ      = "NTSC-J";
        public const string RegionNtsc       = "NTSC";
        public const string RegionUnknown    = "Desconocida";

        public const string SelectPlaceholder = "— Seleccionar —";
        public const string Dash              = "—";
    }

    // ── Dashboard page ────────────────────────────────────────────────────────
    public static class Dashboard
    {
        public const string Title               = "Dashboard";
        public const string StatTotalItems      = "Total artículos";
        public const string StatOwned           = "Adquiridos";
        public const string StatWishlist        = "Lista de deseos";
        public const string StatEstimatedValue  = "Valor estimado";
        public const string StatTotalSpent      = "Total invertido";
        public const string ChartByType         = "Por tipo";
        public const string ChartByPlatformTop8 = "Por plataforma (Top 8)";
    }

    // ── Collection page ───────────────────────────────────────────────────────
    public static class Collection
    {
        public const string Title              = "Colección";
        public static string SubtitleFmt(int count) => $"{count} artículos";

        public const string AddItem            = "+ Añadir artículo";
        public const string SearchPlaceholder  = "Buscar por nombre…";

        public const string FilterAllTypes     = "Todos los tipos";
        public const string FilterAllPlatforms = "Todas las plataformas";
        public const string FilterAllRegions   = "Todas las regiones";
        public const string FilterAllConditions = "Todos los estados";

        public const string SortName           = "Ordenar: Nombre";
        public const string SortDate           = "Ordenar: Fecha";
        public const string SortPrice          = "Ordenar: Precio";
        public const string SortValue          = "Ordenar: Valor";
        public const string SortDesc           = "↓ Desc";
        public const string SortAsc            = "↑ Asc";

        public const string ColName            = "Nombre";
        public const string ColType            = "Tipo";
        public const string ColPlatform        = "Plataforma";
        public const string ColRegion          = "Región";
        public const string ColCondition       = "Estado";
        public const string ColPurchasePrice   = "Pagado";
        public const string ColEstimatedValue  = "Valor";
        public const string ColChecklist       = "Checklist";

        public const string EmptyIcon          = "◈";
        public const string EmptyTitle         = "No se encontraron artículos";
        public const string EmptyDesc          = "Añade tu primer juego o consola para empezar.";
    }

    // ── Wishlist page ─────────────────────────────────────────────────────────
    public static class Wishlist
    {
        public const string Title              = "Lista de deseos";
        public static string SubtitleFmt(int count) => $"{count} artículos que quieres conseguir";

        public const string AddItem            = "+ Añadir a la lista";
        public const string SearchPlaceholder  = "Buscar en la lista…";
        public const string FilterAllTypes     = "Todos los tipos";
        public const string MarkOwned          = "✓ Marcar como adquirido";

        public const string EmptyIcon          = "♡";
        public const string EmptyTitle         = "La lista de deseos está vacía";
        public const string EmptyDesc          = "Registra los juegos y consolas que estás buscando.";
    }

    // ── Item detail / form ────────────────────────────────────────────────────
    public static class ItemDetail
    {
        public const string TitleNew           = "Nuevo artículo";
        public const string SubtitleNew        = "Añadir a tu colección";
        public const string SubtitleEdit       = "Editar detalles";

        public const string SaveNew            = "Añadir artículo";
        public const string SaveEdit           = "Guardar cambios";
        public const string SavingLabel        = "Guardando…";

        // Form field labels
        public const string FieldName          = "Nombre *";
        public const string FieldType          = "Tipo *";
        public const string FieldStatus        = "Estado *";
        public const string FieldPlatform      = "Plataforma *";
        public const string FieldRegion        = "Región *";
        public const string FieldCondition     = "Condición *";
        public const string FieldPurchaseDate  = "Fecha de compra *";
        public const string FieldPurchasePrice = "Precio de compra (€) *";
        public const string FieldEstimatedValue = "Valor estimado actual (€)";
        public const string FieldNotes         = "Notas";

        public const string NamePlaceholder    = "p.ej. The Legend of Zelda: Ocarina of Time";
        public const string PricePlaceholder   = "0.00";
        public const string NotesPlaceholder   = "Detalles de la condición, dónde lo conseguiste, número de serie…";

        // Alerts
        public const string SaveSuccess        = "Cambios guardados correctamente.";
        public const string SaveErrorForm      = "Corrige los errores antes de guardar.";
        public const string SaveErrorServer    = "Error al crear el artículo.";
        public const string SaveErrorEdit      = "Error al guardar los cambios.";

        // Checklist panel
        public const string ChecklistTitle     = "Checklist";
        public const string ChecklistEmpty     = "Sin artículos en el checklist.\nConfigura las plantillas en Ajustes.";
        public static string ChecklistProgress(int done, int total) => $"{done} / {total} completados";

        // Validation messages
        public const string ValidationNameRequired   = "El nombre es obligatorio.";
        public const string ValidationNameMaxLength  = "El nombre no puede superar los 200 caracteres.";
        public const string ValidationPlatform       = "Selecciona una plataforma.";
        public const string ValidationRegion         = "Selecciona una región.";
        public const string ValidationCondition      = "Selecciona una condición.";
        public const string ValidationDateRequired   = "La fecha de compra es obligatoria.";
        public const string ValidationDateFuture     = "La fecha de compra no puede ser futura.";
        public const string ValidationPriceRequired  = "El precio de compra es obligatorio.";
        public const string ValidationPriceNegative  = "El precio no puede ser negativo.";
        public const string ValidationValueNegative  = "El valor estimado no puede ser negativo.";
    }

    // ── Confirm / delete modal ────────────────────────────────────────────────
    public static class Modal
    {
        public const string DeleteTitle        = "¿Eliminar artículo?";
        public static string DeleteMessageFmt(string name)
            => $"Vas a eliminar {name}. Esta acción no se puede deshacer.";
        public static string DeleteWishlistMessageFmt(string name)
            => $"Vas a eliminar {name} de la lista de deseos. Esta acción no se puede deshacer.";
        public const string DeleteConfirmLabel = "Eliminar";
        public const string CancelLabel        = "Cancelar";

        // ConfirmModal component defaults (used when no parameter is passed)
        public const string DefaultTitle       = "¿Eliminar artículo?";
        public const string DefaultMessage     = "Esta acción no se puede deshacer.";
        public const string DefaultConfirm     = "Eliminar";
    }

    // ── Settings page ─────────────────────────────────────────────────────────
    public static class Settings
    {
        public const string Title              = "Ajustes";

        // Data section
        public const string DataSectionTitle   = "Datos";
        public const string ExportCsv          = "Exportar CSV";
        public const string ExportJson         = "Exportar JSON";
        public const string ImportCsv          = "Importar CSV";
        public const string ImportJson         = "Importar JSON";
        public const string Importing          = "Importando…";

        public static string ImportSuccessFmt(int created, int updated)
            => $"Importación completada — {created} creado(s), {updated} actualizado(s).";
        public static string ImportErrorFmt(int created, int updated)
            => $"Importación con errores — {created} creado(s), {updated} actualizado(s).";
        public static string ImportServerErrorFmt(int statusCode)
            => $"Error del servidor: {statusCode}";
        public static string ImportUnexpectedErrorFmt(string message)
            => $"Error inesperado: {message}";

        // Checklist templates section
        public const string ChecklistSuffix       = "Checklist";
        public const string AddTemplateRow        = "+ Añadir";
        public const string TemplateLabelPlaceholder = "Etiqueta…";
        public const string TemplateOrderTitle    = "Orden";
        public static string ChecklistEmptyFmt(string typeLabel)
            => $"Sin artículos. Los nuevos {typeLabel.ToLower()}s tendrán el checklist vacío.";

        // Platforms section
        public const string PlatformsSectionTitle    = "Plataformas";
        public const string PlatformsHint            = "Una plataforma por línea.";
        public const string PlatformsPlaceholder     = "Nintendo 64\nSNES\nPlayStation\n…";

        // Save button & feedback
        public const string SaveButton           = "Guardar ajustes";
        public const string SaveSuccess          = "Ajustes guardados correctamente.";
        public static string SaveErrorFmt(int statusCode)
            => $"Error al guardar los ajustes ({statusCode}).";
    }

    // ── Import service messages ───────────────────────────────────────────────
    public static class Import
    {
        public const string ErrInvalidJson           = "El fichero JSON no es válido: ";
        public const string ErrInvalidCsv            = "El fichero CSV no es válido: ";
        public const string ErrNameRequired          = "el campo 'Name' es obligatorio.";
        public static string ErrUnknownTypeFmt(int row, string name, string type)
            => $"Fila {row} ({name}): tipo '{type}' no reconocido (valores válidos: Game, Console).";
        public static string ErrRowNameMissingFmt(int row)
            => $"Fila {row}: el campo 'Name' es obligatorio.";

        // ImportController
        public const string ErrNoFileProvided        = "No se ha enviado ningún fichero.";
    }
}
