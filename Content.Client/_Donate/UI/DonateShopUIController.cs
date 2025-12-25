// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Client._Donate.Emerald;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared._Donate;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Donate.UI;

public sealed class DonateShopUIController : UIController
{
    [Dependency] private readonly IEntityManager _manager = default!;

    private DonateShopWindow? _donateShopWindow;
    private EmeraldLootboxOpenWindow? _lootboxWindow;

    private MenuButton? DonateButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.DonateButton;

    public void UnloadButton()
    {
        if (DonateButton == null)
        {
            return;
        }

        DonateButton.Pressed = false;
        DonateButton.OnPressed -= OnPressed;
    }

    public void LoadButton()
    {
        if (DonateButton == null)
        {
            return;
        }

        DonateButton.OnPressed += OnPressed;
    }

    private void OnPressed(BaseButton.ButtonEventArgs obj)
    {
        ToggleWindow();
    }

    public void UpdateWindowState(DonateShopState state)
    {
        if (_donateShopWindow == null)
            return;

        _donateShopWindow.ApplyState(state);
    }

    public void UpdateEnergyShopState(EnergyShopState state)
    {
        if (_donateShopWindow == null)
            return;

        _donateShopWindow.ApplyEnergyShopState(state);
    }

    public void UpdateCalendarState(DailyCalendarState state)
    {
        if (_donateShopWindow == null)
            return;

        _donateShopWindow.ApplyCalendarState(state);
    }

    public void HandlePurchaseResult(PurchaseResult result)
    {
        if (_donateShopWindow == null)
            return;

        _donateShopWindow.ShowPurchaseResult(result);
    }

    public void HandleClaimResult(ClaimRewardResult result)
    {
        if (_donateShopWindow == null)
            return;

        _donateShopWindow.ShowClaimResult(result);
    }

    public void HandleLootboxOpenResult(LootboxOpenResult result)
    {
        if (_lootboxWindow != null && _lootboxWindow.IsOpen)
        {
            _lootboxWindow.HandleOpenResult(result);
        }
    }

    public void OpenLootboxWindow(string name, int userItemId, bool stelsHidden)
    {
        if (_lootboxWindow != null)
        {
            _lootboxWindow.Close();
        }

        _lootboxWindow = new EmeraldLootboxOpenWindow();
        _lootboxWindow.SetLootbox(name, userItemId, stelsHidden);

        _lootboxWindow.OnOpenRequested += (itemId, stels) =>
        {
            _manager.EntityNetManager.SendSystemNetworkMessage(new RequestOpenLootbox(itemId, stels));
        };

        _lootboxWindow.OnCloseRequested += () =>
        {
            _lootboxWindow?.Close();
            _lootboxWindow = null;

            _manager.EntityNetManager.SendSystemNetworkMessage(new RequestUpdateDonateShop());
        };

        _lootboxWindow.OnClose += () =>
        {
            _lootboxWindow = null;
        };

        _lootboxWindow.OpenCentered();
    }

    public void ToggleWindow()
    {
        if (_donateShopWindow == null)
        {
            _donateShopWindow = new DonateShopWindow();
            _donateShopWindow.OnClose += () =>
            {
                _donateShopWindow = null;

                if (DonateButton != null)
                    DonateButton.Pressed = false;
            };

            _donateShopWindow.OnOpenLootbox += (name, itemId, stelsHidden) =>
            {
                OpenLootboxWindow(name, itemId, stelsHidden);
            };

            _donateShopWindow.OpenCentered();
            _manager.EntityNetManager.SendSystemNetworkMessage(new RequestUpdateDonateShop());
            return;
        }

        if (_donateShopWindow.IsOpen)
        {
            _donateShopWindow.Close();
        }
        else
        {
            _donateShopWindow.OpenCentered();
            _manager.EntityNetManager.SendSystemNetworkMessage(new RequestUpdateDonateShop());
        }
    }
}
