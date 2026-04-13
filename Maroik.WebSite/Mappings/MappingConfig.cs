using Mapster;
using Maroik.Core.Contract.Dtos;
using Maroik.WebSite.Models.ViewModels.AccountBook;
using Maroik.WebSite.Models.ViewModels.Calendar;
using Maroik.WebSite.Models.ViewModels.Notice;
using AccountBookExpOutput = Maroik.WebSite.Models.ViewModels.AccountBook.ExpenditureOutputViewModel;
using AccountBookIncOutput = Maroik.WebSite.Models.ViewModels.AccountBook.IncomeOutputViewModel;
using DashBoardExpOutput = Maroik.WebSite.Models.ViewModels.DashBoard.ExpenditureOutputViewModel;
using DashBoardIncOutput = Maroik.WebSite.Models.ViewModels.DashBoard.IncomeOutputViewModel;
using MgmtAccountInput = Maroik.WebSite.Models.ViewModels.Management.AccountInputViewModel;
using MgmtAccountOutput = Maroik.WebSite.Models.ViewModels.Management.AccountOutputViewModel;
using MgmtBoardInput = Maroik.WebSite.Models.ViewModels.Management.BoardInputViewModel;
using ForumBoardInput = Maroik.WebSite.Models.ViewModels.Forum.BoardInputViewModel;

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
        // ── Account ──────────────────────────────────────────────────────────
        // Response → OutputViewModel: property names are identical, convention handles it.
        config.NewConfig<AccountResponse, MgmtAccountOutput>();

        // InputViewModel → Request: Password (plain text) has no matching dest property;
        // HashedPassword is computed server-side and must NOT be overwritten by mapping.
        config.NewConfig<MgmtAccountInput, AccountRequest>()
            .Ignore(dest => dest.HashedPassword!);

        // ── Asset ─────────────────────────────────────────────────────────────
        config.NewConfig<AssetResponse, AssetOutputViewModel>();

        // AccountEmail is set server-side from the session, not from the form.
        config.NewConfig<AssetInputViewModel, AssetRequest>()
            .Ignore(dest => dest.AccountEmail!);

        // ── Expenditure ───────────────────────────────────────────────────────
        // MonetaryUnit exists in OutputViewModel but not in ExpenditureResponse;
        // Mapster leaves it at its default - controller populates it from account settings.
        config.NewConfig<ExpenditureResponse, AccountBookExpOutput>();
        config.NewConfig<ExpenditureResponse, DashBoardExpOutput>();

        config.NewConfig<ExpenditureInputViewModel, ExpenditureRequest>()
            .Ignore(dest => dest.AccountEmail!);

        // ── Income ────────────────────────────────────────────────────────────
        config.NewConfig<IncomeResponse, AccountBookIncOutput>();
        config.NewConfig<IncomeResponse, DashBoardIncOutput>();

        config.NewConfig<IncomeInputViewModel, IncomeRequest>()
            .Ignore(dest => dest.AccountEmail!);

        // ── Fixed Expenditure ─────────────────────────────────────────────────
        // MaturityDate: DateTime (DTO) ↔ string (ViewModel) - explicit conversion required.
        // Noticed / Expired exist only in OutputViewModel; controller sets them after mapping.
        config.NewConfig<FixedExpenditureResponse, FixedExpenditureOutputViewModel>()
            .Map(dest => dest.MaturityDate, src => src.MaturityDate.ToString("yyyy-MM-dd"));

        config.NewConfig<FixedExpenditureInputViewModel, FixedExpenditureRequest>()
            .Map(dest => dest.MaturityDate, src => ParseDate(src.MaturityDate))
            .Ignore(dest => dest.AccountEmail!);

        // ── Fixed Income ──────────────────────────────────────────────────────
        config.NewConfig<FixedIncomeResponse, FixedIncomeOutputViewModel>()
            .Map(dest => dest.MaturityDate, src => src.MaturityDate.ToString("yyyy-MM-dd"));

        config.NewConfig<FixedIncomeInputViewModel, FixedIncomeRequest>()
            .Map(dest => dest.MaturityDate, src => ParseDate(src.MaturityDate))
            .Ignore(dest => dest.AccountEmail!);

        // ── Board ─────────────────────────────────────────────────────────────
        // UploadedFile (IFormFile) has no matching property in BoardRequest - ignored automatically.
        // Writer / Type / Created / Updated / View / Deleted are set server-side.
        config.NewConfig<ForumBoardInput, BoardRequest>()
            .Ignore(dest => dest.Writer!)
            .Ignore(dest => dest.Type!)
            .Ignore(dest => dest.Created)
            .Ignore(dest => dest.Updated)
            .Ignore(dest => dest.View)
            .Ignore(dest => dest.Deleted);

        config.NewConfig<MgmtBoardInput, BoardRequest>()
            .Ignore(dest => dest.Writer!)
            .Ignore(dest => dest.Type!)
            .Ignore(dest => dest.Created)
            .Ignore(dest => dest.Updated)
            .Ignore(dest => dest.View)
            .Ignore(dest => dest.Deleted);

        // ── Calendar Event ────────────────────────────────────────────────────
        // StartDate / EndDate come in as strings from the form; parse to DateTime.
        // CalendarEventUploadedFile / SerializedCalendarReminders have no dest match - ignored automatically.
        config.NewConfig<CalendarEventInputViewModel, CalendarEventRequest>()
            .Map(dest => dest.StartDate, src => ParseDate(src.StartDate))
            .Map(dest => dest.EndDate, src => ParseDate(src.EndDate))
            .Ignore(dest => dest.RecurrenceId!)
            .Ignore(dest => dest.Created)
            .Ignore(dest => dest.Updated);

        config.NewConfig<CalendarEventResponse, CalendarEventOutputViewModel>();
    }
}
