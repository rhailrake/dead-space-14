// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared._Donate;
using Robust.Client.UserInterface;

namespace Content.Client._Donate.UI;

public sealed class DonateShopSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _interfaceManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<UpdateDonateShopUIState>(OnUIStateUpdate);
        SubscribeNetworkEvent<UpdateEnergyShopState>(OnEnergyShopUpdate);
        SubscribeNetworkEvent<PurchaseEnergyItemResult>(OnPurchaseResult);
        SubscribeNetworkEvent<UpdateDailyCalendarState>(OnCalendarStateUpdate);
        SubscribeNetworkEvent<ClaimCalendarRewardResult>(OnClaimResult);
    }

    private void OnUIStateUpdate(UpdateDonateShopUIState ev)
    {
        var controller = _interfaceManager.GetUIController<DonateShopUIController>();
        controller.UpdateWindowState(ev.State);
    }

    private void OnEnergyShopUpdate(UpdateEnergyShopState ev)
    {
        var controller = _interfaceManager.GetUIController<DonateShopUIController>();
        controller.UpdateEnergyShopState(ev.State);
    }

    private void OnPurchaseResult(PurchaseEnergyItemResult ev)
    {
        var controller = _interfaceManager.GetUIController<DonateShopUIController>();
        controller.HandlePurchaseResult(ev.Result);
    }

    private void OnCalendarStateUpdate(UpdateDailyCalendarState ev)
    {
        var controller = _interfaceManager.GetUIController<DonateShopUIController>();
        controller.UpdateCalendarState(ev.State);
    }

    private void OnClaimResult(ClaimCalendarRewardResult ev)
    {
        var controller = _interfaceManager.GetUIController<DonateShopUIController>();
        controller.HandleClaimResult(ev.Result);
    }
}
