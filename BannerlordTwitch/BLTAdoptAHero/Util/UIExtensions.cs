using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;

namespace BLTAdoptAHero.Util
{
    public static class UIExtensions
    {
        public static void UpdateInventoryUI(this GameStateManager gsm, Hero adoptedHero = null)
        {
            if (gsm.ActiveState is InventoryState inventoryState
                && (adoptedHero == null || inventoryState.InventoryLogic?.OwnerCharacter == adoptedHero.CharacterObject))
            {
                inventoryState.InventoryLogic?.Reset(false);
            }
        }
    }
}