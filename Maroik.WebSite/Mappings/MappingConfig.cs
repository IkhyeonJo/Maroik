using Mapster;
using Maroik.Core.Contract.Dtos;

namespace Maroik.WebSite.Mappings;

/// <summary>
/// Mapster mapping configuration for the WebSite presentation layer.
/// Registered via <c>TypeAdapterConfig.GlobalSettings.Scan(Assembly)</c> at startup.
/// Defines explicit type-adapter rules where property names differ, conversions are required,
/// or certain destination fields must be ignored (e.g. server-side-only fields like AccountEmail,
/// HashedPassword, Writer, and audit timestamps).
/// </summary>
public class MappingConfig : IRegister
{
    // Expression trees (used by Mapster .Map()) do not support inline `out var` declarations.
    private static DateTime ParseDate(string? s) =>
        DateTime.TryParse(s, out var d) ? d : default;

    /// <summary>Registers all Mapster type-adapter rules for the WebSite presentation layer.</summary>
    public void Register(TypeAdapterConfig config)
    {
        // -- Account ----------------------------------------------------------
        // Response → OutputViewModel: property names are identical, convention handles it.
        config.NewConfig<AccountResponse, Models.ViewModels.Management.AccountOutputViewModel>();

        // InputViewModel → Request: Password (plain text) has no matching dest property;
        // HashedPassword is computed server-side and must NOT be overwritten by mapping.
        config.NewConfig<Models.ViewModels.Management.AccountInputViewModel, AccountRequest>()
            .Ignore(dest => dest.HashedPassword!);

        // -- Asset -------------------------------------------------------------
        config.NewConfig<AssetResponse, Models.ViewModels.AccountBook.AssetOutputViewModel>();

        // AccountEmail is set server-side from the session, not from the form.
        config.NewConfig<Models.ViewModels.AccountBook.AssetInputViewModel, AssetRequest>()
            .Ignore(dest => dest.AccountEmail!);

        // -- Expenditure -------------------------------------------------------
        // MonetaryUnit exists in OutputViewModel but not in ExpenditureResponse;
        // Mapster leaves it at its default - controller populates it from account settings.
        config.NewConfig<ExpenditureResponse, Models.ViewModels.AccountBook.ExpenditureOutputViewModel>();
        config.NewConfig<ExpenditureResponse, Models.ViewModels.DashBoard.ExpenditureOutputViewModel>();

        config.NewConfig<Models.ViewModels.AccountBook.ExpenditureInputViewModel, ExpenditureRequest>()
            .Ignore(dest => dest.AccountEmail!);

        // -- Income ------------------------------------------------------------
        config.NewConfig<IncomeResponse, Models.ViewModels.AccountBook.IncomeOutputViewModel>();
        config.NewConfig<IncomeResponse, Models.ViewModels.DashBoard.IncomeOutputViewModel>();

        config.NewConfig<Models.ViewModels.AccountBook.IncomeInputViewModel, IncomeRequest>()
            .Ignore(dest => dest.AccountEmail!);

        // -- Fixed Expenditure -------------------------------------------------
        // MaturityDate: DateTime (DTO) ↔ string (ViewModel) - explicit conversion required.
        // Noticed / Expired exist only in OutputViewModel; controller sets them after mapping.
        config.NewConfig<FixedExpenditureResponse, Models.ViewModels.Notice.FixedExpenditureOutputViewModel>()
            .Map(dest => dest.MaturityDate, src => src.MaturityDate.ToString("yyyy-MM-dd"));

        config.NewConfig<Models.ViewModels.Notice.FixedExpenditureInputViewModel, FixedExpenditureRequest>()
            .Map(dest => dest.MaturityDate, src => ParseDate(src.MaturityDate))
            .Ignore(dest => dest.AccountEmail!);

        // -- Fixed Income ------------------------------------------------------
        config.NewConfig<FixedIncomeResponse, Models.ViewModels.Notice.FixedIncomeOutputViewModel>()
            .Map(dest => dest.MaturityDate, src => src.MaturityDate.ToString("yyyy-MM-dd"));

        config.NewConfig<Models.ViewModels.Notice.FixedIncomeInputViewModel, FixedIncomeRequest>()
            .Map(dest => dest.MaturityDate, src => ParseDate(src.MaturityDate))
            .Ignore(dest => dest.AccountEmail!);

        // -- Board -------------------------------------------------------------
        // UploadedFile (IFormFile) has no matching property in BoardRequest - ignored automatically.
        // Writer / Type / Created / Updated / View / Deleted are set server-side.
        config.NewConfig<Models.ViewModels.Forum.BoardInputViewModel, BoardRequest>()
            .Ignore(dest => dest.Writer!)
            .Ignore(dest => dest.Type!)
            .Ignore(dest => dest.Created)
            .Ignore(dest => dest.Updated)
            .Ignore(dest => dest.View)
            .Ignore(dest => dest.Deleted);

        config.NewConfig<Models.ViewModels.Management.BoardInputViewModel, BoardRequest>()
            .Ignore(dest => dest.Writer!)
            .Ignore(dest => dest.Type!)
            .Ignore(dest => dest.Created)
            .Ignore(dest => dest.Updated)
            .Ignore(dest => dest.View)
            .Ignore(dest => dest.Deleted);

        // -- Calendar Event ----------------------------------------------------
        // StartDate / EndDate come in as strings from the form; parse to DateTime.
        // CalendarEventUploadedFile / SerializedCalendarReminders have no dest match - ignored automatically.
        config.NewConfig<Models.ViewModels.Calendar.CalendarEventInputViewModel, CalendarEventRequest>()
            .Map(dest => dest.StartDate, src => ParseDate(src.StartDate))
            .Map(dest => dest.EndDate, src => ParseDate(src.EndDate))
            .Ignore(dest => dest.RecurrenceId!)
            .Ignore(dest => dest.Created)
            .Ignore(dest => dest.Updated);

        config.NewConfig<CalendarEventResponse, Models.ViewModels.Calendar.CalendarEventOutputViewModel>();
    }
}
